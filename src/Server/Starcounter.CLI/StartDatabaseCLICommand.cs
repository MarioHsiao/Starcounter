
using Starcounter.CommandLine;
using Starcounter.Internal;
using Starcounter.Server;
using Starcounter.Server.Rest;
using System;
using Starcounter.Rest.ExtensionMethods;
using Starcounter.Server.Rest.Representations.JSON;

namespace Starcounter.CLI {
    using EngineReference = Starcounter.Server.Rest.Representations.JSON.EngineCollection.EnginesElementJson;
    using ExecutableReference = Starcounter.Server.Rest.Representations.JSON.Engine.ExecutablesJson.ExecutingElementJson;
    using Option = Starcounter.CLI.SharedCLI.Option;
    using UnofficialOption = Starcounter.CLI.SharedCLI.UnofficialOptions;

    /// <summary>
    /// Provides functionality for a client to stop hosts and databases.
    /// </summary>
    public class StartDatabaseCLICommand : CLIClientCommand {
        internal StartDatabaseCLICommand() : base() {}

        /// <summary>
        /// Factory method used to create new commands.
        /// </summary>
        /// <param name="args">Optional well-known CLI-level arguments.</param>
        /// <returns>A new command</returns>
        public static StartDatabaseCLICommand Create(ApplicationArguments args = null) {
            args = args ?? ApplicationArguments.Empty;
            
            string databaseName;
            SharedCLI.ResolveDatabase(args, out databaseName);

            var cmd = new StartDatabaseCLICommand() {
                DatabaseName = databaseName,
                AdminAPI = new AdminAPI(),
                CLIArguments = args,
            };
            
            cmd.ResolveServer(args);
            return cmd;
        }

        /// <inheritdoc/>
        protected override void Run() {
            if (this.Parent != null) {
                RunWithinContext();
                return;
            }

            ResponseExtensions.OnUnexpectedResponse = HandleUnexpectedResponse;

            try {
                Status.StartNewJob(string.Format("Starting {0}", DatabaseName.ToLowerInvariant()));
                ShowStatus("assuring server is running", true);
                StartServerCLICommand.Create().ExecuteWithin(this);

                var engine = RunWithinContext();
                Status.CompleteJob(string.Format("started, code host PID: {0}", engine.CodeHostProcess.PID));

            } catch (Exception e) {
                SharedCLI.ShowErrorAndSetExitCode(e, true, false);
                WriteErrorLogsToConsoleAfterRun = true;
                return;
            }
        }

        private Engine RunWithinContext() {
            ErrorDetail errorDetail;
            EngineReference engineRef;
            var node = Node;
            var admin = AdminAPI;
            var uris = admin.Uris;
            var args = CLIArguments;
            var databaseName = DatabaseName.ToLowerInvariant();
            int statusCode;

            ShowStatus("retrieving database status", true);
            var response = node.GET(admin.FormatUri(uris.Engine, databaseName), null);
            statusCode = response.FailIfNotSuccessOr(404);

            if (statusCode == 404) {
                errorDetail = new ErrorDetail();
                try {
                    errorDetail.PopulateFromJson(response.Body);
                } catch {
                    // The content of the response is not ErrorDetail json. It might be some other 
                    // 404 sent from a different source, that sets the content to the original 
                    // exception message. Lets handle it as an unexpected response.
                    HandleUnexpectedResponse(response);
                }

                if (errorDetail.ServerCode == Error.SCERRDATABASENOTFOUND) {
                    var allowed = !args.ContainsFlag(Option.NoAutoCreateDb);
                    if (!allowed) {
                        var notAllowed =
                            ErrorCode.ToMessage(Error.SCERRDATABASENOTFOUND,
                            string.Format("Database: \"{0}\". Remove --{1} to create automatically.", databaseName, Option.NoAutoCreateDb));
                        SharedCLI.ShowErrorAndSetExitCode(notAllowed, true);
                    }

                    CreateDatabaseCLICommand.Create(this.DatabaseName).ExecuteWithin(this);
                }

                ShowStatus("starting database");
                engineRef = new EngineReference();
                engineRef.Name = databaseName;
                engineRef.NoDb = args.ContainsFlag(Option.NoDb);
                engineRef.LogSteps = args.ContainsFlag(Option.LogSteps);

                string codeHostCommands;
                if (args.TryGetProperty(UnofficialOption.CodeHostCommandLineOptions, out codeHostCommands)) {
                    engineRef.CodeHostCommandLineAdditions = codeHostCommands;
                }

                response = node.POST(admin.FormatUri(uris.Engines), engineRef.ToJson(), null);
                response.FailIfNotSuccess();

                response = node.GET(admin.FormatUri(uris.Engine, databaseName), null);
                response.FailIfNotSuccess();
            }

            var engine = new Engine();
            engine.PopulateFromJson(response.Body);

            return engine;
        }
    }
}