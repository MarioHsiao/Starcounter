
using Starcounter.CLI;
using Starcounter.CommandLine;
using Starcounter.CommandLine.Syntax;
using Starcounter.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace staradmin.Commands {

    internal class KillCommand : ICommand {

        public class UserCommand : IUserCommand {
            CommandLine.Command kill = new CommandLine.Command() {
                Name = "kill",
                ShortText = "Kills processes relating to Starcounter",
                Usage = "staradmin kill <target>"
            };

            public CommandSyntaxDefinition Define(ApplicationSyntaxDefinition appSyntax) {
                return appSyntax.DefineCommand(kill.Name, kill.ShortText, 1);
            }

            public CommandLine.Command Info {
                get { return kill; }
            }

            public ICommand CreateCommand(ApplicationArguments args) {
                return new KillCommand(this, args.CommandParameters[0]);
            }

            public void WriteHelp(ShowHelpCommand helpCommand, TextWriter writer) {
                if (!helpCommand.SupressHeader) {
                    writer.WriteLine(Info.ShortText);
                    writer.WriteLine();
                }
                writer.WriteLine("Usage: {0}", Info.Usage);

                var table = new KeyValueTable() { LeftMargin = 2, ColumnSpace = 4 };
                var rows = new Dictionary<string, string>();
                table.Title = "Targets:";
                rows.Add("all", "Kills all processes relating to Starcounter on the current machine. Use this option with care and make sure no mission-critical processes are running.");
                table.Write(rows);
            }
        }

        readonly UserCommand userCommand;
        readonly int TimeoutInMilliseconds;
        readonly string Target;
        

        private KillCommand(UserCommand cmd, string target) {
            userCommand = cmd;
            Target = target;
            TimeoutInMilliseconds = 2000;
        }

        public void Execute() {
            switch (Target.ToLowerInvariant()) {
                case "all":
                    KillAll();
                    break;
                default:
                    ReportUnrecognizedTarget();
                    break;
            };
        }

        void KillAll() {
            var timeout = TimeoutInMilliseconds;

            foreach (var name in StarcounterEnvironment.ScProcessesList) {

                // Trying 10 times to kill running process.
                for (Int32 i = 0; i < 10; i++) {

                    try {

                        foreach (var proc in Process.GetProcessesByName(name)) {

                            try {

                                proc.Kill();
                                proc.WaitForExit();

                                Console.WriteLine(DateTime.Now.TimeOfDay + ": process '" + name + "' successfully killed!");

                            } finally {
                                proc.Close();
                            }
                        }

                    } catch {
                        Console.Error.WriteLine("Failed to kill process: {0}. Retrying in 1 second...", name);
                        Thread.Sleep(1000);
                        continue;
                    }

                    break;
                }
            }
        }

        void ReportUnrecognizedTarget() {
            var helpOnKill = ShowHelpCommand.CreateAsInternalHelp(userCommand.Info.Name);
            var badInput = new ReportBadInputCommand(string.Format("Don't know how to kill '{0}'.", Target), helpOnKill);
            badInput.Execute();
        }
    }
}
