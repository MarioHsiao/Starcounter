using Administrator.Server.Managers;
using Administrator.Server.Model;
using Starcounter;
using Starcounter.Administrator.API.Handlers;
using Starcounter.Administrator.Server;
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


        //static public bool Download(DatabaseApplication application, out Response response) {
        //    throw new NotImplementedException("Download Application");
        //}

        //static public bool Delete(DatabaseApplication application, out Response response) {
        //    throw new NotImplementedException("Delete Application");
        //}

        //static public bool Download(DatabaseApplication application) {
        //    throw new NotImplementedException("Download Application");
        //}

        //static public bool Delete(DatabaseApplication application) {
        //    throw new NotImplementedException("Delete Application");
        //}

        /// <summary>
        /// Get applications from an appstore
        /// </summary>
        /// <param name="databaseName"></param>
        public static void GetApplications(Administrator.Server.Model.Database database, Action<IList<AppStoreApplication>> completionCallback = null, Action<string> errorCallback = null) {

            if (string.IsNullOrEmpty(appStoreHost)) {

                if (errorCallback != null) {
                    errorCallback("Configuration error, Unknown App Store host");
                }
                return;
            }

            string uri = AppStoreManager.appStoreHost + "/appstore/apps";

            X.GET(uri, null, null, (Response response, Object userObject) => {

                IList<AppStoreApplication> applications = new List<AppStoreApplication>();

                try {
                    ResponseToList(response, database, out applications);
                    if (completionCallback != null) {
                        completionCallback(applications);
                    }
                }
                catch (InvalidOperationException e) {
                    if (errorCallback != null) {
                        errorCallback(e.Message);
                    }
                }
            });
        }

        /// <summary>
        /// Get AppStore applications
        /// </summary>
        /// <param name="database"></param>
        /// <param name="applications"></param>
        public static void GetApplications(Administrator.Server.Model.Database database, out IList<AppStoreApplication> applications) {

            string uri = AppStoreManager.appStoreHost + "/appstore/apps";
            Response response = X.GET(uri);

            ResponseToList(response, database, out applications);
        }

        /// <summary>
        /// Translate Response to an AppStore Application list
        /// </summary>
        /// <param name="response"></param>
        /// <param name="database"></param>
        /// <param name="applications"></param>
        /// <returns></returns>
        private static bool ResponseToList(Response response, Administrator.Server.Model.Database database, out IList<AppStoreApplication> applications) {

            applications = new List<AppStoreApplication>();

            if (!response.IsSuccessStatusCode) {
                throw new InvalidOperationException("At the moment The App Store Service is not avaiable. Try again later.");
            }

            Representations.JSON.RemoteAppStoreItems remoteAppStoreItems = new Representations.JSON.RemoteAppStoreItems();
            remoteAppStoreItems.PopulateFromJson(response.Body);

            IList<DeployedConfigFile> downloadedApplications = DeployManager.GetItems(database.ID);

            foreach (var store in remoteAppStoreItems.Stores) {

                foreach (var remoteApp in store.Items) {

                    AppStoreApplication application = RemoteAppStoreApplicationToAppStoreApplication(remoteApp);
                    application.Database = database;
                    applications.Add(application);
                }
            }

            return true;
        }

        public static bool GetStores() {

            string uri = AppStoreManager.appStoreHost + "/appstore/apps";
            Response response = X.GET(uri);


            return false;
        }



        /// <summary>
        /// Get applications from an appstore
        /// </summary>
        /// <param name="databaseName"></param>
        //public static bool GetApplications(string appStoreHost, out IList<AppStoreApplication> applications, out Response response) {

        //    applications = new List<AppStoreApplication>();

        //    Representations.JSON.RemoteAppStoreItems remoteAppStoreItems;
        //    if (GetAppStoreItems(appStoreHost, out remoteAppStoreItems, out response) == false) {
        //        return false;
        //    }

        //    IList<AppConfig> downloadedApplications = DeployManager.GetItems();

        //    foreach (var store in remoteAppStoreItems.Stores) {

        //        foreach (var remoteApp in store.Items) {

        //            AppStoreApplication application = new AppStoreApplication();

        //            application.Database = null;    // TODO:
        //            application.Namespace = remoteApp.Namespace;
        //            application.Channel = remoteApp.Channel;
        //            //application.DatabaseName = string.Empty;
        //            application.DisplayName = remoteApp.DisplayName;
        //            application.AppName = string.Empty;
        //            application.Description = remoteApp.Description;
        //            application.Version = remoteApp.Version;
        //            application.VersionDate = DateTime.Parse(remoteApp.VersionDate, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
        //            application.ResourceFolder = string.Empty;
        //            application.Company = remoteApp.Company;
        //            application.ImageUri = remoteApp.ImageUrl;
        //            application.Executable = string.Empty;
        //            application.Arguments = string.Empty;

        //            application.SourceID = remoteApp.ID;
        //            application.SourceUrl = remoteApp.Url;
        //            application.Heading = remoteApp.Heading;
        //            application.Rating = remoteApp.Rating;

        //            //application.IsRemote = true;

        //            application.ID = Starcounter.Administrator.Server.Utilities.RestUtils.GetHashString(application.SourceUrl); // Use namespace+channel+version ?

        //            // Update IsDeployed status
        //            foreach (var downloadedApplication in downloadedApplications) {
        //                if (string.Equals(downloadedApplication.Namespace, application.Namespace, StringComparison.CurrentCultureIgnoreCase) &&
        //                    string.Equals(downloadedApplication.Channel, application.Channel, StringComparison.CurrentCultureIgnoreCase) &&
        //                    string.Equals(downloadedApplication.Version, application.Version, StringComparison.CurrentCultureIgnoreCase)) {
        //                    application.IsDeployed = true;
        //                    break;
        //                }
        //            }

        //            applications.Add(application);
        //        }
        //    }
        //    return true;
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="applications"></param>
        /// <returns></returns>
        //public static AppStoreApplication GetApplication(string appStoreHost, string id, out Response response) {

        //    IList<AppStoreApplication> applications;
        //    if (GetApplications(appStoreHost, out applications, out response) == false) {
        //        // TODO: Handle error
        //        return null;
        //    }

        //    foreach (AppStoreApplication application in applications) {
        //        if (application.ID == id) {
        //            return application;
        //        }
        //    }
        //    return null;
        //}

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
            application.AppName = string.Empty;
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
