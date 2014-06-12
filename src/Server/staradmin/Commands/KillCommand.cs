
using Starcounter.Internal;
using System;
using System.Diagnostics;

namespace staradmin.Commands {

    internal class KillCommand : ICommand {
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

        readonly int TimeoutInMilliseconds;
        readonly string Target;

        public KillCommand(string target) {
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
            var helpOnKill = new ShowHelpCommand(CommandLine.Commands.Kill.Name);
            var badInput = new ReportBadInputCommand(string.Format("Don't know how to kill '{0}'.", Target), helpOnKill);
            badInput.Execute();
        }
    }
}
