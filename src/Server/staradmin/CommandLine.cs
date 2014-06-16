
using Starcounter.CLI;
using Starcounter.CommandLine;
using Starcounter.CommandLine.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace staradmin {

    internal static class CommandLine {
        public class Option {
            public string Name { get; set; }
            public string ShortText { get; set; }
            public string Usage { get; set; }
            public string[] Alternatives { get; set; }
        }
        public class Command : Option {
            string fullUsage = null;
            public string FullUsage {
                get { return fullUsage ?? Usage; }
                set { fullUsage = value; }
            }
        }

        public static class Options {
            public static Option Help = new Option() {
                Name = "help",
                ShortText = "Shows help about staradmin.exe",
                Alternatives = new string[] { "h" }
            };
            public static Option Database = new Option() {
                Name = SharedCLI.Option.Db,
                ShortText = "Specifies the database commands run against",
                Alternatives = new string[] { "d", "db" }
            };
        }

        public static class Commands {
            public static Command Help = new Command() {
                Name = "help",
                ShortText = "Shows help about a command"
            };
            public static Command Kill = new Command() {
                Name = "kill",
                ShortText = "Kills processes relating to Starcounter",
                Usage = "staradmin kill <target>"
            };
            public static Command Unload = new Command() {
                Name = "unload",
                ShortText = "Unloads data from a data source, usually a database",
                Usage = "staradmin unload [--file=path] [source] [arguments]"
            };
        }

        public static void PreParse(ref string[] args) {
            if (args.Length > 0) {
                var first = args[0].TrimStart('-');
                if (first.Equals(SharedCLI.UnofficialOptions.Debug, StringComparison.InvariantCultureIgnoreCase)) {
                    Debugger.Launch();
                    var stripped = new string[args.Length - 1];
                    Array.Copy(args, 1, stripped, 0, args.Length - 1);
                    args = stripped;
                }
            }
        }

        public static ApplicationArguments Parse(string[] args) {
            if (args.Length == 0) {
                return ApplicationArguments.Empty;
            }

            var syntax = Define();
            try {
                return new Parser(args).Parse(syntax);
            } catch (InvalidCommandLineException e) {
                ConsoleUtil.ToConsoleWithColor(e.Message, ConsoleColor.Red);
                Environment.Exit((int)e.ErrorCode);
            }

            // Can never happen, but doesn't compile without this
            return null;
        }

        public static void WriteUsage(TextWriter writer) {
            var table = new KeyValueTable() { LeftMargin = 2, ColumnSpace = 4 };
            var rows = new Dictionary<string, string>();

            writer.WriteLine("Usage: staradmin [options] <command> [<command options>] [<parameters>]");
            writer.WriteLine();

            table.Title = "Options:";
            rows.Add(string.Format("--{0}", Options.Help.Name), Options.Help.ShortText);
            rows.Add(string.Format("--{0}=<value>", Options.Database.Name), Options.Database.ShortText);
            table.Write(rows);
            writer.WriteLine();

            table.Title = "Commands:";
            rows.Clear();
            rows.Add(Commands.Help.Name, Commands.Help.ShortText);
            rows.Add(Commands.Kill.Name, Commands.Kill.ShortText);
            rows.Add(Commands.Unload.Name, Commands.Unload.ShortText);
            table.Write(rows);
        }

        static IApplicationSyntax Define() {
            var appSyntax = new ApplicationSyntaxDefinition();
            appSyntax.ProgramDescription = "staradmin.exe";

            appSyntax.DefineFlag(
                Options.Help.Name, 
                Options.Help.ShortText,
                OptionAttributes.Default,
                Options.Help.Alternatives
                );
            appSyntax.DefineProperty(
                Options.Database.Name,
                Options.Database.ShortText,
                OptionAttributes.Default,
                Options.Database.Alternatives
                );

            var commandSyntax = appSyntax.DefineCommand(Commands.Help.Name, Commands.Help.ShortText);
            commandSyntax.MinParameterCount = 0;
            commandSyntax.MaxParameterCount = 1;

            commandSyntax = appSyntax.DefineCommand(Commands.Kill.Name, Commands.Kill.ShortText, 1);

            commandSyntax = appSyntax.DefineCommand(Commands.Unload.Name, Commands.Unload.ShortText);
            commandSyntax.MinParameterCount = 0;
            commandSyntax.MaxParameterCount = 2;
            commandSyntax.DefineProperty("file", "Specifies the file to unload into");

            return appSyntax.CreateSyntax();
        }
    }
}