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

        public static string AppStoreServerHost;
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

            if (string.IsNullOrEmpty(store.DepotKey)) {
                GetApplicationsFromAppStore(store, completionCallback, errorCallback);
                return;
            }
            GetApplicationsFromWarehouse(store, completionCallback, errorCallback);

        }

        private static void GetApplicationsFromAppStore(AppStoreStore store, Action<IList<AppStoreApplication>> completionCallback = null, Action<string> errorCallback = null) {

            if (string.IsNullOrEmpty(AppStoreManager.AppStoreServerHost)) {

                if (errorCallback != null) {
                    errorCallback("Configuration error, Unknown App Store host");
                }
                return;
            }

            try {


                Dictionary<String, String> headers = new Dictionary<String, String> { { "acceptversion", "application/appstore.polyjuice.apps-v4+json" } };

                Http.GET("http://" + AppStoreManager.AppStoreServerHost + "/appstore/apps", headers, (Response response) => {

                    try {

                        if (!response.IsSuccessStatusCode) {

                            StarcounterAdminAPI.AdministratorLogSource.Debug("[AppStore 0001] GetApplications(): StatusCode:" + response.StatusCode);
                            if (!string.IsNullOrEmpty(response.Body)) {
                                StarcounterAdminAPI.AdministratorLogSource.Debug("[AppStore 0001]: resultBody:" + response.Body);
                            }

                            throw new InvalidOperationException("At the moment The App Store Service is not avaiable. Try again later.");
                        }

                        if (completionCallback != null) {
                            completionCallback(PopulateApplicationsFromResponse(store, response));
                        }
                    }
                    catch (Exception e) {

                        if (errorCallback != null) {
                            errorCallback(e.Message);
                        }
                    }
                });
            }
            catch (Exception e) {

                if (errorCallback != null) {
                    errorCallback(e.Message);
                }
            }
        }

        /// <summary>
        /// Get applications from appstore
        /// </summary>
        /// <param name="store"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static IList<AppStoreApplication> GetApplications(AppStoreStore store, out string message) {

            IList<AppStoreApplication> applications = new List<AppStoreApplication>();
            message = null;

            if (!string.IsNullOrEmpty(store.DepotKey)) {
                return GetApplicationsFromWarehouse(store, out message);
            }

            Dictionary<String, String> headers = new Dictionary<String, String> { { "acceptversion", "application/appstore.polyjuice.apps-v4+json" } };

            Response response = Http.GET("http://" + AppStoreManager.AppStoreServerHost + "/appstore/apps", headers, 10);

            if (!response.IsSuccessStatusCode) {
                StarcounterAdminAPI.AdministratorLogSource.Debug("[AppStore 0002] GetApplications(): StatusCode:" + response.StatusCode);
                if (!string.IsNullOrEmpty(response.Body)) {
                    StarcounterAdminAPI.AdministratorLogSource.Debug("[AppStore 0002]: resultBody:" + response.Body);
                }

                message = "At the moment The App Store Service is not avaiable. Try again later.";
                return null;
            }

            return PopulateApplicationsFromResponse(store, response);

        }

        /// <summary>
        /// Populate response to an result
        /// </summary>
        /// <param name="store"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        private static IList<AppStoreApplication> PopulateApplicationsFromResponse(AppStoreStore store, Response response) {

            Representations.JSON.RemoteAppStoreItems remoteAppStoreItems = new Representations.JSON.RemoteAppStoreItems();
            remoteAppStoreItems.PopulateFromJson(response.Body);

            IList<AppStoreApplication> applications = new List<AppStoreApplication>();

            foreach (var remoteStore in remoteAppStoreItems.Stores) {

                string id = RestUtils.GetHashString("http://" + AppStoreManager.AppStoreServerHost + remoteStore.ID);
                if (store.ID != id) continue;

                string storeUrl = "http://" + AppStoreManager.AppStoreServerHost + "/appstore/stores/" + remoteStore.ID;    // TODO: Strange url

                foreach (var remoteApp in remoteStore.Items) {

                    AppStoreApplication application = RemoteAppStoreApplicationToAppStoreApplication(remoteApp);
                    application.Database = store.Database;
                    application.StoreID = store.ID;
                    application.StoreUrl = storeUrl;
                    applications.Add(application);
                }
            }

            return applications;
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

            Dictionary<String, String> headers = new Dictionary<String, String> { { "acceptversion", "application/appstore.polyjuice.apps-v4+json" } };
            Http.GET("http://" + AppStoreManager.AppStoreServerHost + "/appstore/apps", headers, (Response response) => {

                if (response.IsSuccessStatusCode) {
                    if (completionCallback != null) {

                        IList<AppStoreStore> result = PopulateStoresFromResponse(response);

                        // Add wharehouse stores
                        GetWarehouseStores((warehouseStores) => {

                            foreach (AppStoreStore store in warehouseStores) {
                                result.Add(store);
                            }
                            completionCallback(result);

                        }, (errorMessage) => {
                            // Warehouse errors is already logged.
                            completionCallback(result);
                        });
                    }
                }
                else {
                    // Add wharehouse stores
                    GetWarehouseStores(completionCallback, errorCallback);
                }
            });
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
            IList<AppStoreStore> stores = new List<AppStoreStore>();
            IList<AppStoreStore> result = null;
            message = null;

            Dictionary<String, String> headers = new Dictionary<String, String> { { "acceptversion", "application/appstore.polyjuice.apps-v4+json" } };
            Response response = Http.GET("http://" + AppStoreManager.AppStoreServerHost + "/appstore/apps", headers, 10);

            if (!response.IsSuccessStatusCode) {
                result = PopulateStoresFromResponse(response);
            }
            else {
                message = "At the moment The App Store Service is not avaiable. Try again later.";
            }

            // Add warehouse stores
            string warehouseMessage;
            IList<AppStoreStore> warehouseStores = GetWarehouseStores(out warehouseMessage);
            if (result == null) {
                return warehouseStores;
            }

            foreach (AppStoreStore store in warehouseStores) {
                result.Add(store);
            }

            if (message == null) {
                message = warehouseMessage;
            }
            else if (warehouseMessage != null) {
                message += ". " + warehouseMessage;
            }

            return result;
        }

        /// <summary>
        /// Populate response to an result
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        private static IList<AppStoreStore> PopulateStoresFromResponse(Response response) {

            Representations.JSON.RemoteAppStoreItems remoteAppStoreItems = new Representations.JSON.RemoteAppStoreItems();
            remoteAppStoreItems.PopulateFromJson(response.Body);

            IList<AppStoreStore> stores = new List<AppStoreStore>();

            foreach (var remoteStore in remoteAppStoreItems.Stores) {

                AppStoreStore store = new AppStoreStore();
                store.ID = RestUtils.GetHashString("http://" + AppStoreManager.AppStoreServerHost + remoteStore.ID);
                store.DisplayName = remoteStore.DisplayName;
                store.Description = remoteStore.Description;
                stores.Add(store);
            }

            return stores;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="remoteApp"></param>
        /// <returns></returns>
        private static AppStoreApplication RemoteAppStoreApplicationToAppStoreApplication(Representations.JSON.RemoteAppStoreItem remoteApp) {

            AppStoreApplication application = new AppStoreApplication();
            application.Namespace = remoteApp.Namespace;
            application.Channel = remoteApp.Channel;
            application.DisplayName = remoteApp.DisplayName;
            //application.AppName = string.Empty;
            application.Description = remoteApp.Description;
            application.Version = remoteApp.Version;
            application.VersionDate = DateTime.Parse(remoteApp.VersionDate, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
            application.ResourceFolder = string.Empty;
            application.Company = remoteApp.Company;
            application.ImageUri = remoteApp.ImageUrl;
            application.Executable = string.Empty;
            application.Arguments = string.Empty;

            application.SourceID = remoteApp.ID;
            application.SourceUrl = remoteApp.Url;

            //application.StoreID = 
            //application.StoreUrl = 

            application.Heading = remoteApp.Heading;
            application.Rating = remoteApp.Rating;

            application.ID = Starcounter.Administrator.Server.Utilities.RestUtils.GetHashString(application.SourceUrl); // Use namespace+channel+version ?

            // Add dependencies
            application.Dependencies = new Dictionary<string, string>();

            foreach (var dependency in remoteApp.Dependencies) {
                application.Dependencies.Add(dependency.Name, dependency.Value);
            }

            return application;
        }

        #region Starcounter Warehouse

        #region GetStores

        /// <summary>
        /// Get warehouse stores from warehouse and extra configured stores
        /// </summary>
        /// <returns>List of warehouse stores</returns>
        private static IList<AppStoreStore> GetWarehouseStores(out string message) {

            List<AppStoreStore> stores = new List<AppStoreStore>();
            message = null;

            Dictionary<String, String> headers = new Dictionary<String, String> { { "acceptversion", "application/warehouse.starcounter.depots-v1+json" } };
            string url = string.Format("http://{0}:{1}/warehouse/api/depots?version={2}&versiondate={3}&channel={4}", WarehouseSettings.WarehouseHost, WarehouseSettings.WarehousePort, HttpUtility.UrlEncode(CurrentVersion.Version), HttpUtility.UrlEncode(CurrentVersion.VersionDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'")), HttpUtility.UrlEncode(CurrentVersion.ChannelName));
            Response response = Http.GET(url, headers, 20);
            if (response.IsSuccessStatusCode) {
                Depots_v1 depotsJson = new Depots_v1();
                depotsJson.PopulateFromJson(response.Body);
                foreach (Depot_v1 depotJson in depotsJson.Depots) {
                    AppStoreStore store = WarehouseDepotToAppStoreStore(url, depotJson);
                    stores.Add(store);
                }
            }
            else {
                message = "At the moment The Basic Warehouse Service is not avaiable. Try again later.";
                StarcounterAdminAPI.AdministratorLogSource.Debug(string.Format("Warehouse: ({0}), {1}", response.StatusCode, response.Body));
            }

            // Extra stores
            List<string> depotKeys = GetDepotKeys();
            depotKeys = depotKeys.Distinct().ToList();

            foreach (string depotKey in depotKeys) {

                url = string.Format("http://{0}:{1}/warehouse/api/depots/{2}", WarehouseSettings.WarehouseHost, WarehouseSettings.WarehousePort, depotKey);

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
                        AppStoreStore store = WarehouseDepotToAppStoreStore(url, depotJson);
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
        /// Get warehouse stores from warehouse and extra configured stores
        /// </summary>
        /// <param name="completionCallback"></param>
        /// <param name="errorCallback"></param>
        private static void GetWarehouseStores(Action<IList<AppStoreStore>> completionCallback, Action<string> errorCallback) {

            ConcurrentQueue<string> depotKeys = new ConcurrentQueue<string>();

            Dictionary<String, String> headers = new Dictionary<String, String> { { "acceptversion", "application/warehouse.starcounter.depots-v1+json" } };
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

            Dictionary<String, String> headers = new Dictionary<String, String> { { "acceptversion", "application/warehouse.starcounter.depot-v1+json" } };
            string url = string.Format("http://{0}:{1}/warehouse/api/depots/{2}", WarehouseSettings.WarehouseHost, WarehouseSettings.WarehousePort, depotKey);
            Http.GET(url, headers, (Response response) => {

                try {
                    if (response.IsSuccessStatusCode) {
                        Depot_v1 depotJson = new Depot_v1();
                        depotJson.PopulateFromJson(response.Body);

                        AppStoreStore store = WarehouseDepotToAppStoreStore(url, depotJson);
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
            settings.WarehouseHost = "warehouse.starcounter.com";
            settings.WarehousePort = 80;

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

        #endregion

        #region Get Applications
        private static IList<AppStoreApplication> GetApplicationsFromWarehouse(AppStoreStore store, out string message) {

            message = null;

            Dictionary<String, String> headers = new Dictionary<String, String> { { "acceptversion", "application/warehouse.starcounter.depotsoftwares-v1+json" } };
            Response response = Http.GET(string.Format("http://{0}:{1}/warehouse/api/depots/{2}/applications", WarehouseSettings.WarehouseHost, WarehouseSettings.WarehousePort, store.DepotKey), headers);

            if (response.IsSuccessStatusCode) {

                DepotSoftwares_v1 depotSoftwares = new DepotSoftwares_v1();
                depotSoftwares.PopulateFromJson(response.Body);

                List<AppStoreApplication> applications = new List<AppStoreApplication>();

                foreach (DepotSoftwares_v1.SoftwaresElementJson item in depotSoftwares.Softwares) {

                    Dictionary<String, String> headers2 = new Dictionary<String, String> { { "acceptversion", "application/warehouse.starcounter.software-v1+json" } };
                    Response softwareResponse = Http.GET(string.Format("http://{0}:{1}{1}", WarehouseSettings.WarehouseHost, WarehouseSettings.WarehousePort, item.Url), headers2);

                    if (softwareResponse.IsSuccessStatusCode) {

                        Software_v1 softwareJson = new Software_v1();
                        softwareJson.PopulateFromJson(softwareResponse.Body);
                        IList<AppStoreApplication> appStoreApplications = WarehouseSoftwareToAppStoreApplication(store, softwareJson);
                        foreach (AppStoreApplication appStoreApplication in appStoreApplications) {
                            if (applications.Find(x => x.ID == item.ID) == null) {
                                applications.Add(appStoreApplication);
                            }
                        }
                    }
                    else {
                        StarcounterAdminAPI.AdministratorLogSource.Debug(string.Format("Warehouse: ({0}), {1}", softwareResponse.StatusCode, softwareResponse.Body));
                    }
                }

                return applications;
            }
            message = "At the moment The Warehouse Store Service is not avaiable. Try again later.";
            return null;
        }

        private static void GetApplicationsFromWarehouse(AppStoreStore store, Action<IList<AppStoreApplication>> completionCallback = null, Action<string> errorCallback = null) {

            Dictionary<String, String> headers = new Dictionary<String, String> { { "acceptversion", "application/warehouse.starcounter.depotsoftwares-v1+json" } };
            Http.GET(string.Format("http://{0}:{1}/warehouse/api/depots/{2}/applications", WarehouseSettings.WarehouseHost, WarehouseSettings.WarehousePort, store.DepotKey), headers, (Response response) => {

                try {
                    if (response.IsSuccessStatusCode) {

                        DepotSoftwares_v1 depotSoftwares = new DepotSoftwares_v1();
                        depotSoftwares.PopulateFromJson(response.Body);

                        ConcurrentQueue<DepotSoftwares_v1.SoftwaresElementJson> queue = new ConcurrentQueue<DepotSoftwares_v1.SoftwaresElementJson>();

                        foreach (var item in depotSoftwares.Softwares) {
                            queue.Enqueue(item);
                        }

                        ProsessSoftware(store, queue, null, completionCallback, errorCallback);
                    }
                    else {
                        StarcounterAdminAPI.AdministratorLogSource.Debug(string.Format("Warehouse: ({0}), {1}", response.StatusCode, response.Body));
                    }
                }
                catch (Exception e) {
                    StarcounterAdminAPI.AdministratorLogSource.LogException(e, "Warehouse");
                    errorCallback.Invoke(e.Message);
                }
            });
        }

        #endregion

        private static void ProsessSoftware(AppStoreStore store, ConcurrentQueue<DepotSoftwares_v1.SoftwaresElementJson> softwares, List<AppStoreApplication> appStoreApplication, Action<List<AppStoreApplication>> completionCallback = null, Action<string> errorCallback = null) {

            if (appStoreApplication == null) {
                appStoreApplication = new List<AppStoreApplication>();
            }

            if (softwares.Count == 0) {
                completionCallback(appStoreApplication);
                return;
            }

            // Get next depot key
            DepotSoftwares_v1.SoftwaresElementJson depotKey;
            softwares.TryDequeue(out depotKey);

            string url = string.Format("http://{0}:{1}{2}", WarehouseSettings.WarehouseHost, WarehouseSettings.WarehousePort, depotKey.Url);

            Dictionary<String, String> headers = new Dictionary<String, String> { { "acceptversion", "application/warehouse.starcounter.software-v1+json" } };
            Http.GET(url, headers, (Response response) => {
                try {
                    if (response.IsSuccessStatusCode) {
                        Software_v1 softwareJson = new Software_v1();
                        softwareJson.PopulateFromJson(response.Body);
                        IList<AppStoreApplication> appStoreApplications = WarehouseSoftwareToAppStoreApplication(store, softwareJson);
                        foreach (AppStoreApplication item in appStoreApplications) {
                            if (appStoreApplication.Find(x => x.ID == item.ID) == null) {
                                appStoreApplication.Add(item);
                            }
                        }

                    }
                    else {
                        StarcounterAdminAPI.AdministratorLogSource.Debug(string.Format("Warehouse: ({0}), {1}", response.StatusCode, response.Body));
                    }

                    ProsessSoftware(store, softwares, appStoreApplication, completionCallback, errorCallback);
                }
                catch (Exception e) {
                    StarcounterAdminAPI.AdministratorLogSource.LogException(e, "Warehouse");
                    errorCallback.Invoke(e.Message);
                }
            }, 20);
        }

        private static IList<AppStoreApplication> WarehouseSoftwareToAppStoreApplication(AppStoreStore store, Software_v1 softwareJson) {

            List<AppStoreApplication> result = new List<AppStoreApplication>();

            if (softwareJson.Apps.Count == 0) {
                foreach (Version_v1 versionJson in softwareJson.Versions) {
                    #region Create Appp

                    AppStoreApplication application = VersionToAppStoreApplication(versionJson);
                    application.Namespace = softwareJson.Namespace;
                    application.DisplayName = softwareJson.Name;
                    application.Description = softwareJson.Description;
                    application.ImageUri = string.Format("http://{0}:{1}{2}", WarehouseSettings.WarehouseHost, WarehouseSettings.WarehousePort, softwareJson.IconUrl);
                    application.Company = softwareJson.Organization;
                    application.Database = store.Database;
                    application.StoreID = store.ID;
                    application.StoreUrl = string.Format("http://{0}:{1}{2}", WarehouseSettings.WarehouseHost, WarehouseSettings.WarehousePort, store.SourceUrl); // "http://" + AppStoreManager.AppStoreServerHost + "/appstore/stores/" + store.ID;    // TODO: Strange url
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

        private static IList<AppStoreApplication> CreateAppsFromSuit(AppStoreStore store, Software_v1 softwareJson) {

            List<AppStoreApplication> result = new List<AppStoreApplication>();

            foreach (var suiteApp in softwareJson.Apps) {

                foreach (Version_v1 versionJson in suiteApp.Versions) {

                    AppStoreApplication application = VersionToAppStoreApplication(versionJson);
                    application.Namespace = suiteApp.Namespace;
                    application.DisplayName = suiteApp.Name;
                    application.Description = suiteApp.Description;
                    application.Company = suiteApp.Organization;
                    application.ImageUri = string.Format("http://{0}:{1}{2}", WarehouseSettings.WarehouseHost, WarehouseSettings.WarehousePort, suiteApp.IconUrl);
                    application.Database = store.Database;
                    application.StoreID = store.ID;
                    application.StoreUrl = string.Format("http://{0}:{1}{2}", WarehouseSettings.WarehouseHost, WarehouseSettings.WarehousePort, store.SourceUrl); // "http://" + AppStoreManager.AppStoreServerHost + "/appstore/stores/" + store.ID;    // TODO: Strange url

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
            store.SourceUrl = url;
            store.DisplayName = depotJson.Name;
            store.Description = depotJson.Description;
            return store;
        }

        #endregion
    }
}
