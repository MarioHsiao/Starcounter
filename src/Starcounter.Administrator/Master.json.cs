using System;
using System.IO;
using System.Runtime.CompilerServices;
using Starcounter;
using Starcounter.ABCIPC.Internal;
using Starcounter.Internal;
using Starcounter.Server;
using Starcounter.Server.PublicModel;

// http://msdn.microsoft.com/en-us/library/system.runtime.compilerservices.internalsvisibletoattribute.aspx

namespace StarcounterApps3 {

    partial class Master : App {

        public static IServerRuntime ServerInterface;
        public static ServerEngine ServerEngine;

        // Argument <path to server configuraton file> <portnumber>
        static void Main(string[] args) {

            int port = 8080; // TODO: Read from some configuration?

            if (args == null || args.Length < 1) {
                Console.WriteLine("Starcounter Administrator: Invalid arguments: Usage <path to server configuraton file>");
                return;
            }

            // Server configuration file
            if (string.IsNullOrEmpty(args[0]) || !File.Exists(args[0])) {
                Console.WriteLine("Starcounter Administrator: Missing server configuration file {0}", args[0]);
            }

            // Port number
            //if (int.TryParse(args[1], out port) == false) {
            //    Console.WriteLine("Starcounter Administrator: Invalid port number {0}", args[1]);
            //    return;
            //};

            Console.WriteLine("Starcounter Administrator started on port {0}.", port);

            AppsBootstrapper.Bootstrap(port);

            Master.ServerEngine = new ServerEngine(args[0]);      // .srv\Personal\Personal.server.config
            Master.ServerEngine.Setup();
            Master.ServerInterface = Master.ServerEngine.Start();

            // Start Engine services
            StartListeningService();

            GET("/", () => {
                return new Master() { View = "index.html" };
            });

            GET("/test", () => {
                return new Master() {
                    View = "test.html",
                    SomeNo = 146,
                    Message = "Click the button!"
                };
            });


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
                    DatabaseApp databaseApp = new DatabaseApp() { DatabaseName = database.Name, Uri = Uri.EscapeDataString(database.Uri) };
                    databaseList.DatabaseList.Add(databaseApp);
                }

                return databaseList;

            });

            //            GET("/databases/{?}/apps", (string uri) => {  // THIS DOSENT WORK
            //GET("/databases/administrator/apps", () => {

            //    //administrator
            //    DatabaseInfo database = Master.ServerInterface.GetDatabase("sc://headsutv15/personal/administrator");

            //    DatabaseAppsApp appsList = new DatabaseAppsApp();
            //    appsList.View = "apps.html";
            //    AppInfo[] apps = database.HostedApps;

            //    foreach (var app in apps) {
            //        AppApp appApp = new AppApp() { AppName = app.ExecutablePath };
            //        appsList.AppsList.Add(appApp);
            //    }

            //    return appsList;
            //});

            GET("/databases/{?}", (string uri) => {

                DatabaseInfo database = Master.ServerInterface.GetDatabase(uri);

                DatabaseApp databaseApp = new DatabaseApp();
                databaseApp.View = "database.html";
                databaseApp.DatabaseName = database.Name;
                databaseApp.Uri = database.Uri;

                return databaseApp;
            });


            GET("/query", () => {
                return new Master() { View = "sql.html" };
            });
            //GET("/empty", () => {
            //    return "empty";
            //});

        }

        static void OnIPCServerReceivedRequest(object sender, string e) {
              ToConsoleWithColor(string.Format("Request: {0}", e), ConsoleColor.Yellow);
        }


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

        void Handle(Input.TheButton input) {
            this.Message = "I clicked the button!";
        }


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
