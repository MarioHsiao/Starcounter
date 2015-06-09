//using System;
//using Codeplex.Data;
//using Starcounter;
//using Starcounter.Advanced;
//using Starcounter.Server.PublicModel;
//using System.Net;
//using System.Diagnostics;
//using Starcounter.Internal;
//using Starcounter.Internal.Web;
//using Starcounter.Administrator.API.Utilities;
//using Starcounter.Administrator.API.Handlers;
//using Starcounter.Server.Rest.Representations.JSON;
//using Starcounter.Server.Rest;
//using Starcounter.CommandLine;
//using System.IO;
//using Starcounter.Rest.ExtensionMethods;
//using System.Collections.Generic;
//using Starcounter.Administrator.Server.Utilities;
//using System.Windows.Forms;
//using System.Text;
//using System.Reflection;
//using System.ComponentModel;
//using Administrator.Server.Managers;
//using Administrator.Server.Model;

//namespace Starcounter.Administrator.Server.Handlers {
//    internal static partial class StarcounterAdminAPI {

//        /// <summary>
//        /// Register Application GET
//        /// </summary>
//        public static void InstalledApplicationTask_POST(ushort port, string appsRootFolder, string appStoreHost, string imageResourceFolder) {

//            //
//            // Application Task
//            //
//            Handle.POST(port, "/api/admin/installed/task", (Request request) => {

//                try {

//                    Representations.JSON.ApplicationTask task = new Representations.JSON.ApplicationTask();
//                    task.PopulateFromJson(request.Body);

//                    if (string.Equals("Install", task.Type, StringComparison.InvariantCultureIgnoreCase)) {
//                        // TODO: Able to use the local ID to get the sourceUrl.
//                        return StarcounterAdminAPI.Install(task.SourceUrl, appsRootFolder, imageResourceFolder);
//                    }
//                    else if (string.Equals("Uninstall", task.Type, StringComparison.InvariantCultureIgnoreCase)) {

//                        return StarcounterAdminAPI.UnInstall(task.ID, appsRootFolder, imageResourceFolder);
//                    }
//                    else if (string.Equals("Upgrade", task.Type, StringComparison.InvariantCultureIgnoreCase)) {
//                        return StarcounterAdminAPI.Upgrade(port, task.ID, appsRootFolder, imageResourceFolder);
//                    }
//                    else if (string.Equals("Start", task.Type, StringComparison.InvariantCultureIgnoreCase)) {
//                        return StarcounterAdminAPI.Start(task.ID, task.DatabaseName, task.Arguments, appsRootFolder);
//                    }
//                    else if (string.Equals("Stop", task.Type, StringComparison.InvariantCultureIgnoreCase)) {
//                        return StarcounterAdminAPI.Stop(task.ID, task.DatabaseName, appsRootFolder);
//                    }

//                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.BadRequest };
//                }
//                catch (InvalidOperationException e) {
//                    ErrorResponse errorResponse = new ErrorResponse();
//                    errorResponse.Text = e.Message;
//                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.Forbidden, BodyBytes = errorResponse.ToJsonUtf8() };
//                }
//                catch (Exception e) {
//                    return RestUtils.CreateErrorResponse(e);
//                }

//            });

//        }

//        /// <summary>
//        /// Install application from sourceUrl
//        /// </summary>
//        /// <param name="sourceUrl"></param>
//        /// <param name="appsRootFolder"></param>
//        /// <returns></returns>
//        internal static Response Install(string sourceUrl, string appsRootFolder, string imageResourceFolder) {

//            try {

//                // Download Application from AppStore host
//                byte[] packageData;
//                HttpStatusCode resultCode = DownloadPackage(sourceUrl, out packageData);
//                if (packageData != null) {
//                    // Success
//                    using (MemoryStream packageZip = new MemoryStream(packageData)) {
//                        DeployedConfigFile config;
//                        PackageManager.Unpack(packageZip, sourceUrl, appsRootFolder, imageResourceFolder, out config);
//                        return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.Created };
//                    }
//                }
//                else {
//                    ErrorResponse errorResponse = new ErrorResponse();
//                    errorResponse.Text = string.Format("Failed to install application.");

