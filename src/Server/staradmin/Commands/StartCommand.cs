
using Starcounter.CLI;
using Starcounter.CommandLine;
using Starcounter.CommandLine.Syntax;
using System.Collections.Generic;
using System.IO;

namespace staradmin.Commands {

    internal abstract class StartCommand : ContextAwareCommand {

        public class UserCommand : IUserCommand {
            CommandLine.Command start = new CommandLine.Command() {
                Name = "start",
                ShortText = "Starts various types of objects, e.g. databases or the server.",
                Usage = "staradmin [-d=<name>] start <type>"
            };

            public CommandSyntaxDefinition Define(ApplicationSyntaxDefinition appSyntax) {
                var cmd = appSyntax.DefineCommand(start.Name, start.ShortText, 0, 2);
                return cmd;
            }

            public CommandLine.Command Info {
                get { return start; }
            }

            public ICommand CreateCommand(ApplicationArguments args) {
                if (args.CommandParameters.Count == 0) {
                    var helpOnStart = ShowHelpCommand.CreateAsInternalHelp(start.Name);
                    return new ReportBadInputCommand("Specify the type of object to start.", helpOnStart);
                }

                var type = args.CommandParameters[0];
                var typeToStart = StartCommand.GetTypeToStart(type);

                ICommand command = null;
                switch (typeToStart) {
                    case ObjectType.Server:
                        command = new StartServerCommand();
                        break;
                    case ObjectType.Database:
                        command = new StartDatabaseCommand();
                        break;
                    default:
                        command = CreateUnrecognizedType(type);
                        break;
                }

                var startCommand = command as StartCommand;
                if (startCommand != null) {
                    startCommand.FactoryCommand = this;
                    startCommand.TypeToStart = typeToStart;
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
                rows.Add("db", "Start a database");
                rows.Add("server", "Starts the server");
                table.Write(rows);
            }

            internal ICommand CreateUnrecognizedType(string type) {
                var help = ShowHelpCommand.CreateAsInternalHelp(Info.Name);
                return new ReportBadInputCommand(string.Format("Don't know how to start type '{0}'.", type), help);
            }
        }

        static ObjectType GetTypeToStart(string type) {
            return type.ToObjectType();
        }

        protected UserCommand FactoryCommand { get; private set; }
        protected ObjectType TypeToStart { get; private set; }

        protected abstract void Start();

        public override void Execute() {
            Start();
        }
    }
}
