
using Starcounter.CLI;
using Starcounter.CommandLine;
using Starcounter.CommandLine.Syntax;
using System;
using System.Collections.Generic;
using System.IO;

namespace staradmin.Commands {

    internal abstract class DeleteCommand : ContextAwareCommand {

        public class UserCommand : IUserCommand {
            CommandLine.Command delete = new CommandLine.Command() {
                Name = "delete",
                ShortText = "Deletes various types of objects, e.g. databases or logs.",
                Usage = "staradmin delete [--force] <type>"
            };

            public CommandSyntaxDefinition Define(ApplicationSyntaxDefinition appSyntax) {
                var cmd = appSyntax.DefineCommand(delete.Name, delete.ShortText, 0, 3);
                cmd.DefineFlag("force", "Force the delete, without any confirmation.");
                return cmd;
            }

            public CommandLine.Command Info {
                get { return delete; }
            }

            public ICommand CreateCommand(ApplicationArguments args) {
                if (args.CommandParameters.Count == 0) {
                    var helpOnDelete = ShowHelpCommand.CreateAsInternalHelp(delete.Name);
                    return new ReportBadInputCommand("Specify the type of artifact to delete.", helpOnDelete);
                }

                var type = args.CommandParameters[0];
                var typeToDelete = DeleteCommand.GetTypeToDelete(type);

                ICommand command = null;
                switch (typeToDelete) {
                    case ObjectType.ServerLog:
                        throw new NotImplementedException();
                    case ObjectType.Database:
                        throw new NotImplementedException();
                    default:
                        command = CreateUnrecognizedType(type);
                        break;
                }

                var deleteCommand = command as DeleteCommand;
                if (deleteCommand != null) {
                    deleteCommand.FactoryCommand = this;
                    deleteCommand.TypeToDelete = typeToDelete;
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
                rows.Add("db", "Deletes a database");
                rows.Add("log", "Deletes the server log");
                table.Write(rows);
            }

            internal ICommand CreateUnrecognizedType(string type) {
                var help = ShowHelpCommand.CreateAsInternalHelp(Info.Name);
                return new ReportBadInputCommand(string.Format("Don't know how to delete type '{0}'.", type), help);
            }
        }

        static ObjectType GetTypeToDelete(string type) {
            return type.ToObjectType();
        }

        protected UserCommand FactoryCommand { get; private set; }
        protected ObjectType TypeToDelete { get; private set; }
    }
}