//                    if (resultCode == System.Net.HttpStatusCode.ServiceUnavailable) {
//                        errorResponse.Text += " " + "Service Unavailable.";
//                    }
//                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.ServiceUnavailable, BodyBytes = errorResponse.ToJsonUtf8() };
//                }

//            }
//            catch (InvalidOperationException e) {
//                ErrorResponse errorResponse = new ErrorResponse();
//                errorResponse.Text = e.Message;
//                return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.Forbidden, BodyBytes = errorResponse.ToJsonUtf8() };
//            }
//        }


//        /// <summary>
//        /// Upgrade installed application
//        /// </summary>
//        /// <param name="port"></param>
//        /// <param name="id">Installed app ID</param>
//        /// <param name="appsRootFolder"></param>
//        /// <param name="imageResourceFolder"></param>
//        /// <returns></returns>
//        internal static Response Upgrade(ushort port, string id, string appsRootFolder, string imageResourceFolder) {

//            try {

//                // Get installed app
//                DeployedConfigFile installedConfig;
//                PackageManager.GetInstalledApp(id, appsRootFolder, out installedConfig);

//                if (installedConfig == null) {
//                    throw new InvalidOperationException("Failed to upgrade, Can not get previously installed application");
//                }

//                // Get appstore  app
//                Representations.JSON.AppStoreApplication appStoreItem = GetAppStoreItem(port, id);
//                if (appStoreItem == null) {
//                    // App Store Service down, App item removed from appstore, ..
//                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NotFound };
//                }


//                //// Get latest version of app
//                //string uri = "http://127.0.0.1:" + port + "/api/admin/appstore/apps/" + id;
//                //Response response;
//                //X.GET(uri, out response, null, 10000);
//                //if (response.StatusCode != (ushort)System.Net.HttpStatusCode.OK) {
//                //    return new Response() { StatusCode = response.StatusCode, BodyBytes = response.BodyBytes };
//                //}

//                //Representations.JSON.AppStoreApplication appStoreItem = new Representations.JSON.AppStoreApplication();
//                //appStoreItem.PopulateFromJson(response.Body);

//                if (appStoreItem.NewVersionAvailable == false) {
//                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NotModified };
//                }


//                // Download latest version
//                byte[] packageData;
//                HttpStatusCode resultCode = DownloadPackage(appStoreItem.LatestVersion.Url, out packageData);
//                if (packageData == null) {
//                    // TODO: Handle original error message
//                    ErrorResponse errorResponse = new ErrorResponse();
//                    errorResponse.Text = string.Format("Failed to install application.");

//                    if (resultCode == System.Net.HttpStatusCode.ServiceUnavailable) {
//                        errorResponse.Text += " " + "Service Unavailable.";
//                    }
//                    return new Response() { StatusCode = (ushort)resultCode, BodyBytes = errorResponse.ToJsonUtf8() };
//                }
//                using (MemoryStream packageZip = new MemoryStream(packageData)) {
//                    DeployedConfigFile newInstalledConfig;
//                    PackageManager.Upgrade(installedConfig, appStoreItem.LatestVersion.Url, packageZip, appsRootFolder, imageResourceFolder, out newInstalledConfig);
//                }
//                //AppsContainer.UnInstall(installedConfig);
//                return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.Created };
//            }
//            catch (InvalidOperationException e) {
//                ErrorResponse errorResponse = new ErrorResponse();
//                errorResponse.Text = e.Message;
//                return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.Forbidden, BodyBytes = errorResponse.ToJsonUtf8() };
//            }
//        }


//        private static Representations.JSON.AppStoreApplication GetAppStoreItem(ushort port, string id ) {

