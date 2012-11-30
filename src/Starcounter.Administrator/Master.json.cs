using System;
using System.Runtime.CompilerServices;
using Starcounter;
using Starcounter.Internal;
using Starcounter.Server;
using Starcounter.Server.PublicModel;

// http://msdn.microsoft.com/en-us/library/system.runtime.compilerservices.internalsvisibletoattribute.aspx

namespace StarcounterApps3 {

    partial class Master : App {


        public static IServerRuntime ServerInterface;
        public static ServerEngine ServerEngine;

        static void Main(string[] args) {

            AppsBootstrapper.Bootstrap(8181, @"..\..\src\Starcounter.Administrator");

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

            GET("/query", () => new SqlApp() { View = "sql.html" });

            //GET("/empty", () => {
            //    return "empty";
            //});

        }

        void Handle(Input.TheButton input) {
            this.Message = "I clicked the button!";
        }
    }

}
