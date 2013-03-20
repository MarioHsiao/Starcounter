using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Sc.Tools.Logging;
using Starcounter;
using Starcounter.Server.PublicModel;

namespace StarcounterApps3 {
    partial class AppsApp : Json {

        void Handle(Input.Upload action) {

        }

        public void Setup() {

            this.Running.Clear();
            this.Available.Clear();

            DatabaseInfo[] databases = Master.ServerInterface.GetDatabases();

            foreach (DatabaseInfo dbinfo in databases) {
                foreach (AppInfo app in dbinfo.HostedApps) {

                    var runningApp = this.Running.Add();
                    runningApp.id = "FAKEID";
                    runningApp.appname = "FAKENAME";
                }
            }

            // FAKE AVAILABLE
            for (int i = 0; i < 5; i++) {

                AvailableApp a = new AvailableApp() { appname = "FAKENAME" + i, id = "FAKEID" + i };
                this.Available.Add(a);
            }

        }




    }


}