//            // Get items
//            string uri = "http://127.0.0.1:" + port + "/api/admin/appstore/apps";
//            Response response;
//            X.GET(uri, out response, null, 10000);
//            if (response.StatusCode != (ushort)System.Net.HttpStatusCode.OK) {
//                return null;
//            }

//            Representations.JSON.AppStoreApplications appStoreItems = new Representations.JSON.AppStoreApplications();
//            appStoreItems.PopulateFromJson(response.Body);

//            foreach (var appStore in appStoreItems.Stores) {

//                foreach (var appStoreItem in appStore.Items) {

//                    if (appStoreItem.ID == id) {
//                        return appStoreItem;
//                    }
//                }
//            }
//            return null;
//        }

//        /// <summary>
//        /// UnInstall installed application
//        /// </summary>
//        /// <param name="id">Installed app ID</param>
//        /// <param name="appsRootFolder"></param>
//        /// <param name="imageResourceFolder"></param>
//        /// <returns></returns>
//        internal static Response UnInstall(string id, string appsRootFolder, string imageResourceFolder) {

//            try {

//                DeployedConfigFile installedConfig;
//                PackageManager.GetInstalledApp(id, appsRootFolder, out installedConfig);

//                if (installedConfig == null) {
//                    throw new InvalidOperationException("Failed to uninstall, Can not get installed application");
//                }

//                PackageManager.Delete(installedConfig, imageResourceFolder);
//                return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NoContent };
//            }
//            catch (InvalidOperationException e) {
//                ErrorResponse errorResponse = new ErrorResponse();
//                errorResponse.Text = e.Message;
//                return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.Forbidden, BodyBytes = errorResponse.ToJsonUtf8() };
//            }
//        }

//        /// <summary>
//        /// 
//        /// </summary>
//        /// <param name="id"></param>
//        /// <param name="databaseName"></param>
//        /// <param name="arguments"></param>
//        /// <returns></returns>
//        internal static Response Start(string id, string databaseName, string arguments, string appsRootFolder) {

//            DeployedConfigFile installedConfig;
//            PackageManager.GetInstalledApp(id, appsRootFolder, out installedConfig);

//            if (installedConfig == null) {
//                throw new InvalidOperationException("Failed to start, Can not get installed application");
//            }

//            // Start engine
//            string startEngineUri = "/api/engines";
//            Starcounter.Server.Rest.Representations.JSON.EngineCollection.EnginesElementJson engineBody = new EngineCollection.EnginesElementJson();
//            engineBody.Name = databaseName;
//            engineBody.NoDb = false;
//            engineBody.LogSteps = false;

//            Response response = X.POST(startEngineUri, engineBody.ToJsonUtf8(), null);
//            if (response.StatusCode >= 200 && response.StatusCode < 300) {
//                // OK
//                // Start app

//                string startAppUri = "/api/engines/" + databaseName + "/executables";
//                Starcounter.Server.Rest.Representations.JSON.Executable body = new Executable();

//                // exe.Name, exe.ApplicationFilePath, exe.Path, exe.WorkingDirectory, userArgs, exe.StartedBy
//                //body.Name = installedConfig.DisplayName;
//                body.StartedBy = "Starcounter Administrator";

//                // Executable
//                string appExe = Path.Combine(appsRootFolder, installedConfig.Namespace);
//                appExe = Path.Combine(appExe, installedConfig.Channel);
//                appExe = Path.Combine(appExe, installedConfig.Version);
//                appExe = Path.Combine(appExe, installedConfig.Executable);

//                string executablePath = appExe.Replace('/', '\\'); // TODO: Fix this when config is verifie

//                if (string.IsNullOrEmpty(installedConfig.AppName)) {
//                    body.Name = Path.GetFileNameWithoutExtension(executablePath);
//                }
//                else {
//                    body.Name = installedConfig.AppName;
//                }

//                body.Path = executablePath;
//                body.ApplicationFilePath = executablePath;

//                body.IsTool = true;

