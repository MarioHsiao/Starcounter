
using Starcounter.Advanced;
using Starcounter.CommandLine;
using Starcounter.Internal;
using Starcounter.Rest.ExtensionMethods;
using Starcounter.Server.Rest;
using Starcounter.Server.Rest.Representations.JSON;
using System;
using System.IO;
using System.Net.Sockets;

namespace Starcounter.CLI {
    using EngineReference = EngineCollection.EnginesObj;
    using ExecutableReference = Engine.ExecutablesObj.ExecutingObj;
    using Option = Starcounter.CLI.SharedCLI.Option;

    /// <summary>
    /// Provides the principal entrypoint to use when a CLI client
    /// want to use the common way to start an executable.
    /// </summary>
    public static class ExeCLI {
        /// <summary>
        /// Runs the given executable using a set of optional arguments
        /// and executable parameters.
        /// </summary>
        /// <param name="exePath">Full path to the executable.</param>
        /// <param name="args">Parsed arguments to use to customize the
        /// settings under which the exeuctable will run and possibly
        /// parameters to be sent to the entrypoint.</param>
        /// <param name="entrypointArgs">Contains the arguments to be
        /// passed to the entrypoint. If not specified explicitly, the
        /// shared CLI will use the parameters from the supplied
        /// <paramref name="args"/>.</param>
        /// <param name="admin">The admin API to target, mainly defining
        /// the resource URIs to use.</param>
        public static void Exec(string exePath, ApplicationArguments args, string[] entrypointArgs = null, AdminAPI admin = null) {
            int serverPort;
            string serverName;
            string serverHost;
            string database;
            ShowVerbose(string.Format("Executing {0}", exePath));

            if (admin == null) {
                admin = new AdminAPI();
            }

            try {
                SharedCLI.ResolveAdminServer(args, out serverHost, out serverPort, out serverName);
                SharedCLI.ResolveDatabase(args, out database);

                if (StarcounterEnvironment.ServerNames.PersonalServer.Equals(serverName, StringComparison.CurrentCultureIgnoreCase)) {
                    ShowHeadline("[Checking personal server]");
                    ShowStatus("Retrieving server status");
                    if (!PersonalServerProcess.IsOnline()) {
                        ShowStatus("Starting server");
                        PersonalServerProcess.Start();
                    }
                    ShowStatus("Server is online");
                }

                var node = new Node(serverHost, (ushort)serverPort);

                ShowHeadline(
                    string.Format("[Starting \"{0}\" in \"{1}\" on \"{2}\" ({3}:{4})]",
                    Path.GetFileName(exePath),
                    database,
                    serverName,
                    node.BaseAddress.Host,
                    node.BaseAddress.Port));

                try {
                    Engine engine;
                    Executable exe;
                    DoExec(node, admin, exePath, database, args, entrypointArgs, out engine, out exe);
                    ShowResultAndSetExitCode(node, engine, exe, args);
                } catch (SocketException se) {
                    ShowSocketErrorAndSetExitCode(se, node.BaseAddress, serverName);
                    return;
                }

            } catch (Exception e) {
                SharedCLI.ShowErrorAndSetExitCode(e, true, false);
                return;
            }
        }

        static void DoExec(
            Node node, AdminAPI admin, string exePath, string databaseName, ApplicationArguments args, string[] entrypointArgs, out Engine engine, out Executable exe) {
            ErrorDetail errorDetail;
            EngineReference engineRef;
            int statusCode;
            var uris = admin.Uris;

            ResponseExtensions.OnUnexpectedResponse = HandleUnexpectedResponse;

            // GET or START the engine
            ShowStatus("Retreiving engine status");

            var response = node.GET(admin.FormatUri(uris.Engine, databaseName), null, null);
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

                ShowStatus("Starting engine");
                engineRef = new EngineReference();
                engineRef.Name = databaseName;
                engineRef.NoDb = args.ContainsFlag(Option.NoDb);
                engineRef.LogSteps = args.ContainsFlag(Option.LogSteps);

                response = node.POST(admin.FormatUri(uris.Engines), engineRef.ToJson(), null, null);
                response.FailIfNotSuccess();

                response = node.GET(admin.FormatUri(uris.Engine, databaseName), null, null);
                response.FailIfNotSuccess();
            }

            engine = new Engine();
            engine.PopulateFromJson(response.Body);
            
            // Restart the engine if the executable is already running, or
            // make sure the host is started if it's not.

