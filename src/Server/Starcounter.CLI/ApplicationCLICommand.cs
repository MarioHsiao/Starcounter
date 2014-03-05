
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
    using EngineReference = EngineCollection.EnginesElementJson;
    using ExecutableReference = Engine.ExecutablesJson.ExecutingElementJson;
    using Option = Starcounter.CLI.SharedCLI.Option;
    using UnofficialOption = Starcounter.CLI.SharedCLI.UnofficialOptions;

    /// <summary>
    /// Provides the principal entrypoint to use when a CLI client
    /// want to use the common way to start or stop an application.
    /// </summary>
    public class ApplicationCLICommand {
        AdminAPI AdminAPI;
        string ServerHost;
        int ServerPort;
        string ServerName;
        string DatabaseName;
        ApplicationBase Application;
        ApplicationArguments CLIArguments;
        string[] EntrypointArguments;
        Node Node;
        
        private ApplicationCLICommand() {
        }

        /// <summary>
        /// Creates a new instance of the <see cref="ApplicationCLICommand"/> class based on
        /// the given arguments. This instance can thereafter be executed with
        /// the <see cref="Execute"/> method.
        /// </summary>
        /// <param name="applicationFilePath">The application file.</param>
        /// <param name="exePath">The compiled application file.</param>
        /// <param name="args">Arguments given to the CLI host.</param>
        /// <param name="entrypointArgs">Arguments that are to be passed along
        /// to the application entrypoint, if the given arguments indicate it's
        /// being started/restarted.</param>
        /// <returns>An instance of <see cref="ApplicationCLICommand"/>.</returns>
        public static ApplicationCLICommand Create(
            string applicationFilePath,
            string exePath,
            ApplicationArguments args,
            string[] entrypointArgs = null) {
            var result = new ApplicationCLICommand();
            
            if (string.IsNullOrWhiteSpace(applicationFilePath)) {
                applicationFilePath = exePath;
            }

            string appName;
            string workingDirectory;
            ResolveWorkingDirectory(args, out workingDirectory);
            SharedCLI.ResolveApplication(args, applicationFilePath, out appName);
            var app = new ApplicationBase(appName, applicationFilePath, exePath, workingDirectory, entrypointArgs);

            SharedCLI.ResolveAdminServer(args, out result.ServerHost, out result.ServerPort, out result.ServerName);
            SharedCLI.ResolveDatabase(args, out result.DatabaseName);

            result.Application = app;
            result.AdminAPI = new AdminAPI();
            result.CLIArguments = args;
            result.EntrypointArguments = entrypointArgs;
            
            return result;
        }

        /// <summary>
        /// Executes the logic of the given CLI arguments on the
        /// application bound to the current instance, on the target
        /// database on the target server.
        /// </summary>
        public void Execute() {
            var args = this.CLIArguments;

            Node = new Node(ServerHost, (ushort)ServerPort);
            try {
                if (args.ContainsFlag(Option.Stop)) {
                    Stop();
                } else {
                    Start();
                }
            } finally {
                Node = null;
            }
        }

        void Start() {
            var app = Application;
            try {
                ShowHeadline(
                    string.Format("[Starting \"{0}\" in \"{1}\" on \"{2}\" ({3}:{4})]",
                    app.Name,
                    DatabaseName,
                    ServerName,
                    Node.BaseAddress.Host,
                    Node.BaseAddress.Port));

                if (StarcounterEnvironment.ServerNames.PersonalServer.Equals(ServerName, StringComparison.CurrentCultureIgnoreCase)) {
                    ShowStatus("Retrieving server status", true);
                    if (!ServerServiceProcess.IsOnline()) {
                        ShowStatus("Starting server");
                        ServerServiceProcess.StartInteractiveOnDemand();
                    }
                    ShowStatus("Server is online", true);
                }

                try {
                    Engine engine;
                    Executable exe;
                    DoStart(out engine, out exe);
                    ShowStartResultAndSetExitCode(Node, DatabaseName, engine, exe, CLIArguments);
                } catch (SocketException se) {
                    ShowSocketErrorAndSetExitCode(se, Node.BaseAddress, ServerName);
                    return;
                }

            } catch (Exception e) {
                SharedCLI.ShowErrorAndSetExitCode(e, true, false);
                return;
            }
        }

        void DoStart(out Engine engine, out Executable exe) {
            ErrorDetail errorDetail;
            EngineReference engineRef;
            int statusCode;

            var node = Node;
            var admin = AdminAPI;
            var uris = admin.Uris;
            var databaseName = DatabaseName;
            var args = CLIArguments;
            var app = Application;
            var entrypointArgs = EntrypointArguments;

            ResponseExtensions.OnUnexpectedResponse = HandleUnexpectedResponse;

            // GET or START the engine
            ShowStatus("Retreiving engine status", true);

            var response = node.GET(admin.FormatUri(uris.Engine, databaseName), null);
            statusCode = response.FailIfNotSuccessOr(404);

            if (statusCode == 404) {
                errorDetail = new ErrorDetail();
                errorDetail.PopulateFromJson(response.Body);
                if (errorDetail.ServerCode == Error.SCERRDATABASENOTFOUND) {
                    var allowed = !args.ContainsFlag(Option.NoAutoCreateDb);
                    if (!allowed) {
                        var notAllowed =
                            ErrorCode.ToMessage(Error.SCERRDATABASENOTFOUND,
                            string.Format("Database: \"{0}\". Remove --{1} to create automatically.", databaseName, Option.NoAutoCreateDb));
                        SharedCLI.ShowErrorAndSetExitCode(notAllowed, true);
                    }

                    ShowStatus("Creating database");
                    CreateDatabase(node, uris, databaseName);
                }

                ShowStatus("Starting database");
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

            engine = new Engine();
            engine.PopulateFromJson(response.Body);

            // Restart the engine if the executable is already running, or
            // make sure the host is started if it's not.

            ExecutableReference exeRef = engine.GetExecutable(app.FilePath);
            if (exeRef == null) {
                // If it's not running, we'll check that the code host is
                // running, and start it if not.
                if (engine.CodeHostProcess.PID == 0) {
                    ShowStatus("Starting database");
                    engineRef = new EngineReference();
                    engineRef.Name = databaseName;
                    engineRef.NoDb = args.ContainsFlag(Option.NoDb);
                    engineRef.LogSteps = args.ContainsFlag(Option.LogSteps);

                    response = node.POST(admin.FormatUri(uris.Engines), engineRef.ToJson(), null);
                    response.FailIfNotSuccess();

                    response = node.GET(admin.FormatUri(uris.Engine, databaseName), null);
                    response.FailIfNotSuccess();

                    engine.PopulateFromJson(response.Body);
                }
            } else {
                if (args.ContainsFlag(Option.NoRestart)) {
                    var file = Path.GetFileName(app.FilePath);
                    var alreadyStarted = string.Format("\"{0}\" already running in database \"{1}\"", file, databaseName);
                    SharedCLI.ShowInformationAndSetExitCode(
                        alreadyStarted,
                        Error.SCERREXECUTABLEALREADYRUNNING,
                        string.Format("Omit the --{0} option to restart it.", Option.NoRestart),
                        false,
                        true,
                        ConsoleColor.Green,
                        ConsoleColor.Yellow
                        );
                }

                var fellowCount = engine.Executables.Executing.Count - 1;
                var status = string.Format("Restarting database \"{0}\"", databaseName);
                if (fellowCount > 0) {
                    status += string.Format(" (and {0} other executable(s))", fellowCount);
                }

                ShowStatus(status);
                response = node.DELETE(node.ToLocal(exeRef.Uri), (String)null, null);
                response.FailIfNotSuccessOr(404);

                response = node.GET(admin.FormatUri(uris.Engine, databaseName), null);
                response.FailIfNotSuccess();

                engine.PopulateFromJson(response.Body);
            }

            // Go ahead and run the exe.
            // We could make this final step conditional, only allowing it
            // to succeed on an engine snapshot based on the one we have
            // from above (where we know for sure this executable is not
            // running). But right now, I find no real need to do so.

            string[] userArgs = null;
            if (entrypointArgs != null) {
                userArgs = entrypointArgs;
            } else if (args.CommandParameters != null) {
                int userArgsCount = args.CommandParameters.Count;
                userArgs = new string[userArgsCount];
                args.CommandParameters.CopyTo(0, userArgs, 0, userArgsCount);
            }

            ShowStatus("Starting executable", true);
            exe = new Executable();
            exe.Path = app.BinaryFilePath;
            exe.ApplicationFilePath = app.FilePath;
            exe.Name = app.Name;
            exe.WorkingDirectory = app.WorkingDirectory;
            exe.StartedBy = SharedCLI.ClientContext.UserAndProgram;
            exe.IsTool = !args.ContainsFlag(Option.Async);
            if (userArgs != null) {
                foreach (var arg in userArgs) {
                    exe.Arguments.Add().dummy = arg;
                }
            }

            response = node.POST(node.ToLocal(engine.Executables.Uri), exe.ToJson(), null);
            response.FailIfNotSuccess();
            exe.PopulateFromJson(response.Body);
        }

        void Stop() {
            var app = Application;
            try {
                ShowHeadline(
                    string.Format("[Stopping \"{0}\" in \"{1}\" on \"{2}\" ({3}:{4})]",
                    app.Name,
                    DatabaseName,
                    ServerName,
                    Node.BaseAddress.Host,
                    Node.BaseAddress.Port));

                if (StarcounterEnvironment.ServerNames.PersonalServer.Equals(ServerName, StringComparison.CurrentCultureIgnoreCase)) {
                    ShowStatus("Retrieving server status", true);
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

            ShowStatus("Retreiving engine status", true);

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
                var status = string.Format("Restarting database \"{0}\"", databaseName);
                if (fellowCount > 0) {
                    status += string.Format(" (and {0} other executable(s))", fellowCount);
                }
                ShowStatus(status);
                response = node.DELETE(node.ToLocal(exeRef.Uri), (String)null, null);
                response.FailIfNotSuccessOr();
            }
        }

        static void ResolveWorkingDirectory(ApplicationArguments args, out string workingDirectory) {
            string dir;
            if (!args.TryGetProperty(Option.ResourceDirectory, out dir)) {
                dir = Environment.CurrentDirectory;
            }
            workingDirectory = dir;
            workingDirectory = Path.GetFullPath(workingDirectory);
        }

        static void CreateDatabase(Node node, AdminAPI.ResourceUris uris, string databaseName) {
            var db = new Database();
            db.Name = databaseName;
            var response = node.POST(uris.Databases, db.ToJson(), null);
            response.FailIfNotSuccess();
        }

        static void HandleUnexpectedResponse(Response response) {
            var red = ConsoleColor.Red;
            int exitCode = response.StatusCode;

            Console.WriteLine();
            // Try extracting an error detail from the body, but make
            // sure that if we fail doing so, we just dump out the full
            // content in it's rawest format (dictated by the
            // Response.ToString implementation).
            try {
                var detail = new ErrorDetail();
                detail.PopulateFromJson(response.Body);
                //ConsoleUtil.ToConsoleWithColor(string.Format("  Starcounter error code: {0}", detail.ServerCode), red);
                ConsoleUtil.ToConsoleWithColor(detail.Text, red);
                Console.WriteLine();
                SharedCLI.ShowHints((uint)detail.ServerCode);
                exitCode = (int)detail.ServerCode;
            } catch {
                ConsoleUtil.ToConsoleWithColor("Unexpected response from server - unable to continue.", red);
                ConsoleUtil.ToConsoleWithColor(string.Format("  Response status code: {0}", response.StatusCode), red);
                ConsoleUtil.ToConsoleWithColor("  Response:", red);
                ConsoleUtil.ToConsoleWithColor(response.ToString(), red);
            } finally {
                Environment.Exit(exitCode);
            }
        }

        static void ShowVerbose(string output, ConsoleColor color = ConsoleColor.Yellow) {
            SharedCLI.ShowVerbose(output, color);
        }

        static void ShowHeadline(string headline) {
            ConsoleUtil.ToConsoleWithColor(headline, ConsoleColor.DarkGray);
        }

        static void ShowStatus(string status, bool onlyIfVerbose = false) {
            var show = !onlyIfVerbose || SharedCLI.Verbose;
            if (show) {
                ConsoleUtil.ToConsoleWithColor(string.Format("  - {0}", status), ConsoleColor.DarkGray);
            }
        }

        static void ShowStartResultAndSetExitCode(Node node, string database, Engine engine, Executable exe, ApplicationArguments args) {
            var color = ConsoleColor.Green;
            
            ConsoleUtil.ToConsoleWithColor(
                string.Format("\"{0}\" started in database \"{1}\". Default port is {2} (Executable), {3} (Admin))",
                Path.GetFileName(exe.ApplicationFilePath),
                database,
                exe.DefaultUserPort,
                node.PortNumber), 
                color);

            color = ConsoleColor.DarkGray;
            ShowVerbose(
                string.Format("Running in process {0}, started by \"{1}\"", engine.CodeHostProcess.PID, exe.StartedBy),
                color);
            Environment.ExitCode = 0;
        }

        static void ShowStopResultAndSetExitCode(Node node, string database, Engine engine, string applicationFile, ApplicationArguments args) {
            var color = ConsoleColor.Green;

            ConsoleUtil.ToConsoleWithColor(
                string.Format("Stopped \"{0}\" in database \"{1}\"",
                Path.GetFileName(applicationFile),
                database),
                color);

            Environment.ExitCode = 0;
        }

        static void ShowSocketErrorAndSetExitCode(SocketException ex, Uri serverUri, string serverName) {

            // Map the socket level error code to a correspoding Starcounter
            // error code. Try to be as specific as possible.

            uint scErrorCode;
            switch (ex.SocketErrorCode) {
                case SocketError.ConnectionRefused:
                    scErrorCode = Error.SCERRSERVERNOTRUNNING;
                    break;
                default:
                    scErrorCode = Error.SCERRSERVERNOTAVAILABLE;
                    break;
            }

            try {
                var serverInfo = string.Format("\"{0}\" at {1}:{2}", serverName, serverUri.Host, serverUri.Port);
                var socketError = string.Format("{0}/{1}: {2}", ex.SocketErrorCode, ex.ErrorCode, ex.Message);

                Console.WriteLine();
                ConsoleUtil.ToConsoleWithColor(
                    ErrorCode.ToMessage(scErrorCode, string.Format("(Server: {0})", serverInfo)),
                    ConsoleColor.Red);
                Console.WriteLine();
                ConsoleUtil.ToConsoleWithColor(
                    string.Format("(Socket error: {0})", socketError), ConsoleColor.DarkGray);

            } finally {
                // If any unexpected problem when constructing the error information
                // or writing them to the console, at least always set the error code.
                Environment.ExitCode = (int)scErrorCode;
            }
        }
    }
}
