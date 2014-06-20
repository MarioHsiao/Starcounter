
using Starcounter.CLI;
using Starcounter.CommandLine;
using Starcounter.CommandLine.Syntax;
using System;
using System.Collections.Generic;
using System.IO;

namespace staradmin.Commands {
    /// <summary>
    /// Govern the dispalying of help for a given topic.
    /// </summary>
    internal class ShowHelpCommand : ICommand {
        /// <summary>
        /// Govern metadata provision about this class as a user command
        /// and defines the factory method to create the actual command
        /// to be executed.
        /// </summary>
        public class UserCommand : IUserCommand {
            readonly CommandLine.Command help = new CommandLine.Command() {
                Name = "help",
                ShortText = "Shows help about a command",
                Usage = "staradmin help [topic]"
            };

            public CommandSyntaxDefinition Define(ApplicationSyntaxDefinition appSyntax) {
                return appSyntax.DefineCommand(help.Name, help.ShortText, 0, 1);
            }

            public CommandLine.Command Info {
                get { return help; }
            }

            public ICommand CreateCommand(ApplicationArguments args) {
                var topic = args.CommandParameters.Count == 0 ? help.Name : args.CommandParameters[0];
                return new ShowHelpCommand(this, topic);
            }

            public void WriteHelp(ShowHelpCommand helpCommand, TextWriter writer) {
                if (!helpCommand.SupressHeader) {
                    writer.WriteLine("Displays help on a specified topic, usually a command.");
                    writer.WriteLine();
                }

                var table = new KeyValueTable() { LeftMargin = 2, ColumnSpace = 4 };
                var rows = new Dictionary<string, string>();
                table.Title = "Topics:";
                foreach (var userCommand in CommandFactory.UserCommands) {
                    rows.Add(userCommand.Info.Name, userCommand.Info.ShortText);
                }
                
                table.Write(rows);
                writer.WriteLine();

                writer.WriteLine("To view the overall help, use staradmin --{0}", CommandLine.Options.Help.Name);
            }
        }

        public static ShowHelpCommand CreateAsInternalHelp(string topic) {
            var factory = CommandFactory.UserCommands.Find((candidate) => {
                return candidate.GetType() == typeof(ShowHelpCommand.UserCommand);
            });

            var cmd = new ShowHelpCommand(factory as ShowHelpCommand.UserCommand, topic);
            cmd.SupressHeader = true;
            return cmd;
        }

        readonly UserCommand userCommand;

        /// <summary>
        /// Indicates the header of a certain topic should be
        /// supressed.
        /// </summary>
        public bool SupressHeader { get; set; }

        /// <summary>
        /// The topic to show help for, usually a staradmin
        /// command.
        /// </summary>
        public readonly string Topic;

        ShowHelpCommand(UserCommand cmd, string topic) {
            userCommand = cmd;
            Topic = topic;
            SupressHeader = false;
        }

        public void Execute() {
            var userCommand = CommandFactory.UserCommands.Find((candidate) => {
                return Topic.Equals(candidate.Info.Name, StringComparison.InvariantCultureIgnoreCase);
            });
            if (userCommand != null) {
                userCommand.WriteHelp(this, Console.Out);
            }
            else {
                ReportUnrecognizedTopic();
            }
        }

        void ReportUnrecognizedTopic() {
            var helpOnHelp = new ShowHelpCommand(this.userCommand, userCommand.Info.Name) { SupressHeader = true };
            var badInput = new ReportBadInputCommand(string.Format("Help for topic '{0}' not found.", Topic), helpOnHelp);
            badInput.Execute();
        }
    }
}
