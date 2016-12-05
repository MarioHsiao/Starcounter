using Administrator.Server.Managers;
using Administrator.Server.Model;
using Starcounter;
using Starcounter.Administrator.API.Handlers;
using Starcounter.Administrator.Server;
using Starcounter.Administrator.Server.Handlers;
using Starcounter.Administrator.Server.Utilities;
using Starcounter.Internal;
using Starcounter.Server.PublicModel;
using Starcounter.Server.Rest.Representations.JSON;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Warehouse.Api.REST;

namespace Administrator.Server.Managers {
    /// <summary>
    /// Responsable for:
    /// * Get a list of all applications on the appstore
    /// </summary>
    public class AppStoreManager {

        private static WarehouseSettings WarehouseSettings {
            get {
                return GetWarehouseSettings();
            }
        }

        /// <summary>
        /// Get applications from appstore
        /// </summary>
        /// <param name="store"></param>
        /// <param name="completionCallback"></param>
        /// <param name="errorCallback"></param>
        public static void GetApplications(AppStoreStore store, Action<IList<AppStoreApplication>> completionCallback = null, Action<string> errorCallback = null) {

            Dictionary<String, String> headers = new Dictionary<String, String> { { "acceptversion", "application/" + typeof(Softwares_v2).FullName + "+json" } };
            Http.GET(string.Format("http://{0}:{1}/warehouse/api/depots/{2}/applications", WarehouseSettings.WarehouseHost, WarehouseSettings.WarehousePort, store.DepotKey), headers, (Response response) => {

                try {
                    if (response.IsSuccessStatusCode) {

                        Softwares_v2 depotSoftwares = new Softwares_v2();
                        depotSoftwares.PopulateFromJson(response.Body);

                        List<AppStoreApplication> result = new List<AppStoreApplication>();

                        foreach (var softwareJson in depotSoftwares.Items) {

                            IList<AppStoreApplication> appStoreApplications = WarehouseSoftwareToAppStoreApplication(store, softwareJson);
                            foreach (AppStoreApplication item in appStoreApplications) {
                                if (result.Find(x => x.ID == item.ID) == null) {
                                    result.Add(item);
                                }
                            }
                        }

                        completionCallback?.Invoke(result);

                    }
                    else {
                        StarcounterAdminAPI.AdministratorLogSource.Debug(string.Format("Warehouse: ({0}), {1}", response.StatusCode, response.Body));
                    }
                }
                catch (Exception e) {
                    StarcounterAdminAPI.AdministratorLogSource.LogException(e, "Warehouse");
                    errorCallback?.Invoke(e.Message);
                }
            });
        }

        /// <summary>
        /// Get applications from appstore
        /// </summary>
        /// <param name="store"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static IList<AppStoreApplication> GetApplications(AppStoreStore store, out string message) {

            message = null;

            Dictionary<String, String> headers = new Dictionary<String, String> { { "acceptversion", "application/" + typeof(Softwares_v2).FullName + "+json" } };
            Response response = Http.GET(string.Format("http://{0}:{1}/warehouse/api/depots/{2}/applications", WarehouseSettings.WarehouseHost, WarehouseSettings.WarehousePort, store.DepotKey), headers);

            if (response.IsSuccessStatusCode) {

                Softwares_v2 depotSoftwares = new Softwares_v2();
                depotSoftwares.PopulateFromJson(response.Body);

                List<AppStoreApplication> applications = new List<AppStoreApplication>();

                foreach (Software_v2 softwareJson in depotSoftwares.Items) {

                    IList<AppStoreApplication> appStoreApplications = WarehouseSoftwareToAppStoreApplication(store, softwareJson);
                    foreach (AppStoreApplication appStoreApplication in appStoreApplications) {
                        if (applications.Find(x => x.ID == softwareJson.Software.ID) == null) {
                            applications.Add(appStoreApplication);
                        }
                    }
                }

                return applications;
            }
            message = "At the moment The Warehouse Service is not avaiable. Try again later.";
            return null;
        }

