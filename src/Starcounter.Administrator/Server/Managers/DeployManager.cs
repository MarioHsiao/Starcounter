﻿using Administrator.Server.Managers;
using Administrator.Server.Model;
using Starcounter;
using Starcounter.Administrator.API.Handlers;
using Starcounter.Administrator.Server;
using Starcounter.Server.PublicModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Administrator.Server.Managers {

    /// <summary>
    /// Responsable for:
    /// * Download packages
    /// * Delete deployed (downloaded) packages
    /// * Get a list of deployed applications
    /// 
    /// When an application is deployed on the server it's copied and unpacked to a subfolder in the database repository folder
    /// </summary>
    public class DeployManager {

        /// <summary>
        /// Retrives application that is deployed on the server (downloaded from Appstore, or locally deployed)
        /// The deployed applications is a subfolder named "apps" in the database folder
        /// </summary>
        public static IList<DeployedConfigFile> GetItems(string databaseName) {
            // TODO: Move Appcontainer layer to DeployManager
            return PackageManager.GetInstallApps(GetDeployFolder(databaseName));
        }

        /// <summary>
        /// Get item
        /// </summary>
        /// <param name="application"></param>
        /// <returns></returns>
        public static DeployedConfigFile GetItemFromApplication(DatabaseApplication application) {

            IList<DeployedConfigFile> items = DeployManager.GetItems(application.DatabaseName);
            foreach (var item in items) {

                // TODO: Is this a safe way to get the application?
                if (string.Equals(item.Namespace, application.Namespace, StringComparison.CurrentCultureIgnoreCase) &&
                                   string.Equals(item.Channel, application.Channel, StringComparison.CurrentCultureIgnoreCase) &&
                                   string.Equals(item.Version, application.Version, StringComparison.CurrentCultureIgnoreCase)) {

                    return item;
                }
            }
            return null;
        }

        /// <summary>
        /// Retrive deploy root folder
        /// </summary>
        /// <returns></returns>
        public static string GetDeployFolder(string databaseName) {

            string databaseDirectory = RootHandler.Host.Runtime.GetServerInfo().Configuration.GetResolvedDatabaseDirectory();
            return Path.Combine(databaseDirectory, Path.Combine(databaseName, "apps"));
        }

        /// <summary>
        /// Deployed image shared folder
        /// </summary>
        /// <returns></returns>
        public static string GetAppImagesFolder() {
            return "appImages";
        }

        /// <summary>
        /// Dowload package
        /// </summary>
        /// <param name="application"></param>
        /// <param name="completionCallback"></param>
        /// <param name="errorCallback"></param>
        internal static void Download(AppStoreApplication application, Action<DatabaseApplication> completionCallback = null, Action<string> errorCallback = null) {

            DeployManager.DownloadPackage(application.SourceUrl, (data) => {

                try {

                    using (MemoryStream packageZip = new MemoryStream(data)) {
                        DeployedConfigFile config;

                        string imageResourceFolder = System.IO.Path.Combine(Program.ResourceFolder, DeployManager.GetAppImagesFolder());

                        // Install package (Unzip)
                        PackageManager.Unpack(packageZip, application.SourceUrl, application.StoreUrl,  DeployManager.GetDeployFolder(application.DatabaseName), imageResourceFolder, out config);

                        // Update server modelF
                        DatabaseApplication deployedApplication = DatabaseApplication.ToApplication(config, application.DatabaseName);
                        deployedApplication.IsDeployed = true;
                        application.Database.Applications.Add(deployedApplication);
                        if (completionCallback != null) {
                            completionCallback(deployedApplication);
                        }
                    }
                }
                catch (Exception e) {

                    if (errorCallback != null) {
                        errorCallback(e.Message);
                    }
                }

            }, (resultCode, resultBody) => {

                string errorMessage = string.Format("Failed to download application.");

                if (resultCode == (ushort)System.Net.HttpStatusCode.ServiceUnavailable) {
                    errorMessage += " " + "Service Unavailable.";
                }

                if (errorCallback != null) {
                    errorCallback(errorMessage);
                }
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="application">AppStore Application</param>
        /// <param name="completionCallback"></param>
        /// <param name="errorCallback"></param>
        internal static void Delete(DatabaseApplication application, bool force, Action<DatabaseApplication> completionCallback = null, Action<string> errorCallback = null) {

            try {

                if (force == false && application.CanBeUninstalled == false) {
                    if (errorCallback != null) {
                        errorCallback("Can not delete locked application");
                    }
                    return;
                }

                if (application.IsRunning) {
                    if (errorCallback != null) {
                        errorCallback("Can not delete running application");
                    }
                    return;
                }

                DeployedConfigFile config = DeployManager.GetItemFromApplication(application);

                if (config == null) {
                    if (errorCallback != null) {
                        errorCallback("Failed to delete application, Could not find deployed application");
                    }
                    return;
                }

                string imageResourceFolder = System.IO.Path.Combine(Program.ResourceFolder, DeployManager.GetAppImagesFolder());

                PackageManager.Delete(config, imageResourceFolder);
                application.IsDeployed = false;

                application.Database.Applications.Remove(application);

                if (completionCallback != null) {
                    completionCallback(application);
                }
            }
            catch (InvalidOperationException e) {

                if (errorCallback != null) {
                    errorCallback(e.Message);
                }
            }
        }

        /// <summary>
        /// Download package from Url
        /// </summary>
        /// <param name="sourceUrl"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private static HttpStatusCode DownloadPackage(string sourceUrl, out byte[] data) {

            Response response;
            HandlerOptions opt = new HandlerOptions() { CallExternalOnly = true };

            // Get package from host
            Dictionary<String, String> headers = new Dictionary<String, String> { { "Accept", "application/octet-stream" } };
            response = Http.GET(sourceUrl, headers, 30000, opt);
            if (response.IsSuccessStatusCode) {
                data = response.BodyBytes;
            }
            else {
                data = null;
                // Error
            }

            return (HttpStatusCode)response.StatusCode;
        }

        /// <summary>
        /// Download package from Url
        /// </summary>
        /// <param name="sourceUrl"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private static void DownloadPackage(string sourceUrl, Action<byte[]> completionCallback = null, Action<ushort, string> errorCallback = null) {

            HandlerOptions opt = new HandlerOptions() { CallExternalOnly = true };

            // Get package from host
            Dictionary<String, String> headers = new Dictionary<String, String> { { "Accept", "application/octet-stream" } };

            Http.GET(sourceUrl, headers, null, (Response response, Object userObject) => {

                if (response.IsSuccessStatusCode) {

                    if (completionCallback != null) {
                        completionCallback(response.BodyBytes);
                    }
                }
                else {
                    if (errorCallback != null) {
                        errorCallback(response.StatusCode, response.Body);
                    }
                }
            }, 1000 * 60 * 60, opt);
        }
    }
}
