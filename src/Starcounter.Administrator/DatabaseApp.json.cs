using System;
using Starcounter;
using Starcounter.Server.PublicModel;
using Starcounter.Server.PublicModel.Commands;

namespace StarcounterApps3 {
    partial class DatabaseApp : App {

        void Handle(Input.Start start) {

            DatabaseInfo db = Master.ServerInterface.GetDatabase(this.Uri);

            var startCMD = new StartDatabaseCommand(Master.ServerEngine, db.Name);
            CommandInfo cmdInfo = Master.ServerInterface.Execute(startCMD);
            Master.ServerInterface.Wait(cmdInfo);

            this.Status = "Started"; // TODO: Use real Status code


        }

    }
}

