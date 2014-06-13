﻿
using Starcounter.CLI;
using System;
using System.Collections.Generic;
using System.IO;

namespace staradmin.Commands {
    /// <summary>
    /// Govern the dispalying of help for a given topic.
    /// </summary>
    internal class ShowHelpCommand : ICommand {
        bool HelpOnHelpSupressHeader = false;

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
            } 
            else if (TopicIs(CommandLine.Commands.Kill)) {
                ShowHelpOnKill(Console.Out);
            }
            else {
                ReportUnrecognizedTopic();
            }
        }

        void ReportUnrecognizedTopic() {
            var helpOnHelp = new ShowHelpCommand(CommandLine.Commands.Help.Name) { HelpOnHelpSupressHeader = true };
            var badInput = new ReportBadInputCommand(string.Format("Help for topic '{0}' not found.", Topic), helpOnHelp);
            badInput.Execute();
        }

        void ShowHelpOnHelp(TextWriter writer) {
            if (!HelpOnHelpSupressHeader) {
                writer.WriteLine("Displays help on a specified topic, usually a command.");
                writer.WriteLine();
            }

            var table = new KeyValueTable() { LeftMargin = 2, ColumnSpace = 4 };
            var rows = new Dictionary<string, string>();
            table.Title = "Topics:";
            
            rows.Add(CommandLine.Commands.Help.Name, "Display help on the help command");
            rows.Add(CommandLine.Commands.Kill.Name, CommandLine.Commands.Kill.ShortText);
            table.Write(rows);
            writer.WriteLine();

            writer.WriteLine("To view the overall help, use staradmin --{0}", CommandLine.Options.Help.Name);
        }

        void ShowHelpOnKill(TextWriter writer) {
            var cmd = CommandLine.Commands.Kill;

            writer.WriteLine(cmd.ShortText);
            writer.WriteLine();
            writer.WriteLine("Usage: {0}", cmd.Usage);

            var table = new KeyValueTable() { LeftMargin = 2, ColumnSpace = 4 };
            var rows = new Dictionary<string, string>();
            table.Title = "Targets:";
            rows.Add("all", "Kills all processes relating to Starcounter on the current machine. Use this option with care and make sure no mission-critical processes are running.");
            table.Write(rows);
        }

        bool TopicIs(CommandLine.Command command) {
            return Topic.Equals(command.Name, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
