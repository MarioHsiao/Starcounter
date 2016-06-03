using Starcounter;
using Starcounter.CLI;
using Starcounter.Internal;
using Starcounter.Server.Rest.Representations.JSON;
using System;
using System.Collections.Generic;
using System.Text;

namespace staradmin.Commands {

    internal class ListApplicationsCommand : ListCommand {
        protected override void List() {
            var cli = new AdminCLI(Context.ServerReference);
            
            try {
                var db = Context.IsExcplicitDatabase ? Context.Database : null;
                var apps = cli.GetApplications(db);
                if (apps.Count == 0) {
                    SharedCLI.ShowInformationAndSetExitCode(
                        "No applications running",
                        0,
                        showStandardHints: false,
                        color: ConsoleColor.DarkGray
                        );
                    return;
                }
                ListResult(apps);

            } catch (Exception e) {
                uint errorCode;
                if (ErrorCode.TryGetCode(e, out errorCode)) {
                    if (errorCode == Error.SCERRSERVERNOTRUNNING) {
                        SharedCLI.ShowInformationAndSetExitCode(
                        "No applications running (server not running)",
                        0,
                        showStandardHints: false,
                        color: ConsoleColor.DarkGray
                        );
                        return;
                    }
                }
                throw e;
            }
        }

        void ListResult(Dictionary<Engine, Executable[]> result) {
            var table = new KeyValueTable();
            var rows = new Dictionary<string, string>();

            foreach (var database in result) {
                var engine = database.Key;
                var scopedApps = database.Value;

                foreach (var app in scopedApps) {
                    ConsoleUtil.ToConsoleWithColor(string.Format("{0} (in {1})", app.Name, engine.Database.Name), ConsoleColor.DarkYellow);
                    rows.Clear();
                    rows.Add("Path", app.ApplicationFilePath);
                    rows.Add("Started", app.RuntimeInfo.Started);
                    table.Write(rows);
                    Console.WriteLine();
                }
            }
        }
    }
}