        /// <summary>
        /// Get Stores
        /// </summary>
        /// <remarks>
        /// Fetches from AppStore and Warehouse
        /// </remarks>
        /// <param name="completionCallback"></param>
        /// <param name="errorCallback"></param>
        public static void GetStores(Action<IList<AppStoreStore>> completionCallback = null, Action<string> errorCallback = null) {

            ConcurrentQueue<string> depotKeys = new ConcurrentQueue<string>();

            Dictionary<String, String> headers = new Dictionary<String, String> { { "acceptversion", "application/" + typeof(Depots_v1).FullName + "+json" } };
            string url = string.Format("http://{0}:{1}/warehouse/api/depots?version={2}&versiondate={3}&channel={4}", WarehouseSettings.WarehouseHost, WarehouseSettings.WarehousePort, HttpUtility.UrlEncode(CurrentVersion.Version), HttpUtility.UrlEncode(CurrentVersion.VersionDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'")), HttpUtility.UrlEncode(CurrentVersion.ChannelName));
            Http.GET(url, headers, (response) => {

                try {
                    if (response.IsSuccessStatusCode) {
                        Depots_v1 depotsJson = new Depots_v1();
                        depotsJson.PopulateFromJson(response.Body);
                        foreach (Depot_v1 depotJson in depotsJson.Depots) {
                            depotKeys.Enqueue(depotJson.DepotKey);
                        }
                    }
                    else {
                        StarcounterAdminAPI.AdministratorLogSource.Debug(string.Format("Warehouse: ({0}), {1}", response.StatusCode, response.Body));
                    }

                    // Extra stores
                    List<string> keys = GetDepotKeys();
                    foreach (string depotKey in keys) {
                        if (!depotKeys.Contains<string>(depotKey)) {
                            depotKeys.Enqueue(depotKey);
                        }
                    }

                    ProcessWarehouseDepot(depotKeys, null, completionCallback, errorCallback);
                }
                catch (Exception e) {
                    StarcounterAdminAPI.AdministratorLogSource.LogException(e, "Warehouse");
                    errorCallback.Invoke(e.Message);
                }
            }, 20);
        }

        /// <summary>
        /// Get Stores
        /// </summary>
        /// <remarks>
        /// Fetches from AppStore and Warehouse
        /// </remarks>
        /// <param name="message"></param>
        /// <returns></returns>
        public static IList<AppStoreStore> GetStores(out string message) {

            List<AppStoreStore> stores = new List<AppStoreStore>();
            message = null;

            Dictionary<String, String> headers = new Dictionary<String, String> { { "acceptversion", "application/" + typeof(Depots_v1).FullName + "+json" } };

            string relativeUrl = "/warehouse/api/depots";
            string url = string.Format("http://{0}:{1}{2}?version={3}&versiondate={4}&channel={5}", WarehouseSettings.WarehouseHost, WarehouseSettings.WarehousePort, relativeUrl, HttpUtility.UrlEncode(CurrentVersion.Version), HttpUtility.UrlEncode(CurrentVersion.VersionDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'")), HttpUtility.UrlEncode(CurrentVersion.ChannelName));
            Response response = Http.GET(url, headers, 20);
            if (response.IsSuccessStatusCode) {
                Depots_v1 depotsJson = new Depots_v1();
                depotsJson.PopulateFromJson(response.Body);
                foreach (Depot_v1 depotJson in depotsJson.Depots) {
                    AppStoreStore store = WarehouseDepotToAppStoreStore(relativeUrl, depotJson);
                    stores.Add(store);
                }
            }
            else {
                message = "At the moment The Warehouse Service is not avaiable. Try again later.";
                StarcounterAdminAPI.AdministratorLogSource.Debug(string.Format("Warehouse: ({0}), {1}", response.StatusCode, response.Body));
            }

            // Extra stores
            List<string> depotKeys = GetDepotKeys();
            depotKeys = depotKeys.Distinct().ToList();

            foreach (string depotKey in depotKeys) {

                relativeUrl = string.Format("/warehouse/api/depots/{0}", depotKey);

                url = string.Format("http://{0}:{1}{2}", WarehouseSettings.WarehouseHost, WarehouseSettings.WarehousePort, relativeUrl);

                response = Http.GET(url, headers, 20);
                if (response.IsSuccessStatusCode) {
                    Depot_v1 depotJson = new Depot_v1();
                    depotJson.PopulateFromJson(response.Body);
                    bool bExist = false;
                    foreach (AppStoreStore item in stores) {
                        if (item.DepotKey == depotJson.DepotKey) {
                            // Exist
                            bExist = true;
                            break;
                        }
                    }
                    if (!bExist) {
                        AppStoreStore store = WarehouseDepotToAppStoreStore(relativeUrl, depotJson);
                        stores.Add(store);
                    }
                }
                else {
                    StarcounterAdminAPI.AdministratorLogSource.Debug(string.Format("Warehouse: ({0}), {1}", response.StatusCode, response.Body));
                }
            }

            return stores;
        }

        /// <summary>
        /// Process list of depots keysm resulting in a list of stores
        /// </summary>
        /// <param name="depotKeys"></param>
        /// <param name="stores"></param>
        /// <param name="completionCallback"></param>
        /// <param name="errorCallback"></param>
        private static void ProcessWarehouseDepot(ConcurrentQueue<string> depotKeys, List<AppStoreStore> stores, Action<List<AppStoreStore>> completionCallback = null, Action<string> errorCallback = null) {

            if (stores == null) {
                stores = new List<AppStoreStore>();
            }

            if (depotKeys.Count == 0) {
                completionCallback(stores);
                return;
            }

            // Get next depot key
            string depotKey;
            depotKeys.TryDequeue(out depotKey);

            Dictionary<String, String> headers = new Dictionary<String, String> { { "acceptversion", "application/" + typeof(Depots_v1).FullName + "+json" } };

            string relativeUrl = string.Format("/warehouse/api/depots/{0}", depotKey);

            string url = string.Format("http://{0}:{1}{2}", WarehouseSettings.WarehouseHost, WarehouseSettings.WarehousePort, relativeUrl);
            Http.GET(url, headers, (Response response) => {

                try {
                    if (response.IsSuccessStatusCode) {
                        Depot_v1 depotJson = new Depot_v1();
                        depotJson.PopulateFromJson(response.Body);

                        AppStoreStore store = WarehouseDepotToAppStoreStore(relativeUrl, depotJson);
                        stores.Add(store);
                    }
                    else {
                        StarcounterAdminAPI.AdministratorLogSource.Debug(string.Format("Warehouse: ({0}), {1}", response.StatusCode, response.Body));
                    }

                    ProcessWarehouseDepot(depotKeys, stores, completionCallback, errorCallback);
                }
                catch (Exception e) {
                    StarcounterAdminAPI.AdministratorLogSource.LogException(e, "Warehouse");
                    errorCallback.Invoke(e.Message);
                }
            }, 20);
        }

        /// <summary>
        /// Get/Assure warehouse settings
        /// </summary>
        /// <returns></returns>
        private static WarehouseSettings GetWarehouseSettings() {

            WarehouseSettings settings = new WarehouseSettings();
            settings.WarehouseHost = StarcounterEnvironment.InternetAddresses.DefaultWarehouseHost;
            settings.WarehousePort = StarcounterEnvironment.InternetAddresses.DefaultWarehousePort;

            try {
                string warehouseConfig = Path.Combine(StarcounterEnvironment.Server.ServerDir, "WarehouseSettings.json");

                if (File.Exists(warehouseConfig)) {
                    settings.PopulateFromJson(File.ReadAllText(warehouseConfig));
                }
                else {
                    settings = new WarehouseSettings();
                    File.WriteAllText(warehouseConfig, settings.ToJson());
                }
                return settings;
            }
            catch (Exception e) {
                StarcounterAdminAPI.AdministratorLogSource.LogException(e, "Warehouse");
                return settings;
            }
        }

        /// <summary>
        /// Get depot keys from settings
        /// </summary>
        /// <returns>List of depot keys</returns>
        private static List<string> GetDepotKeys() {

            List<string> depotKeys = new List<string>();

            foreach (var depot in WarehouseSettings.Stores) {
                depotKeys.Add(depot.DepotKey);
            }

            return depotKeys;
        }

        private static IList<AppStoreApplication> WarehouseSoftwareToAppStoreApplication(AppStoreStore store, Software_v2 softwareJson) {

            List<AppStoreApplication> result = new List<AppStoreApplication>();

            if (softwareJson.SoftwareContent.Count == 0) {
                foreach (Version_v1 versionJson in softwareJson.Software.Versions) {
                    #region Create Appp

                    AppStoreApplication application = VersionToAppStoreApplication(versionJson);
                    application.Namespace = softwareJson.Software.Namespace;
                    application.DisplayName = softwareJson.Software.Name;
                    application.Description = softwareJson.Software.Description;
                    application.ImageUri = string.Format("http://{0}:{1}{2}", WarehouseSettings.WarehouseHost, WarehouseSettings.WarehousePort, softwareJson.Software.IconUrl);
                    application.Company = softwareJson.Software.Organization;
                    application.Database = store.Database;
                    application.StoreID = store.ID;
                    application.StoreUrl = store.SourceUrl;
                    result.Add(application);
                    #endregion
                }
            }
            else {

                // It's a suite.
                IList<AppStoreApplication> suiteContent = CreateAppsFromSuit(store, softwareJson);
                foreach (AppStoreApplication item in suiteContent) {
                    result.Add(item);
                }
            }
            return result;
        }

        private static IList<AppStoreApplication> CreateAppsFromSuit(AppStoreStore store, Software_v2 softwareJson) {

            List<AppStoreApplication> result = new List<AppStoreApplication>();

            foreach (SoftwareItem_v2 suiteApp in softwareJson.SoftwareContent) {

                foreach (Version_v1 versionJson in suiteApp.Versions) {

                    AppStoreApplication application = VersionToAppStoreApplication(versionJson);
                    application.Namespace = suiteApp.Namespace;
                    application.DisplayName = suiteApp.Name;
                    application.Description = suiteApp.Description;
                    application.Company = suiteApp.Organization;
                    application.ImageUri = string.Format("http://{0}:{1}{2}", WarehouseSettings.WarehouseHost, WarehouseSettings.WarehousePort, suiteApp.IconUrl);
                    application.Database = store.Database;
                    application.StoreID = store.ID;
                    application.StoreUrl = store.SourceUrl;

                    result.Add(application);
                }
            }
            return result;
        }

        private static AppStoreApplication VersionToAppStoreApplication(Version_v1 versionJson) {

            AppStoreApplication application = new AppStoreApplication();
            //application.AppName = string.Empty;
            application.ResourceFolder = string.Empty;
            application.Executable = string.Empty;
            application.Arguments = string.Empty;
            application.Heading = string.Empty;
            application.Rating = 0;

            application.Channel = versionJson.Channel;
            application.Version = versionJson.Name;
            application.VersionDate = DateTime.Parse(versionJson.VersionDate, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
            application.SourceID = versionJson.ID;
            application.SourceUrl = string.Format("http://{0}:{1}{2}", WarehouseSettings.WarehouseHost, WarehouseSettings.WarehousePort, versionJson.Url);
            application.ID = Starcounter.Administrator.Server.Utilities.RestUtils.GetHashString(application.SourceUrl); // Use namespace+channel+version ?

            // Add dependencies
            application.Dependencies = new Dictionary<string, string>();
            application.Dependencies.Add("Starcounter", versionJson.Compatiblility);

            return application;
        }

        private static AppStoreStore WarehouseDepotToAppStoreStore(string url, Depot_v1 depotJson) {

            AppStoreStore store = new AppStoreStore();
            store.DepotKey = depotJson.DepotKey;
            store.ID = RestUtils.GetHashString(string.Format("http://{0}{1}", WarehouseSettings.WarehouseHost, store.DepotKey));
            store.SourceID = depotJson.DepotKey;
            store.SourceUrl = string.Format("http://{0}:{1}{2}", WarehouseSettings.WarehouseHost, WarehouseSettings.WarehousePort, url);
            store.DisplayName = depotJson.Name;
            store.Description = depotJson.Description;
            return store;
        }

    }
}
