
using Starcounter.CommandLine;
using Starcounter.Internal;
using Starcounter.Server;
using System;

namespace Starcounter.CLI {
    /// <summary>
    /// Implements a <see cref="CLIClientCommand"/> that support
    /// starting the server.
    /// </summary>
    public class StartServerCLICommand : CLIClientCommand {
        private StartServerCLICommand() : base() { }

        /// <summary>
        /// Factory method supporting the creation of this command.
        /// </summary>
        /// <param name="args">Optional well-known CLI-level arguments.</param>
        /// <returns>A new command</returns>
        public static StartServerCLICommand Create(ApplicationArguments args = null) {
            args = args ?? ApplicationArguments.Empty;
            var cmd = new StartServerCLICommand();
            cmd.ResolveServer(args);
            return cmd;
        }

        /// <summary>
        /// Runs the current command.
        /// </summary>
        /// <seealso cref="CLIClientCommand.Run"/>
        protected override void Run() {
            try {
                Status.StartNewJob("Starting server");

                // What if it's not the personal one? Don't try anything fancy.
                // And what if the server service is running, we can at least verify
                // it, can't we?
                // TODO:

                if (StarcounterEnvironment.ServerNames.PersonalServer.Equals(ServerName, StringComparison.CurrentCultureIgnoreCase)) {
                    ShowStatus("retrieving server status", true);
                    if (!ServerServiceProcess.IsOnline()) {
                        ServerServiceProcess.StartInteractiveOnDemand();
                    }
                    ShowStatus("server is online", true);
                }

                ShowStartResultAndSetExitCode(this.Node);

            } catch (Exception e) {
                SharedCLI.ShowErrorAndSetExitCode(e, true, false);
                WriteErrorLogsToConsoleAfterRun = true;
                return;
            }
        }

        void ShowStartResultAndSetExitCode(Node node) {
            var description = string.Format("admin port {0}", node.PortNumber);
            Status.CompleteWithFinalJob("Started", description);
            Environment.ExitCode = 0;
        }
    }
}