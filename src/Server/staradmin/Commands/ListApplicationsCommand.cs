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
            var cli = new AdminCLI(ServerReference.CreateDefault());
            
            try {
                var apps = cli.GetApplications();
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
            var rows = new Dictionary<string, string>();

            // "app4  Database:Default
            //        Path:C:\apps\myapps

            var s = new StringBuilder();
            foreach (var database in result) {
                var engine = database.Key;
                var scopedApps = database.Value;

                foreach (var app in scopedApps) {
                    s.Clear();
                    s.AppendFormat("Database: {0}", engine.Database.Name);
                    s.AppendLine();
                    s.AppendFormat("Path:{0}", app.Path);
                    rows.Add(app.Name, s.ToString());
                }
            }

            var table = new KeyValueTable();
            table.Title = "Applications:";
            table.Write(rows, ValueSplitOptions.SplitLines);
        }
    }
}