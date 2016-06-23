using Administrator.Server.Managers;
using Administrator.Server.Model;
using Starcounter;
using Starcounter.Administrator.API.Handlers;
using Starcounter.Server.PublicModel;
using Starcounter.Server.Rest.Representations.JSON;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Administrator.Server.Managers {

    /// <summary>
    /// Responsable for:
    /// * Get list of applications
    /// * Start application
    /// * Stop application
    /// * Install application (add to playlist)
    /// * Uninstall application (remove from playlist)
    /// </summary>
    public class ApplicationManager {

        /// <summary>
        /// Start Executable
        /// </summary>
        /// <param name="databaseName"></param>
        /// <param name="path"></param>
        /// <param name="name"></param>
        /// <param name="resourceFolder"></param>
        /// <param name="arguments"></param>
        public static bool StartExecutable(string databaseName, string path, string name, string resourceFolder, string arguments, out Response response) {

            // TODO: Start database if it's not started

            if (!IsDatabaseRunning(databaseName)) {
                if (!StartDatabase(databaseName, out response)) {
                    return response.IsSuccessStatusCode;
                }
            }

            Executable executable = new Executable();
            executable.Path = path;
            executable.ApplicationFilePath = path;
            executable.Name = string.IsNullOrEmpty(name) ? Path.GetFileNameWithoutExtension(path) : name;
            executable.WorkingDirectory = resourceFolder; // TODO: Path can not end with "/"
            executable.AsyncEntrypoint = false;
            executable.TransactEntrypoint = false;

            // TODO: Arguments
            //Executable.ArgumentsElementJson arg = new Executable.ArgumentsElementJson();
            //arg.dummy = item.Executable;
            //executable.Arguments.Add(arg);

            //response = Node.LocalhostSystemPortNode.POST("/api/engines/" + databaseName.ToLower() + "/executables", executable.ToJsonUtf8(), null, 0);
            response = Self.POST("/api/engines/" + databaseName.ToLower() + "/executables", null, executable.ToJsonUtf8());
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Is Database/Engine running
        /// TODO: Move this to a Server Manager
        /// </summary>
        /// <param name="databaseName"></param>
        /// <returns></returns>
        static private bool IsDatabaseRunning(string databaseName) {

            Response response = Self.GET("/api/engines/" + databaseName);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Start database
        /// TODO: Move this to a Server Manager
        /// </summary>
        /// <param name="databaseName"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        static private bool StartDatabase(string databaseName, out Response response) {

            // $http.post('/api/engines', { Name: database.name }).then(function (response) {

            EngineCollection.EnginesElementJson item = new EngineCollection.EnginesElementJson();
            item.Name = databaseName.ToLower();

            response = Self.POST("/api/engines", null, item.ToJsonUtf8());
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Stop Executable
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        public static bool StopExecutable(string uri, out Response response) {

            response = Self.DELETE(uri, string.Empty);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Start Application
        /// </summary>
        /// <param name="application"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        public static bool StartApplication(DatabaseApplication application, out Response response) {

            Response tmpResponse;

            StartExecutable(application.DatabaseName,
                         application.Executable,
                         application.AppName,
                         application.ResourceFolder,
                         application.Arguments,
                         out tmpResponse);

            // Create response
            if (tmpResponse.IsSuccessStatusCode) {

                DatabaseApplication updatedApplication = ApplicationManager.GetApplication(application.DatabaseName, application.ID);

                DatabaseApplicationJson item = updatedApplication.ToDatabaseApplication();

                // Return updated applications
                response = new Response();
                response.StatusCode = (ushort)System.Net.HttpStatusCode.OK;
                response.BodyBytes = item.ToJsonUtf8();
            }
            else {
                // Return error response
                response = tmpResponse;
            }

            return tmpResponse.IsSuccessStatusCode;
        }

        /// <summary>
        /// Stop Application
        /// </summary>
        /// <param name="application"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        public static bool StopApplication(DatabaseApplication application, out Response response) {

            Response tmpResponse;

            AppInfo info = GetRunningAppInfo(application);
            // TODO: Handle error
            string uri = "/api/engines/default/executables/" + info.Key;

            StopExecutable(uri, out tmpResponse);

            // Create response
            if (tmpResponse.IsSuccessStatusCode) {

                response = new Response();

                DatabaseApplication updatedApplication = ApplicationManager.GetApplication(application.DatabaseName, application.ID);
                if (updatedApplication != null) {
                    DatabaseApplicationJson item = updatedApplication.ToDatabaseApplication();
                    response.BodyBytes = item.ToJsonUtf8();
                    response.StatusCode = (ushort)System.Net.HttpStatusCode.OK;
                }
                else {
                    response.StatusCode = (ushort)System.Net.HttpStatusCode.NoContent;
                }
            }
            else {
                // Return error response
                response = tmpResponse;
            }

            return tmpResponse.IsSuccessStatusCode;
        }

        /// <summary>
        /// Install Application
        /// </summary>
        /// <param name="application"></param>
        /// <param name="response"></param>
        public static void InstallApplication(DatabaseApplication application, out Response response) {


            PlaylistManager.InstallApplication(application);

            DatabaseApplication updatedApplication = GetApplication(application.DatabaseName, application.ID);

            DatabaseApplicationJson item = updatedApplication.ToDatabaseApplication();

            response = new Response();
            response.StatusCode = (ushort)System.Net.HttpStatusCode.OK;
            response.BodyBytes = item.ToJsonUtf8();
        }

        /// <summary>
        /// Install Application
        /// </summary>
        /// <param name="application"></param>
        /// <param name="response"></param>
        public static void InstallApplication(DatabaseApplication application) {

            PlaylistManager.InstallApplication(application);
        }

        /// <summary>
        /// Uninstall application
        /// </summary>
        /// <param name="application"></param>
        /// <param name="response"></param>
        public static void UninstallApplication(DatabaseApplication application, out Response response) {

            PlaylistManager.UninstallApplication(application);

            DatabaseApplication updatedApplication = GetApplication(application.DatabaseName, application.ID);

            DatabaseApplicationJson item = updatedApplication.ToDatabaseApplication();

            response = new Response();
            response.StatusCode = (ushort)System.Net.HttpStatusCode.OK;
            response.BodyBytes = item.ToJsonUtf8();
        }

        /// <summary>
        /// Uninstall application
        /// </summary>
        /// <param name="application"></param>
        /// <param name="response"></param>
        public static void UninstallApplication(DatabaseApplication application) {

            PlaylistManager.UninstallApplication(application);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="application"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        public static bool UpgradeApplication(DatabaseApplication application, out Response response) {

            // TODO:
            throw new NotImplementedException("UpgradeApplication not implemented");
        }

        /// <summary>
        /// Get applications (Installed, Deployed and runnings)
        /// Applications that can be started, stopped, installed, uninstalled and upgraded
        /// NOTE: Apps from some AppStore will *NOT* be included
        /// </summary>
        /// <param name="databaseName"></param>
        public static void GetApplications(string databaseName, out IList<DatabaseApplication> freshApplications) {

            freshApplications = new List<DatabaseApplication>();

            #region Deployed

            // Add Deployed items.
            // Items that has been deployed "local cached" in the server repository (ex. downloaded from appstore)
            IList<DeployedConfigFile> deployedItems = DeployManager.GetItems(databaseName);

            foreach (var item in deployedItems) {
                DatabaseApplication freshApplication = DatabaseApplication.ToApplication(item, databaseName);
                freshApplication.IsDeployed = true;
                //application.WantDeployed = true;
                freshApplications.Add(freshApplication);
            }

            #endregion

            #region Installed (playlist)

            // Add/Merge Installed items.
            // Items that has been added to the "playlist".
            Representations.JSON.Playlist playlist;
            PlaylistManager.GetInstalledApplications(databaseName, out playlist);

            // Deployed items
            foreach (var item in playlist.Deployed) {

                DatabaseApplication freshApplication = null;
                // Find deployed item
                // TODO: Create a separate method for this.
                foreach (var deployedItem in deployedItems) {
                    if (string.Equals(deployedItem.Namespace, item.Namespace, StringComparison.CurrentCultureIgnoreCase) &&
                        string.Equals(deployedItem.Channel, item.Channel, StringComparison.CurrentCultureIgnoreCase) &&
                        string.Equals(deployedItem.Version, item.Version, StringComparison.CurrentCultureIgnoreCase)
                        ) {

                        // Find deployed item
                        string deployedId = Starcounter.Administrator.Server.Utilities.RestUtils.GetHashString(databaseName + deployedItem.GetExecutableFullPath(DeployManager.GetRawDeployFolder(databaseName)));

                        freshApplication = GetApplication(deployedId, freshApplications);
                        if (freshApplication == null) {

                            freshApplication = DatabaseApplication.ToApplication(deployedItem, databaseName);
                            freshApplication.IsDeployed = true;
                            //application.WantDeployed = true;
                            freshApplications.Add(freshApplication);
                        }

                        freshApplication.IsInstalled = true;
                        //freshApplication.WantInstalled = true;
                        break;
                    }
                }

                if (freshApplication == null) {
                    // Deployed Playlist item was not found in the deployed list.
                    // Invalid playlist item, it points to a non existing application.
                    // TODO: Mark the application as invalid?, at the moment we will skipp it.
                }
            }

            // Local items (aka not deployed)
            foreach (var item in playlist.Local) {

                DatabaseApplication freshApplication = DatabaseApplication.ToApplication(item, databaseName);
                freshApplication.IsInstalled = true;
                //freshApplication.WantInstalled = true;
                freshApplications.Add(freshApplication);
            }

            #endregion

            #region Running

            // Get running apps
            DatabaseInfo databaseInfo = RootHandler.Host.Runtime.GetDatabaseByName(databaseName.ToLower());

            if (databaseInfo != null &&
                databaseInfo.Engine != null &&
                databaseInfo.Engine.HostProcessId != 0 &&
                databaseInfo.Engine.HostedApps != null) {

                DatabaseApplication freshApplication;
                foreach (AppInfo appInfo in databaseInfo.Engine.HostedApps) {
                    //bool bNew = false;
                    string runningApplicationID = Starcounter.Administrator.Server.Utilities.RestUtils.GetHashString(databaseName + Path.GetFullPath(appInfo.FilePath));
                    freshApplication = GetApplication(runningApplicationID, freshApplications);
                    if (freshApplication == null) {
                        //bNew = true;
                        freshApplication = DatabaseApplication.ToApplication(appInfo, databaseName);
                    }

                    freshApplications.Add(freshApplication);
                    freshApplication.IsRunning = true;
                    //if (bNew) {
                    //    freshApplication.WantRunning = true;
                    //}
                }
            }

            #endregion
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="applications"></param>
        /// <returns></returns>
        public static DatabaseApplication GetApplication(string id, IList<DatabaseApplication> applications) {

            foreach (DatabaseApplication application in applications) {
                if (application.ID == id) {
                    return application;
                }
            }
            return null;
        }

        /// <summary>
        /// Get application
        /// </summary>
        /// <param name="databaseName"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static DatabaseApplication GetApplication(string databaseName, string id) {

            IList<DatabaseApplication> items;

            // TODO: Instead of recreating the list we may use a cached list?
            GetApplications(databaseName, out items);

            foreach (var item in items) {
                if (item.ID == id) {
                    return item;
                }
            }

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="application"></param>
        /// <returns></returns>
        private static AppInfo GetRunningAppInfo(DatabaseApplication application) {

            // Get running apps
            DatabaseInfo databaseInfo = RootHandler.Host.Runtime.GetDatabaseByName(application.DatabaseName);

            if (databaseInfo == null ||
                databaseInfo.Engine == null ||
                databaseInfo.Engine.HostProcessId == 0 ||
                databaseInfo.Engine.HostedApps == null)
                return null;


            foreach (AppInfo appInfo in databaseInfo.Engine.HostedApps) {

                if (string.Equals(Path.GetFullPath(appInfo.FilePath), application.Executable, StringComparison.CurrentCultureIgnoreCase)) {
                    return appInfo;
                }
            }

            return null;
        }
    }
}
