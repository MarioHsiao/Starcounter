
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
            public string[] Alternatives { get; set; }
        }
        public class Command : Option { }

        public static class Options {
            public static Option Help = new Option() {
                Name = "help",
                ShortText = "Shows help about staradmin.exe",
                Alternatives = new string[] { "h" }
            };
        }

        public static class Commands {
            public static Command Help = new Command() {
                Name = "help",
                ShortText = "Shows help about a command"
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
            writer.WriteLine("Usage: staradmin [options] <command> [<command options>] [<parameters>]");
            writer.WriteLine();
            writer.WriteLine("Options:");
            var formatting = "  {0,-22}{1,25}";
            writer.WriteLine(formatting, string.Format("--{0}", Options.Help.Name), Options.Help.ShortText);
            writer.WriteLine();
            writer.WriteLine("Commands:");
            writer.WriteLine(formatting, string.Format("{0}", Commands.Help.Name), Commands.Help.ShortText);
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

            var commandSyntax = appSyntax.DefineCommand(Commands.Help.Name, Commands.Help.ShortText);
            commandSyntax.MinParameterCount = 0;
            commandSyntax.MaxParameterCount = 1;

            return appSyntax.CreateSyntax();
        }
    }
}