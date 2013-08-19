using System;
using Starcounter;
using Starcounter.Advanced;
using StarcounterApplicationWebSocket.VersionHandler.Model;
using Codeplex.Data;
using Starcounter.Applications.UsageTrackerApp.VersionHandler;
using StarcounterApplicationWebSocket.VersionHandler;

namespace StarcounterApplicationWebSocket.API.Versions {
    internal class Utils {
        public static void BootStrap(ushort port) {

            Handle.GET(port, "/cleanall", (Request request) => {

                Db.Transaction(() => {

                    var result = Db.SlowSQL("SELECT o FROM VersionSource o");

                    foreach (VersionSource item in result) {
                        item.Delete();
                    }

                    result = Db.SlowSQL("SELECT o FROM VersionBuild o");
                    foreach (VersionBuild item in result) {
                        item.Delete();
                    }

                    VersionHandlerSettings settings = VersionHandlerSettings.GetSettings();
                    settings.Delete();


                });

                return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NoContent };

            });


            Handle.GET(port, "/refresh", (Request request) => {

                SyncData.Start();

                VersionHandlerApp.UnpackWorker.Trigger();
                VersionHandlerApp.BuildkWorker.Trigger();
                return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NoContent };

            });


            Handle.GET(port, "/trigger", (Request request) => {

                VersionHandlerApp.UnpackWorker.Trigger();
                VersionHandlerApp.BuildkWorker.Trigger();
                return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NoContent };

            });


      

        }
    }
}
