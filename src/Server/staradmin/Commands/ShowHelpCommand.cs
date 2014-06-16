
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
                return new ShowHelpCommand(topic);
            }
        }

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

        public ShowHelpCommand(string topic) {
            Topic = topic;
            SupressHeader = false;
        }

        public void Execute() {
            if (TopicIs(CommandLine.Commands.Help)) {
                ShowHelpOnHelp(Console.Out);
            } 
            else if (TopicIs(CommandLine.Commands.Kill)) {
                ShowHelpOnKill(Console.Out);
            } 
            else if (TopicIs(CommandLine.Commands.Unload)) {
                ShowHelpOnUnload(Console.Out);
            }
            else {
                ReportUnrecognizedTopic();
            }
        }

        void ReportUnrecognizedTopic() {
            var helpOnHelp = new ShowHelpCommand(CommandLine.Commands.Help.Name) { SupressHeader = true };
            var badInput = new ReportBadInputCommand(string.Format("Help for topic '{0}' not found.", Topic), helpOnHelp);
            badInput.Execute();
        }

        void ShowHelpOnHelp(TextWriter writer) {
            if (!SupressHeader) {
                writer.WriteLine("Displays help on a specified topic, usually a command.");
                writer.WriteLine();
            }

            var table = new KeyValueTable() { LeftMargin = 2, ColumnSpace = 4 };
            var rows = new Dictionary<string, string>();
            table.Title = "Topics:";
            
            rows.Add(CommandLine.Commands.Help.Name, "Display help on the help command");
            rows.Add(CommandLine.Commands.Kill.Name, CommandLine.Commands.Kill.ShortText);
            rows.Add(CommandLine.Commands.Unload.Name, CommandLine.Commands.Unload.ShortText);
            table.Write(rows);
            writer.WriteLine();

            writer.WriteLine("To view the overall help, use staradmin --{0}", CommandLine.Options.Help.Name);
        }

        void ShowHelpOnKill(TextWriter writer) {
            var cmd = CommandLine.Commands.Kill;

            if (!SupressHeader) {
                writer.WriteLine(cmd.ShortText);
                writer.WriteLine();
            }
            writer.WriteLine("Usage: {0}", cmd.Usage);

            var table = new KeyValueTable() { LeftMargin = 2, ColumnSpace = 4 };
            var rows = new Dictionary<string, string>();
            table.Title = "Targets:";
            rows.Add("all", "Kills all processes relating to Starcounter on the current machine. Use this option with care and make sure no mission-critical processes are running.");
            table.Write(rows);
        }

        void ShowHelpOnUnload(TextWriter writer) {
            var cmd = CommandLine.Commands.Unload;

            if (!SupressHeader) {
                writer.WriteLine(cmd.ShortText);
                writer.WriteLine();
            }
            writer.WriteLine("Usage: {0}", cmd.Usage);

            var table = new KeyValueTable() { LeftMargin = 2, ColumnSpace = 4 };
            var rows = new Dictionary<string, string>();
            table.Title = "Sources:";
            rows.Add("db", "Unloads a database. If no source is given, 'db' is used as the default.");
            table.Write(rows);

            table.Title = "Examples:";
            table.RowSpace = 1;
            rows.Clear();
            rows.Add("staradmin unload db", "Unloads the default database into the default unload file.");
            rows.Add("staradmin --d=foo unload db", "Unloads the 'foo' database into the default unload file.");
            rows.Add("staradmin --d=bar unload db --file=data.sql", "Unloads the 'bar' database into the 'data.sql' file, resolved to the same directory from which the command runs.");
            rows.Add("staradmin unload", "Shorthand for 'staradmin unload db'");
            writer.WriteLine();
            table.Write(rows);
        }

        bool TopicIs(CommandLine.Command command) {
            return Topic.Equals(command.Name, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
