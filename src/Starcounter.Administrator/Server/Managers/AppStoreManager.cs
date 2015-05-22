using Administrator.Server.Managers;
using Administrator.Server.Model;
using Starcounter;
using Starcounter.Administrator.API.Handlers;
using Starcounter.Administrator.Server;
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

#if LOCAL_APPSTORE
        static string appStoreHost = "http://127.0.0.1:8787";
#else
        static string appStoreHost = "http://appstore.polyjuice.com:8787";
#endif

        /// <summary>
        /// Get applications from an appstore
        /// </summary>
        /// <param name="databaseName"></param>
        public static void GetApplications(Administrator.Server.Model.Database database, AppStoreStore store, Action<IList<AppStoreApplication>> completionCallback = null, Action<string> errorCallback = null) {

            if (string.IsNullOrEmpty(AppStoreManager.appStoreHost)) {

                if (errorCallback != null) {
                    errorCallback("Configuration error, Unknown App Store host");
                }
                return;
            }

            Http.GET(AppStoreManager.appStoreHost + "/appstore/apps", null, null, (Response response, Object userObject) => {

                try {

                    if (!response.IsSuccessStatusCode) {
                        throw new InvalidOperationException("At the moment The App Store Service is not avaiable. Try again later.");
                    }

                    //Representations.JSON.RemoteAppStoreItems remoteAppStoreItems = new Representations.JSON.RemoteAppStoreItems();
                    //remoteAppStoreItems.PopulateFromJson(response.Body);

                    //IList<AppStoreApplication> applications = new List<AppStoreApplication>();

                    //foreach (var remoteStore in remoteAppStoreItems.Stores) {

                    //    string id = RestUtils.GetHashString(AppStoreManager.appStoreHost + remoteStore.ID);
                    //    if (store.ID != id) continue;

                    //    foreach (var remoteApp in remoteStore.Items) {

                    //        AppStoreApplication application = RemoteAppStoreApplicationToAppStoreApplication(remoteApp);
                    //        application.Database = database;
                    //        applications.Add(application);
                    //    }
                    //}

                    if (completionCallback != null) {
                        completionCallback(PopulateApplicationsFromResponse(database, store, response));
                    }
                }
                catch (InvalidOperationException e) {
                    if (errorCallback != null) {
                        errorCallback(e.Message);
                    }
                }
            });
        }

        public static IList<AppStoreApplication> GetApplications(Administrator.Server.Model.Database database, AppStoreStore store, out string message) {

            IList<AppStoreApplication> applications = new List<AppStoreApplication>();
            // TODO:
            message = null;

            Response response = Http.GET(AppStoreManager.appStoreHost + "/appstore/apps", null, 10000);

            if (!response.IsSuccessStatusCode) {
                message = "At the moment The App Store Service is not avaiable. Try again later.";
                return null;
            }

            return PopulateApplicationsFromResponse(database, store, response);

        }

        private static IList<AppStoreApplication> PopulateApplicationsFromResponse(Administrator.Server.Model.Database database, AppStoreStore store, Response response) {

            Representations.JSON.RemoteAppStoreItems remoteAppStoreItems = new Representations.JSON.RemoteAppStoreItems();
            remoteAppStoreItems.PopulateFromJson(response.Body);

            IList<AppStoreApplication> applications = new List<AppStoreApplication>();

            foreach (var remoteStore in remoteAppStoreItems.Stores) {

                string id = RestUtils.GetHashString(AppStoreManager.appStoreHost + remoteStore.ID);
                if (store.ID != id) continue;

                foreach (var remoteApp in remoteStore.Items) {

                    AppStoreApplication application = RemoteAppStoreApplicationToAppStoreApplication(remoteApp);
                    application.Database = database;
                    applications.Add(application);
                }
            }

            return applications;
        }

        /// <summary>
        /// Get AppStore applications
        /// </summary>
        /// <param name="database"></param>
        /// <param name="applications"></param>
        //public static void GetApplications(Administrator.Server.Model.Database database, out IList<AppStoreApplication> applications) {

        //    string uri = AppStoreManager.appStoreHost + "/appstore/apps";
        //    Response response = X.GET(uri);

        //    ResponseToList(response, database, out applications);
        //}

        /// <summary>
        /// Translate Response to an AppStore Application list
        /// </summary>
        /// <param name="response"></param>
        /// <param name="database"></param>
        /// <param name="applications"></param>
        /// <returns></returns>
        //private static bool ResponseToList(Response response, Administrator.Server.Model.Database database, AppStoreStore store, out IList<AppStoreApplication> applications) {

        //    applications = new List<AppStoreApplication>();

        //    if (!response.IsSuccessStatusCode) {
        //        throw new InvalidOperationException("At the moment The App Store Service is not avaiable. Try again later.");
        //    }

        //    Representations.JSON.RemoteAppStoreItems remoteAppStoreItems = new Representations.JSON.RemoteAppStoreItems();
        //    remoteAppStoreItems.PopulateFromJson(response.Body);

        //    IList<DeployedConfigFile> downloadedApplications = DeployManager.GetItems(database.ID);

        //    foreach (var remoteStore in remoteAppStoreItems.Stores) {

        //        string id = RestUtils.GetHashString(AppStoreManager.appStoreHost + remoteStore.ID);
        //        if (store.ID != id) continue;

        //        foreach (var remoteApp in remoteStore.Items) {

        //            AppStoreApplication application = RemoteAppStoreApplicationToAppStoreApplication(remoteApp);
        //            application.Database = database;
        //            applications.Add(application);
        //        }
        //    }

        //    return true;
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="host"></param>
        /// <param name="completionCallback"></param>
        /// <param name="errorCallback"></param>
        public static void GetStores(Administrator.Server.Model.Database database, Action<IList<AppStoreStore>> completionCallback = null, Action<string> errorCallback = null) {

            Http.GET(AppStoreManager.appStoreHost + "/appstore/apps", null, null, (Response response, Object userObject) => {

                if (!response.IsSuccessStatusCode) {
                    if (errorCallback != null) {
                        errorCallback("At the moment The App Store Service is not avaiable. Try again later.");
                    }
                    return;
                }

                //Representations.JSON.RemoteAppStoreItems remoteAppStoreItems = new Representations.JSON.RemoteAppStoreItems();
                //remoteAppStoreItems.PopulateFromJson(response.Body);

                //IList<AppStoreStore> stores = new List<AppStoreStore>();

                //foreach (var remoteStore in remoteAppStoreItems.Stores) {

                //    AppStoreStore store = new AppStoreStore();
                //    store.Database = database;
                //    store.ID = RestUtils.GetHashString(AppStoreManager.appStoreHost + remoteStore.ID);
                //    store.DisplayName = remoteStore.DisplayName;
                //    stores.Add(store);
                //}

                if (completionCallback != null) {
                    completionCallback(PopulateStoresFromResponse(database, response));
                }
            });
        }

        public static IList<AppStoreStore> GetStores(Administrator.Server.Model.Database database, out string message) {
            IList<AppStoreStore> stores = new List<AppStoreStore>();
            // TODO:
            message = null;

            Response response = Http.GET(AppStoreManager.appStoreHost + "/appstore/apps", null, 10000);

            if (!response.IsSuccessStatusCode) {
                message = "At the moment The App Store Service is not avaiable. Try again later.";
                return null;
            }

            return PopulateStoresFromResponse(database, response);
        }

        private static IList<AppStoreStore> PopulateStoresFromResponse(Administrator.Server.Model.Database database, Response response) {

            Representations.JSON.RemoteAppStoreItems remoteAppStoreItems = new Representations.JSON.RemoteAppStoreItems();
            remoteAppStoreItems.PopulateFromJson(response.Body);

            IList<AppStoreStore> stores = new List<AppStoreStore>();

            foreach (var remoteStore in remoteAppStoreItems.Stores) {

                AppStoreStore store = new AppStoreStore();
                store.Database = database;
                store.ID = RestUtils.GetHashString(AppStoreManager.appStoreHost + remoteStore.ID);
                store.DisplayName = remoteStore.DisplayName;
                stores.Add(store);
            }

            return stores;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="appStoreHost"></param>
        /// <param name="items"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        private static bool GetAppStoreItems(string appStoreHost, out Representations.JSON.RemoteAppStoreItems items, out Response response) {


            items = new Representations.JSON.RemoteAppStoreItems();

            if (string.IsNullOrEmpty(appStoreHost)) {
                ErrorResponse errorResponse = new ErrorResponse();
                errorResponse.Text = string.Format("Configuration error, Unknown App Store host");
                response = new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.ServiceUnavailable, BodyBytes = errorResponse.ToJsonUtf8() };
                return false;
            }

            string uri = appStoreHost + "/appstore/apps";

            // Get App Store items (external source)
            HandlerOptions opt = new HandlerOptions() { CallExternalOnly = true };
            X.GET(uri, out response, null, 10000, opt);
            if (!response.IsSuccessStatusCode) {

                ErrorResponse errorResponse = new ErrorResponse();
                errorResponse.Text = string.Format("At the moment The App Store Service is not avaiable. Try again later.");
                errorResponse.Helplink = appStoreHost;
                response = new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.ServiceUnavailable, BodyBytes = errorResponse.ToJsonUtf8() };
                return false;
            }

            items.PopulateFromJson(response.Body);

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="app"></param>
        /// <param name="applicationJson"></param>
        public static AppStoreApplication RemoteAppStoreApplicationToAppStoreApplication(Representations.JSON.RemoteAppStoreItem remoteApp) {

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
            application.Heading = remoteApp.Heading;
            application.Rating = remoteApp.Rating;

            application.ID = Starcounter.Administrator.Server.Utilities.RestUtils.GetHashString(application.SourceUrl); // Use namespace+channel+version ?

            return application;
        }
    }
}
