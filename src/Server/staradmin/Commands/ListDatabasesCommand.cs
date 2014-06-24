using Starcounter;
using Starcounter.CLI;
using Starcounter.Internal;
using Starcounter.Server.Rest.Representations.JSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace staradmin.Commands {

    internal class ListDatabasesCommand : ListCommand {
        protected override void List() {
            var cli = new AdminCLI(Context.ServerReference);

            try {
                var dbs = cli.GetDatabases();
                if (dbs.Length == 0) {
                    SharedCLI.ShowInformationAndSetExitCode(
                        "No databases found",
                        0,
                        showStandardHints: false,
                        color: ConsoleColor.DarkGray
                        );
                    return;
                }
                ListResult(cli, dbs);

            } catch (Exception e) {
                uint errorCode;
                if (ErrorCode.TryGetCode(e, out errorCode)) {
                    if (errorCode == Error.SCERRSERVERNOTRUNNING) {
                        SharedCLI.ShowInformationAndSetExitCode(
                        "Not accessible (server not running)",
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

        void ListResult(AdminCLI cli, Database[] result) {
            var table = new KeyValueTable();
            var rows = new Dictionary<string, string>();
            var engines = cli.GetEngines();

            foreach (var database in result) {
                var engine = engines.FirstOrDefault((candidate) => {
                    return candidate.Database.Name.Equals(database.Name, StringComparison.InvariantCultureIgnoreCase);
                });
                var pid = engine != null && engine.CodeHostProcess.PID != 0 ? engine.CodeHostProcess.PID.ToString() : "n/a";
                var apps = engine != null && engine.CodeHostProcess.PID != 0 ? engine.Executables.Executing.Count.ToString() : "n/a";

                ConsoleUtil.ToConsoleWithColor(database.Name, ConsoleColor.DarkYellow);
                rows.Clear();
                rows.Add("Running", engine != null ? bool.TrueString : bool.FalseString);
                rows.Add("Host process ID", pid);
                rows.Add("Applications", apps);
                table.Write(rows);
                Console.WriteLine();
            }
        }
    }
}