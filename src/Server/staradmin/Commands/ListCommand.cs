
using Starcounter.CLI;
using Starcounter.CommandLine;
using Starcounter.CommandLine.Syntax;
using System;
using System.Collections.Generic;
using System.IO;

namespace staradmin.Commands {

    internal abstract class ListCommand : ContextAwareCommand, ICommand {

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

                var type = args.CommandParameters[0];
                var typeOfList = ListCommand.GetTypeOfList(type);

                ICommand command = null;
                switch (typeOfList) {
                    case ListType.Application:
                        command = new ListApplicationsCommand();
                        break;
                    case ListType.ServerLog:
                    case ListType.Database:
                    default:
                        command = CreateUnrecognizedType(type);
                        break;
                }

                var listCommand = command as ListCommand;
                if (listCommand != null) {
                    listCommand.FactoryCommand = this;
                    listCommand.TypeOfList = typeOfList;
                }

                return command;
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

            internal ICommand CreateUnrecognizedType(string type) {
                var help = ShowHelpCommand.CreateAsInternalHelp(Info.Name);
                return new ReportBadInputCommand(string.Format("Don't know how to list type '{0}'.", type), help);
            }
        }

        static ListType GetTypeOfList(string type) {
            switch (type.ToLowerInvariant()) {
                case "app":
                case "apps":
                case "application":
                case "applications":
                    return ListType.Application;
                default:
                    return ListType.Unknown;
            }
        }

        protected enum ListType {
            Unknown,
            Database,
            Application,
            ServerLog
        }

        protected UserCommand FactoryCommand { get; private set; }
        protected ListType TypeOfList { get; private set; }
        protected int? MaxItems { get; private set; }

        protected abstract void List();

        public override void Execute() {
            string value;
            if (Context.TryGetCommandProperty("max", out value)) {
                uint max;
                if (!uint.TryParse(value, out max)) {
                    var help = ShowHelpCommand.CreateAsInternalHelp(FactoryCommand.Info.Name);
                    var cmd = new ReportBadInputCommand(
                        string.Format("Invalid 'max' given: '{0}'.", value), help);
                    cmd.Execute();
                    return;
                }
                MaxItems = (int) max;
            }

            List();
        }
    }
}
