using Starcounter.CLI;
using Starcounter.CommandLine;
using Starcounter.CommandLine.Syntax;
using System;
using System.Collections.Generic;
using System.IO;

namespace staradmin.Commands {

    internal abstract class NewCommand : ContextAwareCommand, ICommand {

        public class UserCommand : IUserCommand {
            CommandLine.Command newCommand = new CommandLine.Command() {
                Name = "new",
                ShortText = "Creates various types of objects, e.g. databases or applications.",
                Usage = "staradmin new <type|template>"
            };

            public CommandSyntaxDefinition Define(ApplicationSyntaxDefinition appSyntax) {
                var cmd = appSyntax.DefineCommand(newCommand.Name, newCommand.ShortText, 0, int.MaxValue);
                return cmd;
            }

            public CommandLine.Command Info {
                get { return newCommand; }
            }

            public ICommand CreateCommand(ApplicationArguments args) {
                if (args.CommandParameters.Count == 0) {
                    var helpOnNew = ShowHelpCommand.CreateAsInternalHelp(newCommand.Name);
                    return new ReportBadInputCommand("Specify the type of object to create.", helpOnNew);
                }

                var type = args.CommandParameters[0];
                var typeToCreate = NewCommand.GetTypeToCreate(type);

                ICommand command = null;
                switch (typeToCreate) {
                    case ObjectType.Application:
                        var templateName = args.CommandParameters.Count > 1
                            ? args.CommandParameters[1]
                            : CLITemplate.DefaultTemplateName;
                        command = new NewAppCommand(templateName);
                        break;
                    case ObjectType.Database:
                        command = new NewDatabaseCommand();
                        break;
                    default:
                    
                        // Lets try if its the name of a CLI template,
                        // indicating we should create a new applicaton
                        // from it
                        
                        var template = CLITemplate.GetTemplate(type);
                        if (template != null) {
                            command = new NewAppCommand(template);
                        } else {
                            command = CreateUnrecognizedType(type);
                        }
                        break;
                }

                var newCmd = command as NewCommand;
                if (newCmd != null) {
                    newCmd.FactoryCommand = this;
                    newCmd.TypeToCreate = typeToCreate;
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
                rows.Add("app", "Create a new application (based on a template)");
                rows.Add("db", "Create a new database");
                table.Write(rows);

                Console.WriteLine();
                table.Title = "Examples:";
                table.RowSpace = 1;
                rows.Clear();
                rows.Add("staradmin new app", "Creates a new application from the default template, normally named '+'.");
                rows.Add("staradmin new mytemplate", "Creates a new application from 'mytemplate' template.");
                rows.Add("staradmin new app + foo", "Creates a new application from '+' template, naming it 'foo'");
                table.Write(rows);

                Console.WriteLine();
                Console.WriteLine("Running 'staradmin fubar' is the shorthand of 'staradmin new app fubar';");
                Console.WriteLine("it creates a new application based on the 'fubar' template. If the template");
                Console.WriteLine("does not exist, the staradmin usage message is displayed.");
            }

            internal ICommand CreateUnrecognizedType(string type) {
                var help = ShowHelpCommand.CreateAsInternalHelp(Info.Name);
                return new ReportBadInputCommand(string.Format("Don't know how to create new '{0}'.", type), help);
            }
        }

        static ObjectType GetTypeToCreate(string type) {
            return type.ToObjectType();
        }

        protected UserCommand FactoryCommand { get; private set; }
        protected ObjectType TypeToCreate { get; private set; }
        protected int? MaxItems { get; private set; }

        protected abstract void New();

        public override void Execute() {
            New();
        }
    }
}
