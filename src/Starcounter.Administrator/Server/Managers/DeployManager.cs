using Administrator.Server.Managers;
using Administrator.Server.Model;
using Starcounter;
using Starcounter.Administrator.API.Handlers;
using Starcounter.Administrator.Server;
using Starcounter.Server.PublicModel;
using System;
using System.Collections;
using System.Collections.Concurrent;
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
        /// Retrive raw deploy root folder 
        /// (for generating app id)
        /// </summary>
        /// <returns></returns>
        public static string GetRawDeployFolder(string databaseName) {

            string databaseDirectory = RootHandler.Host.Runtime.GetServerInfo().Configuration.GetResolvedDatabaseDirectory();
            return Path.Combine(databaseDirectory, Path.Combine(databaseName, "apps"));
        }

        /// <summary>
        /// Retrive deploy root folder
        /// </summary>
        /// <returns></returns>
        public static string GetDeployFolder(string databaseName) {

            return GetRawDeployFolder(databaseName);

            //try {
            //    char? driveLetter = AssureMappingToAppsFolder(RootHandler.Host.Runtime.GetServerInfo().Configuration.DatabaseDirectory);
            //    return Path.Combine(driveLetter + ":\\", Path.Combine(databaseName, "apps"));
            //}
            //catch (Exception e) {
            //    Starcounter.Administrator.Server.Handlers.StarcounterAdminAPI.AdministratorLogSource.LogException(e);
            //    // Fallback folder
            //    return GetRawDeployFolder(databaseName);
            //}
        }


        /// <summary>
        /// Get Application folder
        /// </summary>
        /// <example>
        /// c:\Users\john\Documents\Starcounter\Personal\Databases\default\apps\StarcounterSamples.Launcher\Stable\
        /// </example>
        /// <param name="database"></param>
        /// <param name="nameSpace"></param>
        /// <param name="channel"></param>
        /// <returns></returns>
        public static string GetApplicationFolder(Database database, string nameSpace, string channel) {
            string appRootFolder = DeployManager.GetDeployFolder(database.ID);
            return Path.Combine(Path.Combine(appRootFolder, nameSpace), channel);
        }

        private static char? AppsDrive = null;
        private static char? AssureMappingToAppsFolder(string folder) {

            if (DeployManager.AppsDrive == null) {
                // No folder mapped yet

                // Check if we already have a mapping
                DriveInfo[] drives = DriveInfo.GetDrives();
                foreach (DriveInfo di in drives) {
                    string ps = Utilities.Subst.GetDriveMapping(di.Name[0]);

                    if (string.Equals(ps, folder, StringComparison.InvariantCultureIgnoreCase)) {
                        // Already mapped
                        return di.Name[0];
                    }
                }

                char? freeDriveLetter = Utilities.Subst.GetFreeDriveLetter();
                if (freeDriveLetter == null) {
                    throw new IndexOutOfRangeException("Could not find free drive letter to map to apps folder.");
                }
                Utilities.Subst.MapDrive((char)freeDriveLetter, folder);
                DeployManager.AppsDrive = freeDriveLetter;
            }
            else {
                string mappedFolder = Utilities.Subst.GetDriveMapping((char)DeployManager.AppsDrive);
                if (!string.Equals(mappedFolder, folder, StringComparison.InvariantCultureIgnoreCase)) {
                    // Remap
                    DeployManager.AppsDrive = null;
                    return AssureMappingToAppsFolder(folder);
                }
            }

            return DeployManager.AppsDrive;
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
        /// <param name="sourceUrl"></param>
        /// <param name="completionCallback"></param>
        /// <param name="errorCallback"></param>
        internal static void Download(string sourceUrl, Database database, bool throwErrorIfExist, Action<DatabaseApplication> completionCallback = null, Action<string> errorCallback = null) {

            // Check if app is already installed.
            DatabaseApplication databaseApplication = database.GetApplicationBySourceUrl(sourceUrl);
            if (databaseApplication != null) {

                if (throwErrorIfExist) {
                    if (errorCallback != null) {
                        errorCallback("Application already deployed.");
                    }
                    return;
                }

                // Application already downloaded.
                if (completionCallback != null) {
                    completionCallback(databaseApplication);
                }
                return;
            }

            DeployManager.DownloadPackage(sourceUrl, (data) => {

                DeployedConfigFile config = null;

                try {

                    using (MemoryStream packageZip = new MemoryStream(data)) {

                        string imageResourceFolder = System.IO.Path.Combine(Program.ResourceFolder, DeployManager.GetAppImagesFolder());

                        // Install package (Unzip)
                        PackageManager.Unpack(packageZip, sourceUrl, sourceUrl, DeployManager.GetDeployFolder(database.ID), imageResourceFolder, out config);

                        // Update server model
                        DatabaseApplication deployedApplication = DatabaseApplication.ToApplication(config, database.ID);
                        deployedApplication.IsDeployed = true;
                        database.Applications.Add(deployedApplication);
                        if (completionCallback != null) {
                            completionCallback(deployedApplication);
                        }
                    }
                }
                catch (InvalidOperationException e) {

                    if (throwErrorIfExist == false && config != null) {
                        // Find app
                        DatabaseApplication existingApplication = database.GetApplication(config.Namespace, config.Channel, config.Version);
                        if (existingApplication != null) {
                            if (completionCallback != null) {
                                completionCallback(existingApplication);
                            }
                            return;
                        }
                    }

                    if (errorCallback != null) {
                        errorCallback(e.Message);
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
                else if (resultCode == (ushort)System.Net.HttpStatusCode.NotFound) {
                    errorMessage += " " + "Application not found.";
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


                PlaylistManager.UninstallApplication(application);

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
            response = Http.GET(sourceUrl, headers, 30, opt);
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

            Http.GET(sourceUrl, headers, (Response response) => {

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
            }, 3600, opt);
        }
    }
}
