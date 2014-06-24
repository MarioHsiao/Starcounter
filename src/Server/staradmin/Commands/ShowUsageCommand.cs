using Starcounter.CLI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace staradmin.Commands {

    internal class ShowUsageCommand : ICommand {

        public void Execute() {
            var writer = Console.Out;
            var table = new KeyValueTable() { LeftMargin = 2, ColumnSpace = 4 };
            var rows = new Dictionary<string, string>();

            writer.WriteLine("Usage: staradmin [options] <command> [<command options>] [<parameters>]");
            writer.WriteLine();

            table.Title = "Options:";
            rows.Add(string.Format("--{0}", CommandLine.Options.Help.Name), CommandLine.Options.Help.ShortText);
            rows.Add(string.Format("--{0}=<name>", CommandLine.Options.Database.Name), CommandLine.Options.Database.ShortText);
            rows.Add(string.Format("--{0}=<host>", CommandLine.Options.ServerHost.Name), CommandLine.Options.ServerHost.ShortText);
            rows.Add(string.Format("--{0}=<port>", CommandLine.Options.ServerPort.Name), CommandLine.Options.ServerPort.ShortText);
            table.Write(rows);
            writer.WriteLine();

            table.Title = "Commands:";
            rows.Clear();
            var sortedCommands = CommandFactory.UserCommands.OrderBy(s => s.Info.Name);
            foreach (var userCommand in sortedCommands) {
                rows.Add(userCommand.Info.Name, userCommand.Info.ShortText);
            }
            table.Write(rows);
        }
    }
}