
using Starcounter.CLI;
using Starcounter.CommandLine;
using Starcounter.CommandLine.Syntax;
using System;
using System.Collections.Generic;
using System.IO;

namespace staradmin.Commands {

    internal class ListCommand : ICommand {

        public class UserCommand : IUserCommand {
            CommandLine.Command list = new CommandLine.Command() {
                Name = "list",
                ShortText = "List various types of objects, e.g. databases or applications.",
                Usage = "staradmin list [--max=count] <type>"
            };

            public CommandSyntaxDefinition Define(ApplicationSyntaxDefinition appSyntax) {
                var cmd = appSyntax.DefineCommand(list.Name, list.ShortText, 0, 3);
                cmd.DefineProperty("max", "Max number of entries to show.");
                return cmd;
            }

            public CommandLine.Command Info {
                get { return list; }
            }

            public ICommand CreateCommand(ApplicationArguments args) {
                if (args.CommandParameters.Count == 0) {
                    var helpOnList = ShowHelpCommand.CreateAsInternalHelp(list.Name);
                    return new ReportBadInputCommand("Specify the type of objects to list.", helpOnList);
                }

                return new ListCommand(this, args.CommandParameters[0]);
            }

            public void WriteHelp(ShowHelpCommand helpCommand, TextWriter writer) {
                if (!helpCommand.SupressHeader) {
                    writer.WriteLine(Info.ShortText);
                    writer.WriteLine();
                }
                writer.WriteLine("Usage: {0}", Info.Usage);

                var table = new KeyValueTable() { LeftMargin = 2, ColumnSpace = 4 };
                var rows = new Dictionary<string, string>();
                table.Title = "Types:";
                rows.Add("db", "List all databases");
                rows.Add("app", "List all applications");
                rows.Add("log", "List the content of the server log");
                table.Write(rows);
            }
        }

        readonly UserCommand userCommand;
        readonly string objectType;

        ListCommand(UserCommand cmd, string type) {
            userCommand = cmd;
            objectType = type;
        }

        public void Execute() {
            throw new NotImplementedException();
        }
    }
}
