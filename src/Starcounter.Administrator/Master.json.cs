using System;
using Starcounter;
using Starcounter.Internal;
using Starcounter.Server;
using Starcounter.Server.PublicModel;

namespace StarcounterApps3 {
    partial class Master : App {

        private static String RESOURCE_DIRECTORY = @"..\..\src\Starcounter.Administrator";

        public static IServerRuntime ServerInterface;
        private static ServerEngine ServerEngine;

        static void Main(string[] args) {

            AppsBootstrapper.Bootstrap(8080, RESOURCE_DIRECTORY);

            Master.ServerEngine = new ServerEngine(@"..\..\bin\Debug\.srv\Personal\Personal.server.config");
            Master.ServerEngine.Setup();
            Master.ServerInterface = Master.ServerEngine.Start();

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


            GET("/_server", () => {

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

                    DatabaseApp databaseApp = new DatabaseApp() { DatabaseName = database.Name, Uri = database.Uri };

                    //    var app = new DatabasesApp() { Title = database.Name };

                    databaseList.DatabaseList.Add(databaseApp);
                    //    master.Databases.Add(app);

                }

                return databaseList;

            });

            GET("/databases/{?}", (string uri) => {

                DatabaseInfo database = Master.ServerInterface.GetDatabase(uri);

                DatabaseApp databaseApp = new DatabaseApp();
                databaseApp.View = "database.html";
                databaseApp.DatabaseName = database.Name;
                databaseApp.Uri = database.Uri;

                return databaseApp;

            });
            //GET("/databases/{?}", (string databasename) => new Master() { 
            //    View = "database.html" });



            //GET("/empty", () => {
            //    return "empty";
            //});

        }

        void Handle(Input.TheButton input) {
            this.Message = "I clicked the button!";
        }
    }
}
