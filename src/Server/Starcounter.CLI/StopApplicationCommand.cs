using Starcounter.Advanced;
using Starcounter.CommandLine;
using Starcounter.Internal;
using Starcounter.Hosting;
using Starcounter.Rest.ExtensionMethods;
using Starcounter.Server;
using Starcounter.Server.Rest;
using Starcounter.Server.Rest.Representations.JSON;
using System;
using System.IO;
using System.Net.Sockets;

namespace Starcounter.CLI {
    using EngineReference = Starcounter.Server.Rest.Representations.JSON.EngineCollection.EnginesElementJson;
    using ExecutableReference = Starcounter.Server.Rest.Representations.JSON.Engine.ExecutablesJson.ExecutingElementJson;
    using Option = Starcounter.CLI.SharedCLI.Option;
    using UnofficialOption = Starcounter.CLI.SharedCLI.UnofficialOptions;

    internal class StopApplicationCommand : ApplicationCLICommand {

        /// <summary>
        /// Initialize a new <see cref="StopApplicationCommand"/>.
        /// </summary>
        /// <param name="application">The application being targetted.</param>
        internal StopApplicationCommand(ApplicationBase application)
            : base(application) {
        }

        protected override void Run() {
            var app = Application;
            try {
                Status.StartNewJob(string.Format("{0} <- {1}", app.Name, DatabaseName.ToLowerInvariant()));
                ShowHeadline(
                    string.Format("[Stopping \"{0}\" in \"{1}\" on \"{2}\" ({3}:{4})]",
                    app.Name,
                    DatabaseName,
                    ServerName,
                    Node.BaseAddress.Host,
                    Node.BaseAddress.Port));

                if (StarcounterEnvironment.ServerNames.PersonalServer.Equals(ServerName, StringComparison.CurrentCultureIgnoreCase)) {
                    ShowStatus("retrieving server status", true);
                    if (!ServerServiceProcess.IsOnline()) {
                        SharedCLI.ShowErrorAndSetExitCode(ErrorCode.ToMessage(Error.SCERRSERVERNOTAVAILABLE), true);
                    }
                }

                try {
                    Engine engine;
                    DoStop(out engine);
                    ShowStopResultAndSetExitCode(Node, DatabaseName, engine, app.FilePath, CLIArguments);
                } catch (SocketException se) {
                    ShowSocketErrorAndSetExitCode(se, Node.BaseAddress, ServerName);
                    return;
                }

            } catch (Exception e) {
                SharedCLI.ShowErrorAndSetExitCode(e, true, false);
                return;
            }
        }

        void DoStop(out Engine engine) {
            ErrorDetail errorDetail;
            int statusCode;

            var admin = AdminAPI;
            var uris = admin.Uris;
            var node = Node;
            var databaseName = DatabaseName;
            var app = Application;

            ResponseExtensions.OnUnexpectedResponse = HandleUnexpectedResponse;

            ShowStatus("retreiving engine status", true);

            var response = node.GET(admin.FormatUri(uris.Engine, databaseName), null);
            statusCode = response.FailIfNotSuccessOr(404);

            if (statusCode == 404) {
                errorDetail = new ErrorDetail();
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

            engine = new Engine();
            engine.PopulateFromJson(response.Body);

            ExecutableReference exeRef = engine.GetExecutable(app.FilePath);
            if (exeRef == null) {
                var notRunning = ErrorCode.ToMessage(Error.SCERREXECUTABLENOTRUNNING, string.Format("Database: \"{0}\".", databaseName));
                SharedCLI.ShowErrorAndSetExitCode(notRunning, true);
            } else {
                var fellowCount = engine.Executables.Executing.Count - 1;
                var status = string.Format("restarting {0}", databaseName);
                if (fellowCount > 0) {
                    status += string.Format(" (and {0} other executable(s))", fellowCount);
                }
                ShowStatus(status);
                response = node.DELETE(node.ToLocal(exeRef.Uri), (String)null, null);
                response.FailIfNotSuccessOr();
            }
        }

        void ShowStopResultAndSetExitCode(Node node, string database, Engine engine, string applicationFile, ApplicationArguments args) {
            var color = ConsoleColor.Green;

            Status.CompleteJob("stopped");
            if (SharedCLI.Verbosity > OutputLevel.Minimal) {
                ConsoleUtil.ToConsoleWithColor(
                    string.Format("Stopped \"{0}\" in {1}",
                    Path.GetFileName(applicationFile),
                    database),
                    color);
            }

            Environment.ExitCode = 0;
        }
    }
}
