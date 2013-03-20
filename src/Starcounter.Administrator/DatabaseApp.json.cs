using System;
using Starcounter;
using Starcounter.Server.PublicModel;
using Starcounter.Server.PublicModel.Commands;

namespace StarcounterApps3 {
    partial class DatabaseApp : Json {

        void Handle(Input.Start start) {

            DatabaseInfo database = Master.ServerInterface.GetDatabase(this.Uri);

            var startCMD = new StartDatabaseCommand(Master.ServerEngine, database.Name);
            CommandInfo cmdInfo = Master.ServerInterface.Execute(startCMD);
            Master.ServerInterface.Wait(cmdInfo);

            // Refresh info
            database = Master.ServerInterface.GetDatabase(this.Uri);
            this.SetDatabaseInfo(database);
        }

        void Handle(Input.Stop action) {

            DatabaseInfo database = Master.ServerInterface.GetDatabase(this.Uri);

            var cmd = new StopDatabaseCommand(Master.ServerEngine, database.Name, true);

            CommandInfo cmdInfo = Master.ServerInterface.Execute(cmd);
            Master.ServerInterface.Wait(cmdInfo);

            // Refresh info
            database = Master.ServerInterface.GetDatabase(this.Uri);
            this.SetDatabaseInfo(database);

        }

        public void SetDatabaseInfo(DatabaseInfo databaseInfo) {

            this.DatabaseName = databaseInfo.Name;
            this.Uri = System.Uri.EscapeDataString(databaseInfo.Uri); // This will be used in the url string for getting one database
            this.MaxImageSize = (int)databaseInfo.MaxImageSize;
            this.TransactionLogSize = (int)databaseInfo.TransactionLogSize;
            this.CollationFile = databaseInfo.CollationFile;
            this.SupportReplication = databaseInfo.SupportReplication;

            this.HostProcessId = databaseInfo.HostProcessId;

//                        Uri = Uri.EscapeDataString(database.Uri),

            this.Apps.Clear(); // TODO: Update list, do not recreate it.
            foreach (var app in databaseInfo.HostedApps) {
                this.Apps.Add(new AppApp() { ExecutablePath = app.ExecutablePath, WorkingDirectory = app.WorkingDirectory });
            }


        }

    }
}

