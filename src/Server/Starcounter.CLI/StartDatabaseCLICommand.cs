
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
            try {
                Status.StartNewJob(string.Format("Starting {0}", DatabaseName.ToLowerInvariant()));

                if (StarcounterEnvironment.ServerNames.PersonalServer.Equals(ServerName, StringComparison.CurrentCultureIgnoreCase)) {
                    ShowStatus("retrieving server status", true);
                    if (!ServerServiceProcess.IsOnline()) {
                        SharedCLI.ShowErrorAndSetExitCode(ErrorCode.ToMessage(Error.SCERRSERVERNOTAVAILABLE), true);
                    }
                }

                ErrorDetail errorDetail;
                EngineReference engineRef;
                var node = Node;
                var admin = AdminAPI;
                var uris = admin.Uris;
                var args = CLIArguments;
                var databaseName = DatabaseName.ToLowerInvariant();
                int statusCode;

                ResponseExtensions.OnUnexpectedResponse = HandleUnexpectedResponse;

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

                    // Consider changing this when its done in isolation. I think
                    // the experience should be the opposite - you specify if you
                    // want it implicitly created.
                    if (errorDetail.ServerCode == Error.SCERRDATABASENOTFOUND) {
                        var allowed = !args.ContainsFlag(Option.NoAutoCreateDb);
                        if (!allowed) {
                            var notAllowed =
                                ErrorCode.ToMessage(Error.SCERRDATABASENOTFOUND,
                                string.Format("Database: \"{0}\". Remove --{1} to create automatically.", databaseName, Option.NoAutoCreateDb));
                            SharedCLI.ShowErrorAndSetExitCode(notAllowed, true);
                        }

                        ShowStatus("creating database");
                        
                        // TODO:
                        // CreateDatabase(node, uris, databaseName);
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

                Status.CompleteJob(string.Format("started, code host PID: {0}", engine.CodeHostProcess.PID));

            } catch (Exception e) {
                SharedCLI.ShowErrorAndSetExitCode(e, true, false);
                WriteErrorLogsToConsoleAfterRun = true;
                return;
            }
        }
    }
}