using System;
using System.IO;
using System.Runtime.CompilerServices;
using Sc.Tools.Logging;
using Starcounter;
using Starcounter.ABCIPC.Internal;
using Starcounter.Internal;
using Starcounter.Server;
using Starcounter.Server.PublicModel;
using Starcounter.Server.PublicModel.Commands;
using StarcounterAppsLogTester;
using Newtonsoft.Json;
using Starcounter.Internal.REST;
using Starcounter.Advanced;

using ExecRequest = StarcounterApps3.ExecRequest;

// http://msdn.microsoft.com/en-us/library/system.runtime.compilerservices.internalsvisibletoattribute.aspx

namespace StarcounterApps3 {

    partial class Master : Puppet {

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

            Master.ServerEngine = new ServerEngine(args[0]);      // .srv\Personal\Personal.server.config
            Master.ServerEngine.Setup();
            Master.ServerInterface = Master.ServerEngine.Start();

            // Start Engine services
            StartListeningService();

            // Start listening on log-events
            ServerInfo serverInfo = Master.ServerInterface.GetServerInfo();

            //SetupLogListener(serverInfo.Configuration.LogDirectory);

            LogApp.Setup(serverInfo.Configuration.LogDirectory);

            RegisterGETS();
            RegisterPOSTS();
        }

        static void RegisterGETS() {

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

            GET("/", () => {
                return new Master() { View = "index.html" };
            });

            // Accept "", text/html, OR application/json. Otherwise, 406.
            GET("/server", () => {

                ServerInfo serverInfo = Master.ServerInterface.GetServerInfo();

                ServerApp serverApp = new ServerApp();
                serverApp.View = "server.html";
                serverApp.DatabaseDirectory = serverInfo.Configuration.DatabaseDirectory;
                serverApp.LogDirectory = serverInfo.Configuration.LogDirectory;
                serverApp.TempDirectory = serverInfo.Configuration.TempDirectory;
                serverApp.ServerName = serverInfo.Configuration.Name;

                return serverApp;
            });

            GET("/databases", () => {

                DatabaseInfo[] databases = Master.ServerInterface.GetDatabases();

                DatabasesApp databaseList = new DatabasesApp();

                databaseList.View = "databases.html";
                foreach (var database in databases) {
                    DatabaseApp databaseApp = new DatabaseApp();
                    databaseApp.SetDatabaseInfo(database);

                    databaseList.DatabaseList.Add(databaseApp);

                }

                return databaseList;

            });

            GET("/databases/{?}", (string uri) => {

                DatabaseInfo database = Master.ServerInterface.GetDatabase(uri);

                DatabaseApp databaseApp = new DatabaseApp();
                databaseApp.View = "database.html";

                databaseApp.SetDatabaseInfo(database);

                return databaseApp;
            });


            GET("/apps", () => {
                AppsApp appsApp = new AppsApp();
                appsApp.View = "apps.html";

                appsApp.Setup();

                return appsApp;
            });


            GET("/query", () => {

                SqlApp sqlApp = new SqlApp();
                sqlApp.View = "sql.html";
                sqlApp.DatabaseName = "default";
                sqlApp.Query = "SELECT m FROM systable m";
                sqlApp.Port = 8181;

                return sqlApp;

                //return new SqlApp() { View = "sql.html", DatabaseName = "default", Query = "SELECT m FROM systable m", Port = 8181 };
            });

            GET("/log", () => {
                LogApp logApp = new LogApp() { FilterNotice = true, FilterWarning = true, FilterError = true };
                logApp.View = "log.html";
                logApp.RefreshLogEntriesList();
                return logApp;
            });
        }

        static void RegisterPOSTS() {

            // Define the handler responsible for handling requests to
            // execute (implemented as a POST to a given database executable
            // collection resource).
            //   The handler will change to use the Message class as the input
            // parameter rather than the request. For now, we'll have to do
            // with the request and convert it's body to a Message instance by
            // hand.
            POST<HttpRequest, string>("/databases/{?}/executables", (HttpRequest request, string name) => {
                ServerEngine engine = Master.ServerEngine;
                IServerRuntime runtime = Master.ServerInterface;

                // TODO:
                // Commented the following code until the codegenerated deserialization is in place.
                // For now we need to manually create an instance and fill it from a string.
//                var execRequest = ExecRequest.FromJson(request);

                var execRequest = new ExecRequest();
                execRequest.PopulateFromJson(request.GetBodyStringUtf8_Slow());
                
                var cmd = new ExecAppCommand(engine, execRequest.ExecutablePath, null, null);
                cmd.DatabaseName = name;
                cmd.EnableWaiting = true;
                cmd.LogSteps = execRequest.LogSteps;
                cmd.NoDb = execRequest.NoDb;

                var commandInfo = runtime.Execute(cmd);
                commandInfo = runtime.Wait(commandInfo);

                // For illustration purposes, showing that we can return
                // Message objects as the content, we simply return the
                // one we have at hand. This should change, returning an
                // entity that contains context-relative information about
                // the resouce (i.e. the now running executable).
                // TODO:

                return execRequest;
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

        static void ToConsoleWithColor(string text, ConsoleColor color) {
            try {
                Console.ForegroundColor = color;
                Console.WriteLine(text);
            } finally {
                Console.ResetColor();
            }
        }
    }

}
