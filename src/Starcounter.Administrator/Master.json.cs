﻿
using Codeplex.Data;
using Starcounter;
using Starcounter.ABCIPC.Internal;
using Starcounter.Administrator;
using Starcounter.Advanced;
using Starcounter.Internal;
using Starcounter.Internal.JsonPatch;
using Starcounter.Internal.REST;
using Starcounter.Server;
using Starcounter.Server.PublicModel;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Web;

// http://msdn.microsoft.com/en-us/library/system.runtime.compilerservices.internalsvisibletoattribute.aspx

namespace Starcounter.Administrator {

    partial class Master : Json {

        public static IServerRuntime ServerInterface;
        public static ServerEngine ServerEngine;

        // Argument <path to server configuraton file> <portnumber>
        static void Main(string[] args) {

            if (args == null || args.Length < 1) {
                Console.WriteLine("Starcounter Administrator: Invalid arguments: Usage <path to server configuraton file>");
                return;
            }

            // Server configuration file.
            if (string.IsNullOrEmpty(args[0]) || !File.Exists(args[0])) {
                Console.WriteLine("Starcounter Administrator: Missing server configuration file {0}", args[0]);
            }

            // Administrator port.
            UInt16 adminPort = StarcounterConstants.NetworkPorts.DefaultPersonalServerSystemHttpPort;
            if (UInt16.TryParse(args[1], out adminPort) == false) {
                Console.WriteLine("Starcounter Administrator: Invalid port number {0}", args[1]);
                return;
            };

            Console.WriteLine("Starcounter Administrator started on port: " + adminPort);

            AppsBootstrapper.Bootstrap(adminPort, "scadmin");
            //AppsBootstrapper.Bootstrap(adminPort, @"c:\github\Level1\src\Starcounter.Administrator");   // TODO:REMOVE

            Master.ServerEngine = new ServerEngine(args[0]);      // .srv\Personal\Personal.server.config
            Master.ServerEngine.Setup();
            Master.ServerInterface = Master.ServerEngine.Start();


            // Start Engine services
            StartListeningService();

            // Start listening on log-events
            ServerInfo serverInfo = Master.ServerInterface.GetServerInfo();

            LogApp.Setup(serverInfo.Configuration.LogDirectory);

            HostedExecutables.Setup(
                Dns.GetHostEntry(String.Empty).HostName,
                adminPort,
                Master.ServerEngine,
                Master.ServerInterface
            );

            RegisterHandlers(serverInfo.Configuration.SystemHttpPort);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sytemHttpPort">Port for doing SQL queries</param>
        static void RegisterHandlers(ushort sytemHttpPort) {

            // Registering default handler for ALL static resources on the server.
            GET("/{?}", (string res) => {
                return null;
            });

            // Redirecting root to index.html.
            GET("/", (Request req) => {
                Response resp;

                // Doing another request with original request attached.
                Node.LocalhostSystemPortNode.GET("/index.html", req, out resp);

                if (resp == null)
                    throw ErrorCode.ToException(Error.SCERRENDPOINTUNREACHABLE);

                // Returns this response to original request.
                return resp;
            });

            #region Server
            // Accept "", text/html, OR application/json. Otherwise, 406.
            GET("/server", () => {

                ServerInfo serverInfo = Master.ServerInterface.GetServerInfo();
                ServerApp serverApp = new ServerApp();
                serverApp.SystemHttpPort = serverInfo.Configuration.SystemHttpPort;
                serverApp.DatabaseDirectory = serverInfo.Configuration.DatabaseDirectory;
                serverApp.LogDirectory = serverInfo.Configuration.LogDirectory;
                serverApp.TempDirectory = serverInfo.Configuration.TempDirectory;
                serverApp.ServerName = serverInfo.Configuration.Name;

                return serverApp;
            });

            #endregion

            #region Database(s)

            // Returns a list of databases
            GET("/databases", (Request req) => {

                if (InternalHandlers.StringExistInList("application/json", req["Accept"])) {

                    DatabaseInfo[] databases = Master.ServerInterface.GetDatabases();
                    DatabasesApp databaseList = new DatabasesApp();
                    foreach (var database in databases) {
                        DatabaseApp databaseApp = new DatabaseApp();
                        databaseApp.SetDatabaseInfo(database);
                        databaseList.DatabaseList.Add(databaseApp);
                    }
                    return databaseList;
                }
                else {
                    return HttpStatusCode.NotAcceptable;
                }

            });


            // Returns a database
            GET("/databases/{?}", (string databaseid, Request req) => {

                if (InternalHandlers.StringExistInList("application/json", req["Accept"])) {
                    DatabaseInfo database = Master.ServerInterface.GetDatabase(Master.DecodeFrom64(databaseid));
                    if (database != null) {
                        DatabaseApp databaseApp = new DatabaseApp();
                        databaseApp.SetDatabaseInfo(database);
                        //Session.Data = databaseApp;
                        return databaseApp;
                    }
                    return HttpStatusCode.NotFound;
                }
                return HttpStatusCode.NotAcceptable;

            });

            #endregion

            #region Log

            GET("/log?{?}", (string parameters, Request req) => {

                if (InternalHandlers.StringExistInList("application/json", req["Accept"])) {

                    NameValueCollection collection = System.Web.HttpUtility.ParseQueryString(parameters);

                    LogApp logApp = new LogApp();

                    #region Set Filter
                    Boolean filter_debug;
                    Boolean.TryParse(collection["debug"], out filter_debug);
                    logApp.FilterDebug = filter_debug;

                    Boolean filter_notice;
                    Boolean.TryParse(collection["notice"], out filter_notice);
                    logApp.FilterNotice = filter_notice;

                    Boolean filter_warning;
                    Boolean.TryParse(collection["warning"], out filter_warning);
                    logApp.FilterWarning = filter_warning;

                    Boolean filter_error;
                    Boolean.TryParse(collection["error"], out filter_error);
                    logApp.FilterError = filter_error;
                    #endregion

                    logApp.RefreshLogEntriesList();
                    return logApp;
                }
                else {
                    return HttpStatusCode.NotAcceptable;
                }
            });

            // Returns the log
            GET("/log", (Request req) => {

                if (InternalHandlers.StringExistInList("application/json", req["Accept"])) {
                    LogApp logApp = new LogApp() { FilterDebug = false, FilterNotice = false, FilterWarning = true, FilterError = true };
                    logApp.RefreshLogEntriesList();
                    return logApp;
                }
                else {
                    return HttpStatusCode.NotAcceptable;
                }
            });

            #endregion

            #region SQL

            POST("/sql/{?}", (string databasename, Request req) => {

                if (InternalHandlers.StringExistInList("application/json", req["Accept"])) {
                    try {
                        Response response;
                        string bodyData = req.GetBodyStringUtf8_Slow();   // Retrieve the sql command in the body

                        Node.LocalhostSystemPortNode.POST(string.Format("/__{0}/sql", databasename), bodyData, null, out response);

                        if (response == null) {
                            return HttpStatusCode.ServiceUnavailable;
                        }
                        if (response.StatusCode >= 200 && response.StatusCode < 300) {
                            return response.GetBodyStringUtf8_Slow();
                        }

                        return (int)response.StatusCode;
                    }
                    catch (Exception e) {
                        dynamic resultJson = new DynamicJson();
                        resultJson.columns = new object[] { };
                        resultJson.rows = new object[] { };
                        resultJson.exception = new { message = e.Message, helpLink = e.HelpLink, stackTrace = e.StackTrace };
                        resultJson.sqlException = null;
                        return resultJson.ToString();
                    }
                }
                else {
                    return HttpStatusCode.NotAcceptable;
                }

            });

            #endregion

            #region Get Console output from database
            // Returns the log
            GET("/databases/{?}/console", (string databaseid, Request req) => {

                if (InternalHandlers.StringExistInList("application/json", req["Accept"])) {
                    dynamic resultJson = new DynamicJson();
                    resultJson.console = null;
                    resultJson.exception = null;

                    try {
                        Response response;
                        string bodyData = req.GetBodyStringUtf8_Slow();   // Retrieve the sql command in the body

                        Node.LocalhostSystemPortNode.GET(string.Format("/__{0}/console", databaseid), null, out response);

                        if (response == null) {
                            return HttpStatusCode.ServiceUnavailable;
                        }

                        if (response.StatusCode >= 200 && response.StatusCode < 300) {
                            // Success
                            return response.GetBodyStringUtf8_Slow();
                        }

                        return (int)response.StatusCode;
                    }
                    catch (Exception e) {
                        resultJson = new DynamicJson();
                        resultJson.exception = new { message = e.Message, helpLink = e.HelpLink, stackTrace = e.StackTrace };
                        return resultJson.ToString();
                    }
                }
                else {
                    return HttpStatusCode.NotAcceptable;
                }

            });
            #endregion


            POST("/addstaticcontentdir", (Request req) => {

                // Getting POST contents.
                String content = req.GetBodyStringUtf8_Slow();

                // Splitting contents.
                String[] settings = content.Split(new String[] { StarcounterConstants.NetworkConstants.CRLF }, StringSplitOptions.RemoveEmptyEntries);

                try {
                    // Registering static handler on given port.
                    GET(UInt16.Parse(settings[0]), "/{?}", (string res) => {
                        return null;
                    });
                }
                catch (Exception exc) {
                    UInt32 errCode;

                    // Checking if this handler is already registered.
                    if (ErrorCode.TryGetCode(exc, out errCode)) {
                        if (Starcounter.Error.SCERRHANDLERALREADYREGISTERED == errCode)
                            return "Success!";
                    }
                    throw exc;
                }

                // Adding static files serving directory.
                AppsBootstrapper.AddFileServingDirectory(settings[1]);

                return "Success!";
            });

            GET("/return/{?}", (int code) => {
                return code;
            });

            GET("/returnstatus/{?}", (int code) => {
                return (System.Net.HttpStatusCode)code;
            });

            GET("/returnwithreason/{?}", (string codeAndReason) => {
                // Example input: 404ThisIsMyCustomReason
                var code = int.Parse(codeAndReason.Substring(0, 3));
                var reason = codeAndReason.Substring(3);
                return new HttpStatusCodeAndReason(code, reason);
            });

            GET("/test", () => {
                return "hello";
            });


        }

        #region ServerServices

        static void StartListeningService() {
            System.Threading.ThreadPool.QueueUserWorkItem(ServerServicesThread);
        }

        static private void ServerServicesThread(object state) {
            ServerServices services;
            string pipeName;
            pipeName = ScUriExtensions.MakeLocalServerPipeString(Master.ServerEngine.Name);
            var ipcServer = ClientServerFactory.CreateServerUsingNamedPipes(pipeName);
            ipcServer.ReceivedRequest += OnIPCServerReceivedRequest;
            services = new ServerServices(Master.ServerEngine, ipcServer);
            ToConsoleWithColor(string.Format("Accepting service calls on pipe '{0}'...", pipeName), ConsoleColor.DarkGray);

            services.Setup();
            // Start the engine and run the configured services.
            Master.ServerEngine.Run(services);

        }

        static void OnIPCServerReceivedRequest(object sender, string e) {
            ToConsoleWithColor(string.Format("Request: {0}", e), ConsoleColor.Yellow);
        }

        #endregion

        static public string EncodeTo64(string toEncode) {
            byte[] toEncodeAsBytes = System.Text.ASCIIEncoding.ASCII.GetBytes(toEncode);
            string returnValue = System.Convert.ToBase64String(toEncodeAsBytes);
            return returnValue;
        }

        static public string DecodeFrom64(string encodedData) {
            byte[] encodedDataAsBytes = System.Convert.FromBase64String(encodedData);
            string returnValue = System.Text.ASCIIEncoding.ASCII.GetString(encodedDataAsBytes);
            return returnValue;
        }

        static void ToConsoleWithColor(string text, ConsoleColor color) {
            try {
                Console.ForegroundColor = color;
                Console.WriteLine(text);
            }
            finally {
                Console.ResetColor();
            }
        }
    }

}
