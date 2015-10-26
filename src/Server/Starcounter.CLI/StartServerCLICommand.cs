
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
            if (this.Parent != null) {
                RunWithinContext();
                return;
            }

            try {
                Status.StartNewJob("Starting server");
                RunWithinContext();
                ShowStartResultAndSetExitCode(this.Node);
            } catch (Exception e) {
                SharedCLI.ShowErrorAndSetExitCode(e, true, false);
                WriteErrorLogsToConsoleAfterRun = true;
            }
        }

        private void RunWithinContext() {
            ShowStatus("retrieving server status", true);
            if (!ServerServiceProcess.IsOnline()) {
                ShowStatus("starting server");
                ServerServiceProcess.StartInteractiveOnDemand();
            }
            ShowStatus("server is running", true);
        }

        void ShowStartResultAndSetExitCode(Node node) {
            var description = string.Format("admin port {0}", node.PortNumber);
            Status.CompleteWithFinalJob("Started", description);
            Environment.ExitCode = 0;
        }
    }
}