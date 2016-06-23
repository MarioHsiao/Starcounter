using Starcounter;
using Starcounter.Internal;
using System;
using Server.API;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter.Logging;
using System.Net;
using Starcounter.Administrator.Server;

namespace Server.API {
    public class RestHandlers {

        public static LogSource ServerRestApiSource = new LogSource("ServerAPI");

        /// <summary>
        /// Register Server REST API
        /// </summary>
        /// <remarks>
        /// It will reroute the incoming request to the internal starcounter api and return it's result
        /// </remarks>
        public static void Register() {


            RestSettings settings = RestHandlers.GetSettings();
            if (settings == null || !settings.Enabled) return;
            ushort port = (ushort)settings.Port;

            HandlerOptions opt = new HandlerOptions() { SkipHandlersPolicy = false, SkipRequestFilters = true };
            try {

                #region Tasks
                // Get task
                Handle.GET(port, "/serverapi/tasks/{?}", (string taskId, Request request) => {

                    Response response;
                    if (!Authentication.Authenticate(settings, request, out response)) { return response; }
                    return Self.GET(string.Format("/api/tasks/{0}", taskId));
                }, opt);

                // Initiate task, create database
                Handle.POST(port, "/serverapi/tasks/createdatabase", (Request request) => {

                    Response response;
                    if (!Authentication.Authenticate(settings, request, out response)) { return response; }
                    return Self.POST("/api/tasks/createdatabase", null, request.BodyBytes, null);
                }, opt);

                // Initiate task, delete database
                Handle.POST(port, "/serverapi/tasks/deletedatabase", (Request request) => {

                    Response response;
                    if (!Authentication.Authenticate(settings, request, out response)) { return response; }
                    return Self.POST("/api/tasks/deletedatabase", null, request.BodyBytes, null);
                }, opt);

                // Initiate task, start database
                Handle.POST(port, "/serverapi/tasks/startdatabase", (Request request) => {

                    Response response;
                    if (!Authentication.Authenticate(settings, request, out response)) { return response; }
                    return Self.POST("/api/tasks/startdatabase", null, request.BodyBytes, null);
                }, opt);

                // Initiate task, install software
                Handle.POST(port, "/serverapi/tasks/installsoftware", (Request request) => {

                    Response response;
                    if (!Authentication.Authenticate(settings, request, out response)) { return response; }
                    return Self.POST("/api/tasks/installsoftware", null, request.BodyBytes, null);
                }, opt);

                // Initiate task, uninstall software
                Handle.POST(port, "/serverapi/tasks/uninstallsoftware", (Request request) => {

                    Response response;
                    if (!Authentication.Authenticate(settings, request, out response)) { return response; }
                    return Self.POST("/api/tasks/uninstallsoftware", null, request.BodyBytes, null);
                }, opt);



                // Initiate task, install application
                Handle.POST(port, "/serverapi/tasks/installapplication", (Request request) => {

                    Response response;
                    if (!Authentication.Authenticate(settings, request, out response)) { return response; }
                    return Self.POST("/api/tasks/installapplication", null, request.BodyBytes, null);
                }, opt);

                // Initiate task, install application
                Handle.POST(port, "/serverapi/tasks/upgradeapplication", (Request request) => {

                    Response response;
                    if (!Authentication.Authenticate(settings, request, out response)) { return response; }
                    return Self.POST("/api/tasks/upgradeapplication", null, request.BodyBytes, null);
                }, opt);

                // Initiate task, uninstall application
                Handle.POST(port, "/serverapi/tasks/uninstallapplication", (Request request) => {

                    Response response;
                    if (!Authentication.Authenticate(settings, request, out response)) { return response; }
                    return Self.POST("/api/tasks/uninstallapplication", null, request.BodyBytes, null);
                }, opt);

                // Initiate task, start application
                Handle.POST(port, "/serverapi/tasks/startapplication", (Request request) => {

                    Response response;
                    if (!Authentication.Authenticate(settings, request, out response)) { return response; }
                    return Self.POST("/api/tasks/startapplication", null, request.BodyBytes, null);
                }, opt);

                #endregion

                // Get Server information
                // Returns ServerInformationJson or { Text, StackTrace, Helplink }
                Handle.GET(port, "/serverapi/server", (Request request) => {

                    Response response;
                    if (!Authentication.Authenticate(settings, request, out response)) { return response; }

                    ServerInformationJson info = new ServerInformationJson();
                    info.StarcounterVersionChannel = CurrentVersion.ChannelName;
                    info.StarcounterVersion = CurrentVersion.Version;
                    info.StarcounterVersionDate = CurrentVersion.VersionDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");

                    ulong FreeBytesAvailable;
                    ulong TotalNumberOfBytes;
                    ulong TotalNumberOfFreeBytes;

                    bool success = Administrator.Server.Utilities.Utils.GetDiskFreeSpaceEx(StarcounterEnvironment.Server.ServerDir, out FreeBytesAvailable, out TotalNumberOfBytes, out TotalNumberOfFreeBytes);
                    if (success) {
                        info.TotalDisk = (TotalNumberOfBytes / 1024 / 1024);
                        info.FreeDisk = (TotalNumberOfFreeBytes / 1024 / 1024);
                    }
                    else {
                        info.TotalDisk = -1;
                        info.FreeDisk = -1;
                    }

                    float cpuload;
                    float ramusage;

                    if (Administrator.Server.Utilities.Utils.GetCPUAndMemeoryUsage(out cpuload, out ramusage)) {
                        info.CPULoad = new decimal(cpuload);
                        info.FreeRam = -1;
                        info.TotalRam = -1;
                    }
                    else {
                        info.CPULoad = -1;
                        info.FreeRam = -1;
                        info.TotalRam = -1;
                    }

                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.OK, Body = info.ToJson() };
                }, opt);

                // Get databases
                // Returns DatabasesJson or { Text, StackTrace, Helplink }
                Handle.GET(port, "/serverapi/databases", (Request request) => {

                    Response response;
                    if (!Authentication.Authenticate(settings, request, out response)) { return response; }
                    return Self.GET("/api/admin/databases");
                }, opt);

                // Get database
                Handle.GET(port, "/serverapi/databases/{?}", (string id, Request request) => {

                    Response response;
                    if (!Authentication.Authenticate(settings, request, out response)) { return response; }
                    return Self.GET(string.Format("/api/admin/databases/{0}", id));
                }, opt);

                // Get database software
                Handle.GET(port, "/serverapi/databases/{?}/software", (string databaseName, Request request) => {

                    Response response;
                    if (!Authentication.Authenticate(settings, request, out response)) { return response; }
                    return Self.GET(string.Format("/api/admin/databases/{0}/software", databaseName));
                }, opt);

                // Get database software
                Handle.GET(port, "/serverapi/databases/{?}/software/{?}", (string databaseName, string id, Request request) => {

                    Response response;
                    if (!Authentication.Authenticate(settings, request, out response)) { return response; }
                    return Self.GET(string.Format("/api/admin/databases/{0}/software/{1}", databaseName, id));
                }, opt);

                // Get database applications
                Handle.GET(port, "/serverapi/databases/{?}/applications", (string databaseName, Request request) => {

                    Response response;
                    if (!Authentication.Authenticate(settings, request, out response)) { return response; }
                    return Self.GET(string.Format("/api/admin/databases/{0}/applications", databaseName));
                }, opt);

                // Get database application
                Handle.GET(port, "/serverapi/databases/{?}/applications/{?}", (string databaseName, string id, Request request) => {

                    Response response;
                    if (!Authentication.Authenticate(settings, request, out response)) { return response; }
                    return Self.GET(string.Format("/api/admin/databases/{0}/applications/{1}", databaseName, id));
                }, opt);

                // Get database application
                Handle.GET(port, "/serverapi/databases/{?}/applications/{?}/{?}/{?}", (string databaseName, string nameSpace, string channel, string version, Request request) => {

                    Response response;
                    if (!Authentication.Authenticate(settings, request, out response)) { return response; }
                    return Self.GET(string.Format("/api/admin/databases/{0}/applications/{1}/{2}/{3}", databaseName, nameSpace, channel, version));
                }, opt);

                // Get database applications
                Handle.GET(port, "/serverapi/databases/{?}/applications/{?}/{?}", (string databaseName, string nameSpace, string channel, Request request) => {

                    Response response;
                    if (!Authentication.Authenticate(settings, request, out response)) { return response; }
                    return Self.GET(string.Format("/api/admin/databases/{0}/applications/{1}/{2}", databaseName, nameSpace, channel));
                }, opt);


                // Get default database settings
                Handle.GET(port, "/serverapi/defaultsettings/database", (Request request) => {

                    Response response;
                    if (!Authentication.Authenticate(settings, request, out response)) { return response; }
                    return Self.GET("/api/admin/settings/database");
                }, opt);


                // Get reverseproxies
                Handle.GET(port, "/serverapi/reverseproxies", (Request request) => {

                    Response response;
                    if (!Authentication.Authenticate(settings, request, out response)) { return response; }
                    return Http.GET(string.Format("http://127.0.0.1:{0}/sc/reverseproxies", StarcounterEnvironment.Default.SystemHttpPort));
                }, opt);

                // Get reverseproxy item
                Handle.GET(port, "/serverapi/reverseproxies/{?}/{?}", (string matchingHost, long starcounterProxyPort, Request request) => {

                    Response response;
                    if (!Authentication.Authenticate(settings, request, out response)) { return response; }
                    return Http.GET(string.Format("http://127.0.0.1:{0}/sc/reverseproxies/{1}/{2}", StarcounterEnvironment.Default.SystemHttpPort, matchingHost, starcounterProxyPort));
                }, opt);

                // Add (if not exists) reverse proxy item
                Handle.PUT(port, "/serverapi/reverseproxies", (Request request) => {

                    Response response;
                    if (!Authentication.Authenticate(settings, request, out response)) { return response; }
                    return Http.PUT("http://127.0.0.1:" + StarcounterEnvironment.Default.SystemHttpPort + "/sc/reverseproxies", request.BodyBytes, null);
                    //                    return Self.PUT("/sc/reverseproxies", null, request.BodyBytes, null, StarcounterEnvironment.Default.SystemHttpPort);
                }, opt);

                // Remove reverse proxy item
                Handle.DELETE(port, "/serverapi/reverseproxies/{?}/{?}", (string matchingHost, string starcounterProxyPort, Request request) => {

                    Response response;
                    if (!Authentication.Authenticate(settings, request, out response)) { return response; }
                    string uri = string.Format("http://127.0.0.1:{0}/sc/reverseproxies/{1}/{2}", StarcounterEnvironment.Default.SystemHttpPort, matchingHost, starcounterProxyPort);
                    return Http.DELETE(uri, request.BodyBytes, null);
                    //return Self.DELETE(string.Format("/sc/reverseproxies/{0}/{1}", matchingHost, starcounterProxyPort), null, request.BodyBytes, null;
                }, opt);


                // Get uri aliases
                Handle.GET(port, "/serverapi/urialiases", (Request request) => {

                    Response response;
                    if (!Authentication.Authenticate(settings, request, out response)) { return response; }
                    return Http.GET(string.Format("http://127.0.0.1:{0}/sc/alias", StarcounterEnvironment.Default.SystemHttpPort));
                }, opt);

                // Get uri alias item
                Handle.GET(port, "/serverapi/urialiases/{?}/{?}/{?}", (string httpMethod, long dbport, string fromUri, Request request) => {

                    Response response;
                    if (!Authentication.Authenticate(settings, request, out response)) { return response; }
                    return Http.GET(string.Format("http://127.0.0.1:{0}/sc/alias/{1}/{2}/{3}", StarcounterEnvironment.Default.SystemHttpPort, httpMethod, dbport, fromUri));
                }, opt);

                // Add (if not exists) uri alias item
                Handle.PUT(port, "/serverapi/urialiases", (Request request) => {

                    Response response;
                    if (!Authentication.Authenticate(settings, request, out response)) { return response; }
                    return Http.PUT("http://127.0.0.1:" + StarcounterEnvironment.Default.SystemHttpPort + "/sc/alias", request.BodyBytes, null);
                }, opt);

                // Remove uri alias item
                Handle.DELETE(port, "/serverapi/urialiases/{?}/{?}/{?}", (string httpMethod, long dbport, string fromUri, Request request) => {

                    Response response;
                    if (!Authentication.Authenticate(settings, request, out response)) { return response; }
                    return Http.DELETE(string.Format("http://127.0.0.1:{0}/sc/alias/{1}/{2}/{3}", StarcounterEnvironment.Default.SystemHttpPort, httpMethod, dbport, fromUri), request.BodyBytes, null);
                }, opt);
            }
            catch (Exception e) {
                // Failed to register an handler
                RestHandlers.ServerRestApiSource.LogNotice("Failed to register server api handlers, " + e.Message);
            }
        }