            ExecutableReference exeRef = engine.GetExecutable(exePath);
            if (exeRef == null) {
                // If it's not running, we'll check that the host host is
                // running, and start it if not.
                if (engine.CodeHostProcess.PID == 0) {
                    ShowStatus("Starting host");
                    engineRef = new EngineReference();
                    engineRef.Name = databaseName;
                    engineRef.NoDb = args.ContainsFlag(Option.NoDb);
                    engineRef.LogSteps = args.ContainsFlag(Option.LogSteps);

                    response = node.POST(admin.FormatUri(uris.Engines), engineRef.ToJson(), null, null);
                    response.FailIfNotSuccess();

                    response = node.GET(admin.FormatUri(uris.Engine, databaseName), null, null);
                    response.FailIfNotSuccess();

                    engine.PopulateFromJson(response.Body);
                }
            }
            else {
                ShowStatus("Stopping host");
                response = node.DELETE(node.ToLocal(engine.CodeHostProcess.Uri), (String)null, null, null);
                response.FailIfNotSuccessOr(404);

                ShowStatus("Starting host");
                engineRef = new EngineReference();
                engineRef.Name = databaseName;
                engineRef.NoDb = args.ContainsFlag(Option.NoDb);
                engineRef.LogSteps = args.ContainsFlag(Option.LogSteps);

                response = node.POST(admin.FormatUri(uris.Engines), engineRef.ToJson(), null, null);
                response.FailIfNotSuccess();

                response = node.GET(admin.FormatUri(uris.Engine, databaseName), null, null);
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
            }
            else if (args.CommandParameters != null) {
                int userArgsCount = args.CommandParameters.Count;
                userArgs = new string[userArgsCount];
                args.CommandParameters.CopyTo(0, userArgs, 0, userArgsCount);
            }

            ShowStatus("Starting executable");
            exe = new Executable();
            exe.Path = exePath;
            exe.StartedBy = SharedCLI.ClientContext.UserAndProgram;
            exe.IsTool = args.ContainsFlag(Option.WaitForEntrypoint);
            if (userArgs != null) {
                foreach (var arg in userArgs) {
                    exe.Arguments.Add().dummy = arg;
                }
            }

            response = node.POST(node.ToLocal(engine.Executables.Uri), exe.ToJson(), null, null);
            response.FailIfNotSuccess();
            exe.PopulateFromJson(response.Body);
        }

        static void CreateDatabase(Node node, AdminAPI.ResourceUris uris, string databaseName) {
            var db = new Database();
            db.Name = databaseName;
            var response = node.POST(uris.Databases, db.ToJson(), null, null);
            response.FailIfNotSuccess();
        }

        static void HandleUnexpectedResponse(Response response) {
            var red = ConsoleColor.Red;
            int exitCode = response.StatusCode;

            Console.WriteLine();
            ConsoleUtil.ToConsoleWithColor("Unexpected response from server - unable to continue.", red);
            ConsoleUtil.ToConsoleWithColor(string.Format("  Response status code: {0}", response.StatusCode), red);

            // Try extracting an error detail from the body, but make
            // sure that if we fail doing so, we just dump out the full
            // content in it's rawest format (dictated by the
            // Response.ToString implementation).
            try {
                var detail = new ErrorDetail();
                detail.PopulateFromJson(response.Body);
                ConsoleUtil.ToConsoleWithColor(string.Format("  Starcounter error code: {0}", detail.ServerCode), red);
                ConsoleUtil.ToConsoleWithColor(string.Format("  Error message: {0}", detail.Text), red);
                ConsoleUtil.ToConsoleWithColor(string.Format("  Help link: {0}", detail.Helplink), red);
                exitCode = (int)detail.ServerCode;
            } catch {
                ConsoleUtil.ToConsoleWithColor("  Response:", red);
                ConsoleUtil.ToConsoleWithColor(response.ToString(), red);
            } finally {
                Environment.Exit(exitCode);
            }
        }

        static void ShowVerbose(string output) {
            if (SharedCLI.Verbose) {
                ConsoleUtil.ToConsoleWithColor(output, ConsoleColor.Yellow);
            }
        }

        static void ShowHeadline(string headline) {
            ConsoleUtil.ToConsoleWithColor(headline, ConsoleColor.DarkGray);
        }

        static void ShowStatus(string status) {
            ConsoleUtil.ToConsoleWithColor(string.Format("  - {0}", status), ConsoleColor.DarkGray);
        }

        static void ShowResultAndSetExitCode(Node node, Engine engine, Executable exe, ApplicationArguments args) {
            var color = ConsoleColor.Green;
            
            ConsoleUtil.ToConsoleWithColor(
                string.Format("Successfully started \"{0}\" (engine PID:{1}, default port is {2} (Executable), {3} (Admin))", 
                             Path.GetFileName(exe.Path), 
                             engine.CodeHostProcess.PID, 
                             exe.DefaultUserPort, 
                             node.PortNumber), 
                color);
            color = ConsoleColor.DarkGray;
            ConsoleUtil.ToConsoleWithColor(string.Format("Started by \"{0}\"", exe.StartedBy), color);
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
