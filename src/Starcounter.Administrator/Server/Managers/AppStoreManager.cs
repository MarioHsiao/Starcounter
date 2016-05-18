using Administrator.Server.Managers;
using Administrator.Server.Model;
using Starcounter;
using Starcounter.Administrator.API.Handlers;
using Starcounter.Administrator.Server;
using Starcounter.Administrator.Server.Handlers;
using Starcounter.Administrator.Server.Utilities;
using Starcounter.Server.PublicModel;
using Starcounter.Server.Rest.Representations.JSON;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Administrator.Server.Managers {
    /// <summary>
    /// Responsable for:
    /// * Get a list of all applications on the appstore
    /// </summary>
    public class AppStoreManager {

        public static string AppStoreServerHost;

        /// <summary>
        /// Get applications from appstore
        /// </summary>
        /// <param name="store"></param>
        /// <param name="completionCallback"></param>
        /// <param name="errorCallback"></param>
        public static void GetApplications(AppStoreStore store, Action<IList<AppStoreApplication>> completionCallback = null, Action<string> errorCallback = null) {

            lock (ServerManager.ServerInstance) {

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
                        catch (InvalidOperationException e) {

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
        }

        /// <summary>
        /// Get applications from appstore
        /// </summary>
        /// <param name="store"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static IList<AppStoreApplication> GetApplications(AppStoreStore store, out string message) {

            IList<AppStoreApplication> applications = new List<AppStoreApplication>();
            // TODO:
            message = null;

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

                    //                    Starcounter.Internal.CurrentVersion.Version;

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
        /// <param name="completionCallback"></param>
        /// <param name="errorCallback"></param>
        public static void GetStores(Action<IList<AppStoreStore>> completionCallback = null, Action<string> errorCallback = null) {

            lock (ServerManager.ServerInstance) {

                Dictionary<String, String> headers = new Dictionary<String, String> { { "acceptversion", "application/appstore.polyjuice.apps-v4+json" } };

                Http.GET("http://" + AppStoreManager.AppStoreServerHost + "/appstore/apps", headers, (Response response) => {

                    if (response.IsSuccessStatusCode) {
                        if (completionCallback != null) {
                            completionCallback(PopulateStoresFromResponse(response));
                        }
                        return;
                    }

                    if (errorCallback != null) {
                        StarcounterAdminAPI.AdministratorLogSource.Debug("[AppStore 0003] GetStores(): StatusCode:" + response.StatusCode);
                        if (!string.IsNullOrEmpty(response.Body)) {
                            StarcounterAdminAPI.AdministratorLogSource.Debug("[AppStore 0003]: resultBody:" + response.Body);
                        }

                        if (errorCallback != null) {
                            errorCallback("At the moment The App Store Service is not avaiable. Try again later.");
                        }
                    }
                });
            }
        }

        /// <summary>
        /// Get Stores
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static IList<AppStoreStore> GetStores(out string message) {
            IList<AppStoreStore> stores = new List<AppStoreStore>();
            // TODO:
            message = null;

            Dictionary<String, String> headers = new Dictionary<String, String> { { "acceptversion", "application/appstore.polyjuice.apps-v4+json" } };

            Response response = Http.GET("http://" + AppStoreManager.AppStoreServerHost + "/appstore/apps", headers, 10);

            if (!response.IsSuccessStatusCode) {
                StarcounterAdminAPI.AdministratorLogSource.Debug("[AppStore 0004] GetStores(): StatusCode:" + response.StatusCode);
                if (!string.IsNullOrEmpty(response.Body)) {
                    StarcounterAdminAPI.AdministratorLogSource.Debug("[AppStore 0004]: resultBody:" + response.Body);
                }
                message = "At the moment The App Store Service is not avaiable. Try again later.";
                return null;
            }

            return PopulateStoresFromResponse(response);
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
    }
}