        /// <summary>
        /// Get RestServer settings (credentials,port)
        /// </summary>
        /// <returns></returns>
        private static RestSettings GetSettings() {

            RestSettings settings;
            string restConfigFile = System.IO.Path.Combine(Program.ResourceFolder, StarcounterEnvironment.FileNames.RestSettingsFileName);

            if (System.IO.File.Exists(restConfigFile)) {
                try {

                    string fileContent = System.IO.File.ReadAllText(restConfigFile);
                    if (fileContent != null) {
                        settings = new RestSettings();
                        settings.PopulateFromJson(fileContent);

                        if (settings.Port < IPEndPoint.MinPort || settings.Port > IPEndPoint.MaxPort) {
                            throw new ArgumentOutOfRangeException("Invalid portnumber");
                        };

                        return settings;
                    }
                    else {
                        throw new System.IO.InvalidDataException("Invalid file content");
                    }
                }
                catch (Exception e) {

                    RestHandlers.ServerRestApiSource.LogException(e, "Failed to read REST settings file, " + StarcounterEnvironment.FileNames.RestSettingsFileName + ".");
                }
            }

            // Default settings
            settings = new RestSettings();
            settings.Enabled = true;
            settings.Username = "admin";
            settings.Password = "admin";
            settings.Port = 8182;

            return settings;
        }
    }
}
