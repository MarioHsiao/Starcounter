﻿
using Starcounter.CLI;
using Starcounter.CommandLine;
using Starcounter.CommandLine.Syntax;
using System.Collections.Generic;
using System.IO;

namespace staradmin.Commands {

    internal abstract class StopCommand : ContextAwareCommand, ICommand {

        public class UserCommand : IUserCommand {
            CommandLine.Command stop = new CommandLine.Command() {
                Name = "stop",
                ShortText = "Stops various types of objects, e.g. databases or applications.",
                Usage = "staradmin stop <type>"
            };

            public CommandSyntaxDefinition Define(ApplicationSyntaxDefinition appSyntax) {
                var cmd = appSyntax.DefineCommand(stop.Name, stop.ShortText, 0, 1);
                return cmd;
            }

            public CommandLine.Command Info {
                get { return stop; }
            }

            public ICommand CreateCommand(ApplicationArguments args) {
                if (args.CommandParameters.Count == 0) {
                    var helpOnStop = ShowHelpCommand.CreateAsInternalHelp(stop.Name);
                    return new ReportBadInputCommand("Specify the type of objects to stop.", helpOnStop);
                }

                var type = args.CommandParameters[0];
                var typeToStop = StopCommand.GetTypeToStop(type);

                ICommand command = null;
                switch (typeToStop) {
                    case ObjectType.Application:
                    case ObjectType.Database:
                    case ObjectType.CodeHost:
                        command = new ReportBadInputCommand(string.Format("Stopping '{0}' is yet not implemented.", typeToStop.ToString()));
                        break;
                    default:
                        command = CreateUnrecognizedType(type);
                        break;
                }

                var listCommand = command as StopCommand;
                if (listCommand != null) {
                    listCommand.FactoryCommand = this;
                    listCommand.TypeToStop = typeToStop;
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
                rows.Add("db", "Stop a database");
                rows.Add("app", "Stop an application");
                rows.Add("host", "Stop a code host");
                table.Write(rows);
            }

            internal ICommand CreateUnrecognizedType(string type) {
                var help = ShowHelpCommand.CreateAsInternalHelp(Info.Name);
                return new ReportBadInputCommand(string.Format("Don't know how to stop type '{0}'.", type), help);
            }
        }

        static ObjectType GetTypeToStop(string type) {
            return type.ToObjectType();
        }

        protected UserCommand FactoryCommand { get; private set; }
        protected ObjectType TypeToStop { get; private set; }
        
        protected abstract void Stop();

        public override void Execute() {
            Stop();
        }
    }
}
