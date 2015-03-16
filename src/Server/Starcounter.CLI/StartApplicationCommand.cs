﻿using Starcounter.Advanced;
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
    using System.Threading;

    /// <summary>
    /// Implements a <see cref="ApplicationCLICommand"/> that support
    /// starting an application.
    /// </summary>
    public class StartApplicationCommand : ApplicationCLICommand {
        readonly internal ApplicationBase Application;
        internal string[] EntrypointArguments;

        /// <summary>
        /// Gets or sets the string normally displayed in the CLI
        /// when the command starts executing its primary job.
        /// </summary>
        public string JobDescription { get; set; }

        /// <summary>
        /// Gets or sets the string normally displayed in the CLI
        /// when the command completes.
        /// </summary>
        public string JobCompletionDescription { get; set; }

        /// <summary>
        /// Gets or sets the string normally displayed in the CLI
        /// when everything has been setup and the actual application
        /// is about to start.
        /// </summary>
        public string ApplicationStartingDescription { get; set; }

        /// <summary>
        /// Initialize a new <see cref="StartApplicationCommand"/>.
        /// </summary>
        /// <param name="application">The application being targetted.</param>
        internal StartApplicationCommand(ApplicationBase application)
            : base(application.Name) {    
            Application = application;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="StartApplicationCommand"/>
        /// class based on the given arguments. This instance can thereafter be
        /// executed with the <see cref="CLIClientCommand.Execute"/> method.
        /// </summary>
        /// <param name="applicationFilePath">The application file.</param>
        /// <param name="exePath">The compiled application file.</param>
        /// <param name="args">Arguments given to the CLI host.</param>
        /// <param name="entrypointArgs">Arguments that are to be passed along
        /// to the application entrypoint.</param>
        /// <returns>An instance of <see cref="StartApplicationCommand"/>.</returns>
        public static StartApplicationCommand FromFile(
            string applicationFilePath,
            string exePath,
            ApplicationArguments args,
            string[] entrypointArgs = null) {
            if (string.IsNullOrWhiteSpace(applicationFilePath)) {
                applicationFilePath = exePath;
            }

            string appName;
            string workingDirectory;
            string databaseName;
            ResolveWorkingDirectory(args, out workingDirectory);
            SharedCLI.ResolveApplication(args, applicationFilePath, out appName);
            var app = new ApplicationBase(appName, applicationFilePath, exePath, workingDirectory, entrypointArgs);

            SharedCLI.ResolveDatabase(args, out databaseName);

            var command = new StartApplicationCommand(app) {
                DatabaseName = databaseName,
                AdminAPI = new AdminAPI(),
                CLIArguments = args,
                EntrypointArguments = entrypointArgs
            };
            command.ResolveServer(args);

            command.Initialize();

            return command;
        }

        static void ResolveWorkingDirectory(ApplicationArguments args, out string workingDirectory) {
            string dir;
            if (!args.TryGetProperty(Option.ResourceDirectory, out dir)) {
                dir = Environment.CurrentDirectory;
            }
            workingDirectory = dir;
            workingDirectory = Path.GetFullPath(workingDirectory);
        }

        void Initialize() {
            JobDescription = string.Format("{0} -> {1}", Application.Name, DatabaseName.ToLowerInvariant());
            JobCompletionDescription = null;
            ApplicationStartingDescription = "starting application";
        }

        /// <summary>
        /// Runs the current command.
        /// </summary>
        /// <seealso cref="CLIClientCommand.Run"/>
        protected override void Run() {
            var app = Application;
            try {
                Status.StartNewJob(JobDescription);
                ShowHeadline(
                    string.Format("[Starting \"{0}\" in \"{1}\" on \"{2}\" ({3}:{4})]",
                    app.Name,
                    DatabaseName,
                    ServerName,
                    Node.BaseAddress.Host,
                    Node.BaseAddress.Port));

                if (StarcounterEnvironment.ServerNames.PersonalServer.Equals(ServerName, StringComparison.CurrentCultureIgnoreCase)) {
                    ShowStatus("retrieving server status", true);
                    if (!ServerServiceProcess.IsOnline()) {
                        ShowStatus("starting server");
                        ServerServiceProcess.StartInteractiveOnDemand();
                    }
                    ShowStatus("server is online", true);
                }

                try {
                    Engine engine;
                    Executable exe;
                    DoStart(out engine, out exe);
                    ShowStartResultAndSetExitCode(Node, DatabaseName, engine, exe, CLIArguments);
                } catch (SocketException se) {
                    ShowSocketErrorAndSetExitCode(se, Node.BaseAddress, ServerName);
                    WriteErrorLogsToConsoleAfterRun = true;
                    return;
                }

            } catch (Exception e) {
                SharedCLI.ShowErrorAndSetExitCode(e, true, false);
                WriteErrorLogsToConsoleAfterRun = true;
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
            var databaseName = DatabaseName.ToLowerInvariant();
            var args = CLIArguments;
            var app = Application;
            var entrypointArgs = EntrypointArguments;

            ResponseExtensions.OnUnexpectedResponse = HandleUnexpectedResponse;

            // GET or START the engine
            ShowStatus("retreiving engine status", true);

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

                    ShowStatus("creating database");
                    CreateDatabase(node, uris, databaseName);
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

            engine = new Engine();
            engine.PopulateFromJson(response.Body);

            // Restart the engine if the executable is already running, or
            // make sure the host is started if it's not.

            ExecutableReference exeRef = engine.GetExecutable(app.FilePath);
            if (exeRef == null) {
                // If it's not running, we'll check that the code host is
                // running, and start it if not.
                if (engine.CodeHostProcess.PID == 0) {
                    ShowStatus("starting database");
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
                var status = string.Format("restarting {0}", databaseName);
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

            ShowStatus(ApplicationStartingDescription);
            exe = new Executable();
            exe.Path = app.BinaryFilePath;
            exe.ApplicationFilePath = app.FilePath;
            exe.Name = app.Name;
            exe.WorkingDirectory = app.WorkingDirectory;
            exe.StartedBy = ClientContext.GetCurrentContextInfo();
            exe.IsTool = !args.ContainsFlag(Option.Async);
            if (userArgs != null) {
                foreach (var arg in userArgs) {
                    exe.Arguments.Add().dummy = arg;
                }
            }

            var responded = new ManualResetEvent(false);
            node.POST(node.ToLocal(engine.Executables.Uri), exe.ToJson(), null, null, (resp, ignored) => {
                response = resp;
                responded.Set();
            });
            AwaitExecutableStartup(databaseName, exe.Name, responded);
            
            response.FailIfNotSuccess();
            exe.PopulateFromJson(response.Body);
        }
        
        void ShowStartResultAndSetExitCode(Node node, string database, Engine engine, Executable exe, ApplicationArguments args) {
            var color = ConsoleColor.Green;
            var description = JobCompletionDescription ?? 
                string.Format("started, default port {0}, admin {1}", exe.DefaultUserPort, node.PortNumber);

            Status.CompleteJob(description);
            if (SharedCLI.Verbosity > OutputLevel.Minimal) {
                ConsoleUtil.ToConsoleWithColor(
                    string.Format("\"{0}\" started in {1}. Default port is {2} (Application), {3} (Admin))",
                    Path.GetFileName(exe.ApplicationFilePath),
                    database,
                    exe.DefaultUserPort,
                    node.PortNumber),
                    color);
            }

            color = ConsoleColor.DarkGray;
            ShowVerbose(
                string.Format("Running in process {0}, started by \"{1}\"", engine.CodeHostProcess.PID, exe.StartedBy),
                color);
            Environment.ExitCode = 0;
        }

        void AwaitExecutableStartup(string databaseName, string appName, ManualResetEvent started) {
            var c = new CodeHostConsole(databaseName, DateTime.Now, appName);
            c.MessageWritten = (a, b) => {
                Console.Write(b);
            };
            
            c.Open();
            started.WaitOne();
            c.Close();
        }

        void CreateDatabase(Node node, AdminAPI.ResourceUris uris, string databaseName) {
            var db = new Database();
            db.Name = databaseName;
            var response = node.POST(uris.Databases, db.ToJson(), null);
            response.FailIfNotSuccess();
        }
    }
}