//                // Resource folder
//                string appResourcFolder = Path.Combine(appsRootFolder, installedConfig.Namespace);
//                appResourcFolder = Path.Combine(appResourcFolder, installedConfig.Channel);
//                appResourcFolder = Path.Combine(appResourcFolder, installedConfig.Version);
//                appResourcFolder = Path.Combine(appResourcFolder, installedConfig.ResourceFolder);
//                appResourcFolder = appResourcFolder.Replace('/', '\\');  // TODO: Fix this when config is verified
//                string resourceFolder = appResourcFolder;
//                body.WorkingDirectory = resourceFolder;

//                response = X.POST(startAppUri, body.ToJsonUtf8(), null);
//                if (response.StatusCode >= 200 && response.StatusCode < 300) {
//                    // OK
//                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.OK };
//                }
//            }

//            // Error
//            return new Response() { StatusCode = response.StatusCode, BodyBytes = response.BodyBytes };
//        }

//        /// <summary>
//        /// 
//        /// </summary>
//        /// <param name="id"></param>
//        /// <returns></returns>
//        internal static Response Stop(string id, string databaseName, string appsRootFolder) {


//            DeployedConfigFile installedConfig;
//            PackageManager.GetInstalledApp(id, appsRootFolder, out installedConfig);

//            if (installedConfig == null) {
//                throw new InvalidOperationException("Failed to stop, Can not find installed application");
//            }

//            // Find running executable
//            string stopAppUri = GetRunningAppUri(installedConfig, databaseName, appsRootFolder);
//            if (stopAppUri == null) {
//                throw new InvalidOperationException("Failed to stop, Can not find running application");
//            }

//            Response response = X.DELETE(stopAppUri, string.Empty, null);
//            if (response.StatusCode >= 200 && response.StatusCode < 300) {
//                // OK
//                return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.OK };
//            }

//            // Error
//            return new Response() { StatusCode = response.StatusCode, BodyBytes = response.BodyBytes };
//        }

//        /// <summary>
//        /// Get running installed App
//        /// </summary>
//        /// <param name="installedConfig"></param>
//        /// <param name="databaseName"></param>
//        /// <param name="appsRootFolder"></param>
//        /// <returns></returns>
//        private static string GetRunningAppUri(DeployedConfigFile installedConfig, string databaseName, string appsRootFolder) {

//            string exeFile = StarcounterAdminAPI.BuildAppExecutablePath(installedConfig, appsRootFolder);

//            // Find running executable
//            Response runningResponse = Node.LocalhostSystemPortNode.GET("/api/admin/applications");

//            Executables runningExecutables = new Executables();
//            runningExecutables.PopulateFromJson(runningResponse.Body);

//            string stopAppUri = string.Empty;
//            foreach (var runningExe in runningExecutables.Items) {
//                if (string.Equals(runningExe.Path, exeFile, StringComparison.InvariantCultureIgnoreCase)) {
//                    if (runningExe.Engine.Uri.EndsWith("/" + databaseName, StringComparison.InvariantCultureIgnoreCase)) {
//                        return runningExe.Uri;
//                    }
//                }
//            }
//            return null;
//        }

//        /// <summary>
//        /// Download package from Url
//        /// </summary>
//        /// <param name="sourceUrl"></param>
//        /// <param name="data"></param>
//        /// <returns></returns>
//        private static HttpStatusCode DownloadPackage(string sourceUrl, out byte[] data) {

//            Response response;
//            HandlerOptions opt = new HandlerOptions() { CallExternalOnly = true };

//            // Get package from host
//            Dictionary<String, String> headers = new Dictionary<String, String> { { "Accept", "application/octet-stream" } };
//            X.GET(sourceUrl, out response, headers, 0, opt);
//            if (response.StatusCode >= 200 && response.StatusCode < 300) {

//                data = response.BodyBytes;
//            }
//            else {
//                data = null;
//                // Error
//            }

//            return (HttpStatusCode)response.StatusCode;
//        }
//    }
//}
