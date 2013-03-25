
using Starcounter;
using Starcounter.ABCIPC.Internal;
using Starcounter.Administrator;
using Starcounter.Advanced;
using Starcounter.Internal;
using Starcounter.Internal.REST;
using Starcounter.Server;
using Starcounter.Server.PublicModel;
using System;
using System.IO;
using System.Net;

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
//            AppsBootstrapper.Bootstrap(adminPort, @"c:\github\Level1\src\Starcounter.Administrator");   // TODO:REMOVE

            Master.ServerEngine = new ServerEngine(args[0]);      // .srv\Personal\Personal.server.config
            Master.ServerEngine.Setup();
            Master.ServerInterface = Master.ServerEngine.Start();

            // Start Engine services
            StartListeningService();

            // Start listening on log-events
            ServerInfo serverInfo = Master.ServerInterface.GetServerInfo();

            //SetupLogListener(serverInfo.Configuration.LogDirectory);

            LogApp.Setup(serverInfo.Configuration.LogDirectory);

            HostedExecutables.Setup(
                Dns.GetHostEntry(String.Empty).HostName,
                adminPort,
                Master.ServerEngine,
                Master.ServerInterface
            );


            RegisterHandlers();
        }

        static void RegisterHandlers() {

            // This dosent work.
            GET("/", () => {
                return StarcounterBase.Get("index.html");
            });
            // Registering default handler for ALL static resources on the server.
            GET("/{?}", (string res) => {
                return null;
            });

            #region Database(s)

            // Returns a list of databases
            GET("/databases", (Request req) => {

                var contentType = req["Accept"];

                if (contentType != null) {
                    string[] types = contentType.Split(',');

                    foreach (string type in types) {

                        if (string.Equals(type, "text/html", StringComparison.CurrentCultureIgnoreCase) ||
                            string.Equals(type, "text/plain", StringComparison.CurrentCultureIgnoreCase)) {
                            return StarcounterBase.Get("partials/databases.html");
                        }
                        else if (string.Equals(type, "application/json", StringComparison.CurrentCultureIgnoreCase)) {

                            DatabaseInfo[] databases = Master.ServerInterface.GetDatabases();
                            DatabasesApp databaseList = new DatabasesApp();



                            foreach (var database in databases) {
                                DatabaseApp databaseApp = new DatabaseApp();

                                databaseApp.DefaultUserHttpPort = 80;   // TODO: Get this from the database config (via the server enging API)
                                databaseApp.SystemHttpPort = 8181;      // TODO: Get this from the server config (via the server enging API)

                                databaseApp.SetDatabaseInfo(database);
                                databaseList.DatabaseList.Add(databaseApp);
                            }
                            Session.Data = databaseList;
                            return databaseList;
                        }
                    }
                }
                return 404;


            });


            // Returns a database
            GET("/databases/{?}", (string databaseid, Request req) => {

                var contentType = req["Accept"];

                if (contentType != null) {
                    string[] types = contentType.Split(',');

                    foreach (string type in types) {

                        if (string.Equals(type, "application/json", StringComparison.CurrentCultureIgnoreCase)) {

                            DatabaseInfo database = Master.ServerInterface.GetDatabase(Master.DecodeFrom64(databaseid));
                            if (database != null) {
                                DatabaseApp databaseApp = new DatabaseApp();
                                databaseApp.SetDatabaseInfo(database);
                                Session.Data = databaseApp;
                                return databaseApp;
                            }
                            return 404;
                        }
                    }
                }

                return 404;

            });

            #region Log
            // Returns the log
            GET("/log", (Request req) => {

                var contentType = req["Accept"];

                if (contentType != null) {
                    string[] types = contentType.Split(',');

                    foreach (string type in types) {

                        if (string.Equals(type, "text/html", StringComparison.CurrentCultureIgnoreCase) ||
                            string.Equals(type, "text/plain", StringComparison.CurrentCultureIgnoreCase)) {
                            return StarcounterBase.Get("partials/log.html");
                        }
                        else if (string.Equals(type, "application/json", StringComparison.CurrentCultureIgnoreCase)) {

                            LogApp logApp = new LogApp() { FilterNotice = true, FilterWarning = true, FilterError = true };
                            logApp.RefreshLogEntriesList();
                            Session.Data = logApp;
                            return logApp;
                        }
                    }
                }
                return 404;

            });

            #endregion

            #region SQL

            GET("/sql", (Request req) => {

                var contentType = req["Accept"];

                if (contentType != null) {
                    string[] types = contentType.Split(',');

                    foreach (string type in types) {

                        if (string.Equals(type, "text/html", StringComparison.CurrentCultureIgnoreCase) ||
                            string.Equals(type, "text/plain", StringComparison.CurrentCultureIgnoreCase)) {
                            return StarcounterBase.Get("partials/sql.html");
                        }
                        //else if (string.Equals(type, "application/json", StringComparison.CurrentCultureIgnoreCase)) {
                        //    SqlApp sqlApp = new SqlApp();
                        //    sqlApp.DatabaseName = "default";            // Remove
                        //    sqlApp.Query = "SELECT m FROM systable m";  // Remove
                        //    sqlApp.Port = 8181;                         // Remove
                        //    Session.Data = sqlApp;
                        //    return sqlApp;
                        //}
                    }
                }
                return 404;


                //return new SqlApp() { View = "sql.html", DatabaseName = "default", Query = "SELECT m FROM systable m", Port = 8181 };
            });

            #endregion

            //// Returns a database
            //POST("/databases/{?}", (string databaseid, Request req) => {

            //    databaseid = databaseid.Replace("_colon_", ":"); // TODO: Remove when bug is fixed
            //    databaseid = databaseid.Replace("_slash_", "/");    // TODO: Remove when bug is fixed

            //    var str = req["Accept"];

            //    if (str != null) {
            //        string[] types = str.Split(',');

            //        foreach (string type in types) {

            //            if (string.Equals(type, "application/json", StringComparison.CurrentCultureIgnoreCase)) {

            //                DatabaseInfo database = Master.ServerInterface.GetDatabase(databaseid);
            //                if (database != null) {
            //                    DatabaseApp databaseApp = new DatabaseApp();
            //                    databaseApp.SetDatabaseInfo(database);
            //                    Session.Data = databaseApp;
            //                    return databaseApp;
            //                }
            //                return 404;
            //            }
            //        }
            //    }

            //    return 404;

            //});


            #endregion


            POST("/addstaticcontentdir", (Request req) => {

                // Getting POST contents.
                String content = req.GetContentStringUtf8_Slow();

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


            // Accept "", text/html, OR application/json. Otherwise, 406.
            GET("/server", () => {

                ServerInfo serverInfo = Master.ServerInterface.GetServerInfo();

                ServerApp serverApp = new ServerApp();
                //                serverApp.View = "server.html";
                serverApp.DatabaseDirectory = serverInfo.Configuration.DatabaseDirectory;
                serverApp.LogDirectory = serverInfo.Configuration.LogDirectory;
                serverApp.TempDirectory = serverInfo.Configuration.TempDirectory;
                serverApp.ServerName = serverInfo.Configuration.Name;

                return serverApp;
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
