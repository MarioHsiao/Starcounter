
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
                ShortText = "Deletes various types of objects, e.g. databases.",
                Usage = "staradmin delete [--force] [--failMissing] <type>"
            };

            public CommandSyntaxDefinition Define(ApplicationSyntaxDefinition appSyntax) {
                var cmd = appSyntax.DefineCommand(delete.Name, delete.ShortText, 0, 3);
                cmd.DefineFlag("force", "Force the delete, without any confirmation.");
                cmd.DefineFlag("failMissing", "Make the delete fail if the artifact is missing.");
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
                    case ObjectType.Database:
                        command = new DeleteDatabaseCommand();
                        break;
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
        protected bool Force { get; private set; }
        protected bool FailIfMissing { get; private set; }

        protected abstract void Delete();

        public override void Execute() {
            Force = Context.ContainsCommandFlag("force");
            FailIfMissing = Context.ContainsCommandFlag("failMissing");
            Delete();
        }
    }
}
