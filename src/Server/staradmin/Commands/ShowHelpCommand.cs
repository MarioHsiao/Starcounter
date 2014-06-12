
using System;
using System.IO;

namespace staradmin.Commands {
    /// <summary>
    /// Govern the dispalying of help for a given topic.
    /// </summary>
    internal class ShowHelpCommand : ICommand {
        /// <summary>
        /// The topic to show help for, usually a staradmin
        /// command.
        /// </summary>
        public readonly string Topic;

        public ShowHelpCommand(string topic) {
            Topic = topic;
        }

        public void Execute() {
            if (TopicIs(CommandLine.Commands.Help)) {
                ShowHelpOnHelp(Console.Out);
            } else {
                throw new NotImplementedException();
            }
        }

        void ShowHelpOnHelp(TextWriter writer) {
            writer.WriteLine("Displays help on a specified topic, usually a command.");
            writer.WriteLine();
            writer.WriteLine("Topics:");
            var formatting = "  {0,-22}{1,25}";
            writer.WriteLine(formatting, string.Format("{0}", CommandLine.Commands.Help.Name), "Displays this help");
            writer.WriteLine();
            writer.WriteLine("To view the overall help, use staradmin --{0}", CommandLine.Options.Help.Name);
        }

        bool TopicIs(CommandLine.Command command) {
            return Topic.Equals(command.Name, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
