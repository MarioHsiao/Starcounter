using System;
using Codeplex.Data;
using Starcounter;
using Starcounter.Advanced;
using Starcounter.Server.PublicModel;
using System.Net;
using System.Diagnostics;
using Starcounter.Internal;
using Starcounter.Internal.Web;
using Starcounter.Administrator.API.Utilities;
using Starcounter.Administrator.API.Handlers;
using Starcounter.Server.Rest.Representations.JSON;
using Starcounter.Server.Rest;
using Starcounter.CommandLine;
using System.IO;
using Starcounter.Rest.ExtensionMethods;
using System.Collections.Generic;
using Starcounter.Administrator.Server.Utilities;
using System.Windows.Forms;
using System.Text;
using System.Reflection;
using System.ComponentModel;
using Administrator.Server.ApplicationContainer;

namespace Starcounter.Administrator.Server.Handlers {
    internal static partial class StarcounterAdminAPI {

        /// <summary>
        /// Register Application GET
        /// </summary>
        public static void InstalledApplication_POST(ushort port, string appsRootFolder, string appStoreHost, string imageResourceFolder) {

            //
            // Application Task
            //
            Handle.POST(port, "/api/admin/installed/task", (Request request) => {

                try {

                    Representations.JSON.ApplicationTask task = new Representations.JSON.ApplicationTask();
                    task.PopulateFromJson(request.Body);

                    if (string.Equals("Install", task.Type, StringComparison.InvariantCultureIgnoreCase)) {
                        // TODO: Able to use the local ID to get the sourceUrl.
                        return StarcounterAdminAPI.Install(task.SourceUrl, appsRootFolder, imageResourceFolder);
                    }
                    else if (string.Equals("Uninstall", task.Type, StringComparison.InvariantCultureIgnoreCase)) {

                        return StarcounterAdminAPI.UnInstall(task.ID, appsRootFolder, imageResourceFolder);
                    }
                    else if (string.Equals("Upgrade", task.Type, StringComparison.InvariantCultureIgnoreCase)) {
                        return StarcounterAdminAPI.Upgrade(port, task.ID, appsRootFolder, imageResourceFolder);
                    }
                    else if (string.Equals("Start", task.Type, StringComparison.InvariantCultureIgnoreCase)) {
                        return StarcounterAdminAPI.Start(task.ID, task.DatabaseName, task.Arguments, appsRootFolder);
                    }
                    else if (string.Equals("Stop", task.Type, StringComparison.InvariantCultureIgnoreCase)) {
                        return StarcounterAdminAPI.Stop(task.ID, appsRootFolder);
                    }

                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.BadRequest };
                }
                catch (InvalidOperationException e) {
                    ErrorResponse errorResponse = new ErrorResponse();
                    errorResponse.Text = e.Message;
                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.Forbidden, BodyBytes = errorResponse.ToJsonUtf8() };
                }
                catch (Exception e) {
                    return RestUtils.CreateErrorResponse(e);
                }

            });

            //
            // Install Application 
            //
            //Handle.POST(port, "/api/admin/installed/apps", (Request request) => {

            //    try {
            //        return StarcounterAdminAPI.InstallOLD(request, appsRootFolder, appStoreHost);
            //    }
            //    catch (Exception e) {
            //        return RestUtils.CreateErrorResponse(e);
            //    }

