
using Starcounter.CommandLine;
using Starcounter.CommandLine.Syntax;
using Starcounter.Internal;
using System;
using System.Diagnostics;

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
        }

        static string[] knownStarcounterProcessNames = new string[] {
            StarcounterConstants.ProgramNames.ScService,
            StarcounterConstants.ProgramNames.ScIpcMonitor,
            StarcounterConstants.ProgramNames.ScNetworkGateway,
            StarcounterConstants.ProgramNames.ScAdminServer,
            StarcounterConstants.ProgramNames.ScCode,
            StarcounterConstants.ProgramNames.ScData,
            StarcounterConstants.ProgramNames.ScDbLog,
            "scnetworkgatewayloopedtest",
            StarcounterConstants.ProgramNames.ScWeaver,
            StarcounterConstants.ProgramNames.ScSqlParser,
            StarcounterConstants.ProgramNames.ScTrayIcon,
            "ServerLogTail"
        };

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

            foreach (var name in knownStarcounterProcessNames) {
                foreach (var proc in Process.GetProcessesByName(name)) {
                    try {
                        proc.Kill();
                        proc.WaitForExit(timeout);

                        // Shape this up - at least don't just throw an exception.
                        // TODO:

                        if (!proc.HasExited) {
                            var processCantBeKilled = "Process " + proc.ProcessName + " can not be killed." + Environment.NewLine +
                                "Please shutdown the corresponding application explicitly.";
                            throw new Exception(processCantBeKilled);

                        } else {
                            Console.WriteLine(DateTime.Now.TimeOfDay + ": process '" + name + "' successfully killed!");
                        }

                    } finally { proc.Close(); }
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
