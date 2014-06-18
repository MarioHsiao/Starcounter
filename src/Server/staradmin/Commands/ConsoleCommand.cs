using Starcounter.CLI;
using Starcounter.CommandLine;
using Starcounter.CommandLine.Syntax;
using Starcounter.Internal;
using System;
using System.Collections.Generic;
using System.IO;

namespace staradmin.Commands {

    internal class ConsoleCommand : ContextAwareCommand {
        public class UserCommand : IUserCommand {
            readonly CommandLine.Command console = new CommandLine.Command() {
                Name = "console",
                ShortText = "Shows console output from applications",
                Usage = "staradmin console [databases]"
            };

            public CommandSyntaxDefinition Define(ApplicationSyntaxDefinition appSyntax) {
                var syntax = appSyntax.DefineCommand(console.Name, console.ShortText);
                syntax.MinParameterCount = 0;
                return syntax;
            }

            public CommandLine.Command Info {
                get { return console; }
            }

            public ICommand CreateCommand(ApplicationArguments args) {
                var sources = args.CommandParameters.Count == 0 
                    ? new string[] { StarcounterConstants.DefaultDatabaseName } 
                    : args.CommandParameters.ToArray();
                return new ConsoleCommand(this, sources);
            }

            public void WriteHelp(ShowHelpCommand helpCommand, TextWriter writer) {
                if (!helpCommand.SupressHeader) {
                    writer.WriteLine(Info.ShortText);
                    writer.WriteLine();
                }
                writer.WriteLine("Usage: {0}", Info.Usage);

                var table = new KeyValueTable() { LeftMargin = 2, ColumnSpace = 4 };
                var rows = new Dictionary<string, string>();
                table.Title = "Examples:";
                table.RowSpace = 1;
                rows.Add("staradmin console", "Shows console output from the default database");
                rows.Add("staradmin --d=foo console", "Shows console output from the 'foo' database");
                rows.Add("staradmin console foo", "Shows console output from the 'foo' database");
                rows.Add("staradmin console foo bar baz", "Shows console output from the 'foo', 'bar' and 'baz' databases");
                writer.WriteLine();
                table.Write(rows);
            }
        }

        ConsoleCommand(UserCommand cmd, string[] sources) {
        }

        public override void Execute() {
            throw new NotImplementedException();
        }
    }
}
