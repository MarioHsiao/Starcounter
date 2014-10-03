using Sc.Tools.Logging;
using Starcounter.CLI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace staradmin.Commands {

    internal class ListLogsCommand : ListCommand {
        
        protected override void List() {
            int count = MaxItems.HasValue ? MaxItems.Value : 25;
            var types = Severity.Notice;
            string source = null;
            var context = Context;

            // staradmin list log [severity] [source-filter]

            ICommand error = null;

            if (context.CommandParameters.Count > 1) {
                var t = context.CommandParameters[1];
                switch (t) {
                    case "d":
                    case "debug":
                    case "all":
                    case "any":
                        types = Severity.Debug;
                        break;
                    case "n":
                    case "notice":
                    case "info":
                        types = Severity.Notice;
                        break;
                    case "w":
                    case "warning":
                    case "warnings":
                        types = Severity.Warning;
                        break;
                    case "e":
                    case "error":
                    case "errors":
                        types = Severity.Error;
                        break;
                    default:
                        var help = ShowHelpCommand.CreateAsInternalHelp(FactoryCommand.Info.Name);
                        error = new ReportBadInputCommand(string.Format("Invalid log severity given: '{0}'.", t), help);
                        break;
                };
            }

            if (context.CommandParameters.Count > 2) {
                source = context.CommandParameters[2];
            }

            if (error != null) {
                error.Execute();
                return;
            }

            string databaseFilter = null;
            if (!context.TryGetGlobalProperty(staradmin.CommandLine.Options.Database.Name, out databaseFilter)) {
                databaseFilter = null;
            }

            // Read and filter the log and send the result
            // to the console
            try {
                var console = new LogConsole();
                var reader = new FilterableLogReader() {
                    Count = count,
                    TypeOfLogs = types,
                    Source = source,
                    Database = databaseFilter
                };
                var capacity = Math.Min(count, 200);
                var stack = new Stack<LogEntry>(capacity);

                reader.Fetch((log) => { stack.Push(log); });
                foreach (var entry in stack) {
                    console.Write(entry);
                }

            } catch (Exception e) {
                ConsoleUtil.ToConsoleWithColor(string.Format("Failed getting logs: {0}", e.Message), ConsoleColor.Red);
            }
        }
    }
}