            //});


        }

        /// <summary>
        /// Install application from sourceUrl
        /// </summary>
        /// <param name="sourceUrl"></param>
        /// <param name="appsRootFolder"></param>
        /// <returns></returns>
        internal static Response Install(string sourceUrl, string appsRootFolder, string imageResourceFolder) {

            try {

                // Download Application from AppStore host
                byte[] packageData;
                HttpStatusCode resultCode = DownloadPackage(sourceUrl, out packageData);
                if (packageData != null) {
                    // Success
                    using (MemoryStream packageZip = new MemoryStream(packageData)) {
                        AppConfig config;
                        Package.Install(packageZip, sourceUrl, appsRootFolder, imageResourceFolder, out config);
                        return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.Created };
                    }
                }
                else {
                    ErrorResponse errorResponse = new ErrorResponse();
                    errorResponse.Text = string.Format("Failed to install application.");

                    if (resultCode == System.Net.HttpStatusCode.ServiceUnavailable) {
                        errorResponse.Text += " " + "Service Unavailable.";
                    }
                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.ServiceUnavailable, BodyBytes = errorResponse.ToJsonUtf8() };
                }

            }
            catch (InvalidOperationException e) {
                ErrorResponse errorResponse = new ErrorResponse();
                errorResponse.Text = e.Message;
                return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.Forbidden, BodyBytes = errorResponse.ToJsonUtf8() };
            }
        }

        /// <summary>
        /// Upgrade installed application
        /// </summary>
        /// <param name="sourceUrl"></param>
        /// <param name="appsRootFolder"></param>
        /// <returns></returns>
        internal static Response Upgrade(ushort port, string id, string appsRootFolder, string imageResourceFolder) {

            try {

                // Get latest version of app
                string uri = "http://127.0.0.1:" + port + "/api/admin/appstore/apps/" + id;
                Response response;
                X.GET(uri, out response, null, 10000);
                if (response.StatusCode != (ushort)System.Net.HttpStatusCode.OK) {
                    return new Response() { StatusCode = response.StatusCode, BodyBytes = response.BodyBytes };
                }

                Representations.JSON.AppStoreApplication appStoreItem = new Representations.JSON.AppStoreApplication();
                appStoreItem.PopulateFromJson(response.Body);

                if (appStoreItem.NewVersionAvailable == false) {
                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NotModified };
                }

                AppConfig installedConfig;
                AppsContainer.GetInstalledApp(id, appsRootFolder, out installedConfig);

                if (installedConfig == null) {
                    throw new InvalidOperationException("Failed to upgrade, Can not get installed application");
                }

                // Download latest version
                byte[] packageData;
                HttpStatusCode resultCode = DownloadPackage(appStoreItem.LatestVersion.Url, out packageData);
                if (packageData == null) {
                    // TODO: Handle original error message
                    ErrorResponse errorResponse = new ErrorResponse();
                    errorResponse.Text = string.Format("Failed to install application.");

                    if (resultCode == System.Net.HttpStatusCode.ServiceUnavailable) {
                        errorResponse.Text += " " + "Service Unavailable.";
                    }
                    return new Response() { StatusCode = (ushort)resultCode, BodyBytes = errorResponse.ToJsonUtf8() };
                }
                using (MemoryStream packageZip = new MemoryStream(packageData)) {
                    AppConfig newInstalledConfig;
                    Package.Upgrade(installedConfig, appStoreItem.LatestVersion.Url, packageZip, appsRootFolder, imageResourceFolder, out newInstalledConfig);
                }
                //AppsContainer.UnInstall(installedConfig);
                return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.Created };
            }
            catch (InvalidOperationException e) {
                ErrorResponse errorResponse = new ErrorResponse();
                errorResponse.Text = e.Message;
                return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.Forbidden, BodyBytes = errorResponse.ToJsonUtf8() };
            }
        }

        /// <summary>
        /// UnInstall installed application
        /// </summary>
        /// <param name="sourceUrl"></param>
        /// <param name="appsRootFolder"></param>
        /// <returns></returns>
        internal static Response UnInstall(string id, string appsRootFolder, string imageResourceFolder) {

            try {

                AppConfig installedConfig;
                AppsContainer.GetInstalledApp(id, appsRootFolder, out installedConfig);

                if (installedConfig == null) {
                    throw new InvalidOperationException("Failed to upgrade, Can not get installed application");
                }

                AppsContainer.UnInstall(installedConfig, imageResourceFolder);
                return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NoContent };
            }
            catch (InvalidOperationException e) {
                ErrorResponse errorResponse = new ErrorResponse();
                errorResponse.Text = e.Message;
                return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.Forbidden, BodyBytes = errorResponse.ToJsonUtf8() };
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="databaseName"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        internal static Response Start(string id, string databaseName, string arguments, string appsRootFolder) {

            AppConfig installedConfig;
            AppsContainer.GetInstalledApp(id, appsRootFolder, out installedConfig);

            if (installedConfig == null) {
                throw new InvalidOperationException("Failed to start, Can not get installed application");
            }

            // Start engine
            string startEngineUri = "/api/engines";
            Starcounter.Server.Rest.Representations.JSON.EngineCollection.EnginesElementJson engineBody = new EngineCollection.EnginesElementJson();
            engineBody.Name = databaseName;
            engineBody.NoDb = false;
            engineBody.LogSteps = false;

            Response response = X.POST(startEngineUri, engineBody.ToJsonUtf8(), null);
            if (response.StatusCode >= 200 && response.StatusCode < 300) {
                // OK
                // Start app

                string startAppUri = "/api/engines/" + databaseName + "/executables";
                Starcounter.Server.Rest.Representations.JSON.Executable body = new Executable();

                // exe.Name, exe.ApplicationFilePath, exe.Path, exe.WorkingDirectory, userArgs, exe.StartedBy
                //body.Name = installedConfig.DisplayName;
                body.StartedBy = "Starcounter Administrator";

                // Executable
                string appExe = Path.Combine(appsRootFolder, installedConfig.Namespace);
                appExe = Path.Combine(appExe, installedConfig.Channel);
                appExe = Path.Combine(appExe, installedConfig.Version);
                appExe = Path.Combine(appExe, installedConfig.Executable);

                string executablePath = appExe.Replace('/', '\\'); // TODO: Fix this when config is verifie

                if (string.IsNullOrEmpty(installedConfig.AppName)) {
                    body.Name = Path.GetFileNameWithoutExtension(executablePath);
                }
                else {
                    body.Name = installedConfig.AppName;
                }

                body.Path = executablePath;
                body.ApplicationFilePath = executablePath;

                body.IsTool = true;

                // Resource folder
                string appResourcFolder = Path.Combine(appsRootFolder, installedConfig.Namespace);
                appResourcFolder = Path.Combine(appResourcFolder, installedConfig.Channel);
                appResourcFolder = Path.Combine(appResourcFolder, installedConfig.Version);
                appResourcFolder = Path.Combine(appResourcFolder, installedConfig.ResourceFolder);
                appResourcFolder = appResourcFolder.Replace('/', '\\');  // TODO: Fix this when config is verified
                string resourceFolder = appResourcFolder;
                body.WorkingDirectory = resourceFolder;

                response = X.POST(startAppUri, body.ToJsonUtf8(), null);
                if (response.StatusCode >= 200 && response.StatusCode < 300) {
                    // OK
                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.OK };
                }
            }

            // Error
            return new Response() { StatusCode = response.StatusCode, BodyBytes = response.BodyBytes };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        internal static Response Stop(string id, string appsRootFolder) {
            return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NoContent };
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
            string headers = "Accept: application/octet-stream\r\n";
            X.GET(sourceUrl, out response, headers, 0, opt);
            if (response.StatusCode >= 200 && response.StatusCode < 300) {

                data = response.BodyBytes;
            }
            else {
                data = null;
                // Error
            }

            return (HttpStatusCode)response.StatusCode;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="replace"></param>
        /// <returns></returns>
        //internal static Response InstallOLD(Request request, string appsRootFolder, string appStoreHost) {

        //    try {
        //        string host = request["Host"];

        //        if (string.IsNullOrEmpty(appStoreHost)) {
        //            ErrorResponse errorResponse = new ErrorResponse();
        //            errorResponse.Text = string.Format("Configuration error, Unknown App Store host");
        //            return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.ServiceUnavailable, BodyBytes = errorResponse.ToJsonUtf8() };
        //        }

        //        //Representations.JSON.Application appStoreApplication = new Representations.JSON.Application();
        //        //appStoreApplication.PopulateFromJson(request.Body);
        //        Representations.JSON.ApplicationTask task = new Representations.JSON.ApplicationTask();
        //        task.PopulateFromJson(request.Body);

        //        // Download Application from AppStore host
        //        string url = appStoreHost + "/appstore/apps/" + task.InstallID;

        //        Response response;
        //        HandlerOptions opt = new HandlerOptions() { CallExternalOnly = true };

        //        // Get package from host
        //        string headers = "Accept: application/octet-stream\r\n";
        //        X.GET(url, out response, headers, 0, opt);
        //        if (response.StatusCode >= 200 && response.StatusCode < 300) {

        //            // Success
        //            using (MemoryStream packageZip = new MemoryStream(response.BodyBytes)) {
        //                AppConfig config;

        //                if (!string.IsNullOrEmpty(task.UninstallID)) {
        //                    // Upgrade/replace existing version
        //                    AppConfig installedConfig;
        //                    AppsContainer.GetInstalledApp(task.UninstallID, appsRootFolder, out installedConfig);

        //                    if (installedConfig == null) {
        //                        throw new InvalidOperationException("Failed to upgrade, Can not get installed application");
        //                    }

        //                    Package.Upgrade(installedConfig, url, packageZip, appsRootFolder, out config);
        //                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.OK };
        //                }
        //                else {
        //                    Package.Install(packageZip, url, appsRootFolder, out config);
        //                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.Created };
        //                }
        //            }
        //        }
        //        else {
        //            // Error

        //            ErrorResponse errorResponse = new ErrorResponse();
        //            errorResponse.Text = string.Format("Failed to {0} application", string.IsNullOrEmpty(task.UninstallID) ? "install" : "upgrade");

        //            if (response.StatusCode == (ushort)System.Net.HttpStatusCode.ServiceUnavailable) {
        //                errorResponse.Text += ", " + "Service Unavailable";
        //            }
        //            return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.ServiceUnavailable, BodyBytes = errorResponse.ToJsonUtf8() };
        //        }
        //    }
        //    catch (InvalidOperationException e) {
        //        ErrorResponse errorResponse = new ErrorResponse();
        //        errorResponse.Text = e.Message;
        //        return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.Forbidden, BodyBytes = errorResponse.ToJsonUtf8() };
        //    }
        //}
    }
}
