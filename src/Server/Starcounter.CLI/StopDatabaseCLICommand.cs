
using Starcounter.CommandLine;
using Starcounter.Internal;
using Starcounter.Server;
using Starcounter.Server.Rest;
using System;
using Starcounter.Rest.ExtensionMethods;
using Starcounter.Server.Rest.Representations.JSON;

namespace Starcounter.CLI {
    /// <summary>
    /// Provides functionality for a client to stop hosts and databases.
    /// </summary>
    public class StopDatabaseCLICommand : CLIClientCommand {
        /// <summary>
        /// Indicates stopping of the code host only. Other processes
        /// running as part of the target database will remain.
        /// </summary>
        public bool StopCodeHostOnly { get; set; }

        internal StopDatabaseCLICommand()
            : base() {
        }

        /// <summary>
        /// Factory method used to create new commands.
        /// </summary>
        /// <param name="args">Optional well-known CLI-level arguments.</param>
        /// <returns>A new command</returns>
        public static StopDatabaseCLICommand Create(ApplicationArguments args = null) {
            args = args ?? ApplicationArguments.Empty;
            var cmd = new StopDatabaseCLICommand() {
                AdminAPI = new AdminAPI()
            };
            cmd.ResolveServer(args);
            return cmd;
        }

        /// <inheritdoc/>
        protected override void Run() {
            try {
                Status.StartNewJob(string.Format("Stopping {0}", DatabaseName.ToLowerInvariant()));
                
                if (StarcounterEnvironment.ServerNames.PersonalServer.Equals(ServerName, StringComparison.CurrentCultureIgnoreCase)) {
                    ShowStatus("retrieving server status", true);
                    if (!ServerServiceProcess.IsOnline()) {
                        SharedCLI.ShowErrorAndSetExitCode(ErrorCode.ToMessage(Error.SCERRSERVERNOTAVAILABLE), true);
                    }
                }

                var node = Node;
                var admin = AdminAPI;
                var uris = admin.Uris;
                var databaseName = DatabaseName.ToLowerInvariant();
                int statusCode;

                var response = node.GET(admin.FormatUri(uris.Engine, databaseName), null);
                statusCode = response.FailIfNotSuccessOr(404);
                if (statusCode == 404) {
                    var errorDetail = new ErrorDetail();
                    errorDetail.PopulateFromJson(response.Body);
                    switch (errorDetail.ServerCode) {
                        case Error.SCERRDATABASENOTFOUND:
                        case Error.SCERRDATABASEENGINENOTRUNNING:
                            var notAccessible = ErrorCode.ToMessage((uint)errorDetail.ServerCode, string.Format("Database: \"{0}\".", databaseName));
                            SharedCLI.ShowErrorAndSetExitCode(notAccessible, true);
                            break;
                        default:
                            var other404 = ErrorCode.ToMessage((uint)errorDetail.ServerCode, string.Format("Text from server: \"{0}\".", errorDetail.Text));
                            SharedCLI.ShowErrorAndSetExitCode(other404, true);
                            break;
                    }
                }

                var engine = new Engine();
                engine.PopulateFromJson(response.Body);

                if (StopCodeHostOnly && engine.CodeHostProcess.PID == 0) {
                    var notRunning = ErrorCode.ToMessage(Error.SCERRDATABASEENGINENOTRUNNING, string.Format("Database: \"{0}\".", databaseName));
                    SharedCLI.ShowErrorAndSetExitCode(notRunning, true);
                }

                var uri = StopCodeHostOnly ? engine.CodeHostProcess.Uri : engine.Uri;
                
                response = node.DELETE(node.ToLocal(uri), (string)null, null);
                response.FailIfNotSuccess();

                Status.CompleteJob("stopped");

            } catch (Exception e) {
                SharedCLI.ShowErrorAndSetExitCode(e, true, false);
                WriteErrorLogsToConsoleAfterRun = true;
                return;
            }
        }


    }
}