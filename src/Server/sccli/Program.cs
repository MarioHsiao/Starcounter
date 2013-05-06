
using Starcounter;
using Starcounter.CommandLine;
using Starcounter.CommandLine.Syntax;
using Starcounter.Internal;
using Starcounter.Server;
using Starcounter.Server.Rest;
using Starcounter.Server.Rest.Representations.JSON;
using Starcounter.Server.Setup;
using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;

namespace star {
    using Starcounter.Advanced;
    using EngineReference = EngineCollection.EnginesApp;
    using ExecutableReference = Engine.ExecutablesApp.ExecutingApp;
    using Option = SharedCLI.Option;

    class Program {
        const string ProgramName = "star.exe";
        static string ProgramNameAndContext {
            get {
                try { return string.Format("{0}@{1} (via {2})", Environment.UserName.ToLowerInvariant(), Environment.MachineName.ToLowerInvariant(), ProgramName);
                } catch {
                    return Program.ProgramName;
                }
            }
        }
       
        static void Main(string[] args) {
            ApplicationArguments appArgs;
            int serverPort;
            string serverName;
            string serverHost;
            string database;

            if (args.Length == 0) {
                Usage(null);
                return;
            }

            var syntax = DefineCommandLineSyntax();
            var parser = new Parser(args);
            try {
                appArgs = parser.Parse(syntax);
            } catch (InvalidCommandLineException e) {
                ConsoleUtil.ToConsoleWithColor(e.Message, ConsoleColor.Red);
                Environment.ExitCode = (int)e.ErrorCode;
                return;
            }

            // Process global options that has precedence

            if (appArgs.ContainsFlag(SharedCLI.UnofficialOptions.Debug)) {
                System.Diagnostics.Debugger.Launch();
            }

            if (appArgs.ContainsFlag(StarOption.NoColor)) {
                ConsoleUtil.DisableColors = true;
            }

            var syntaxTests = appArgs.ContainsFlag(StarOption.Syntax);
            if (syntaxTests) {
                ConsoleUtil.ToConsoleWithColor(() => { SyntaxTreeToConsole(syntax); }, ConsoleColor.DarkGray);
                ConsoleUtil.ToConsoleWithColor(() => { ParsedArgumentsToConsole(appArgs, syntax); }, ConsoleColor.Green);
                // Include how we resolve the admin server port / server, if applicable.
                // By design, silently ignore any error.
                try {
                    SharedCLI.ResolveAdminServer(appArgs, out serverHost, out serverPort, out serverName);
                    ConsoleUtil.ToConsoleWithColor(string.Format("Server \"{0}\" on @ {1}:{2}.", serverName, serverHost, serverPort), ConsoleColor.Yellow);
                } catch { }

                // Exiting, because we were asked to test syntax only.
                return;
            }

            if (appArgs.ContainsFlag(StarOption.Help, CommandLineSection.GlobalOptions)) {
                Usage(syntax);
                return;
            }

            if (appArgs.ContainsFlag(StarOption.HelpEx, CommandLineSection.GlobalOptions)) {
                Usage(syntax, true);
                return;
            }

            if (appArgs.ContainsFlag(StarOption.Info, CommandLineSection.GlobalOptions)) {
                ShowInfoAboutStarcounter();
                return;
            }

            if (appArgs.ContainsFlag(StarOption.Version, CommandLineSection.GlobalOptions)) {
                ShowVersionInfo();
                return;
            }

            if (appArgs.CommandParameters.Count == 0) {
                ConsoleUtil.ToConsoleWithColor("No file specified. Aborting.", ConsoleColor.Yellow);
                return;
            }

            // We are told to execute. We got at least one parameter. Check if it's
            // the temporary @@CreateRepo.
            if (appArgs.CommandParameters[0].Equals("@@CreateRepo", StringComparison.InvariantCultureIgnoreCase)) {
                CreateServerRepository(appArgs);
                return;
            }

            // We got an executable to host.
            // Get the only required parameter - the executable - and send it
            // to the server.
            // We utilize a strategy where we do minimal client side validation,
            // because everything we validate here (on the client) actually
            // might not hold true on the server. For example, what if the
            // server employs some cool fallback for sending a file that doesn't
            // exist? We shouldnt take away the ability for the server to do so
            // by validating if the file exist here.
            // So bottomline: a client with "full" transparency.
            
            try {
                SharedCLI.ResolveAdminServer(appArgs, out serverHost, out serverPort, out serverName);
                SharedCLI.ResolveDatabase(appArgs, out database);
                
                // Aware of the client transparency guideline stated previously,
                // we still do resolve the path of the given executable based on
                // the location of the client. It's most likely what the user
                // intended.
                // On top of that, we check if we can find the file once resolved
                // and if we can't, we do a try on the given name + the .exe file
                // extension. If we find such file, we assume the user meant that
                // to be the file.
                //   After this resolving has taken place, if we still can't find
                // the file, the correct thing to do is to pass it to the server,
                // at least in theory. We have no way of knowing how the server do
                // handle a file that does not exist. Maybe it can create something
                // on the fly, or using some default?
                //  However, in practice, it's probably a decent thing to fail
                // upfront, right here and now, to offload the server and dont have
                // a lot of processing being done when the file is missing. So that
                // is what we do. If we find we must be more strict to theory later
                // on, we should implement a swich that allows this to be turned of.
                var exePath = appArgs.CommandParameters[0];
                exePath = Path.GetFullPath(exePath);
                if (!File.Exists(exePath)) {
                    var executableEx = appArgs.CommandParameters[0] + ".exe";
                    executableEx = Path.GetFullPath(executableEx);
                    if (File.Exists(executableEx)) {
                        exePath = executableEx;
                    }
                }
                if (!File.Exists(exePath)) {
                    ShowErrorAndSetExitCode(
                        ErrorCode.ToMessage(Error.SCERREXECUTABLENOTFOUND, string.Format("File: {0}", exePath)), true);
                }

                RequestHandler.InitREST();
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
                    Exec(node, new AdminAPI(), exePath, database, appArgs, out engine, out exe);
                    ShowResultAndSetExitCode(engine, exe, appArgs);
                } catch (SocketException se) {
                    ShowSocketErrorAndSetExitCode(se, node.BaseAddress, serverName);
                    return;
                }
                
            } catch (Exception e) {
                ShowErrorAndSetExitCode(e, true, false);
                return;
            }
        }

        static void Exec(
            Node node, AdminAPI admin, string exePath, string databaseName, ApplicationArguments args, out Engine engine, out Executable exe) {
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
                errorDetail.PopulateFromJson(response.GetBodyStringUtf8_Slow());
                if (errorDetail.ServerCode == Error.SCERRDATABASENOTFOUND) {
                    var allowed = !args.ContainsFlag(Option.NoAutoCreateDb);
                    if (!allowed) {
                        var notAllowed =
                            ErrorCode.ToMessage(Error.SCERRDATABASENOTFOUND, 
                            string.Format("Database: \"{0}\". Remove --{1} to create automatically.", databaseName, Option.NoAutoCreateDb));
                        ShowErrorAndSetExitCode(notAllowed, true);
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
            }
            engine = new Engine();
            engine.PopulateFromJson(response.GetBodyStringUtf8_Slow());
            var engineETag = response["ETag"];

            // Restart the engine if the executable is already running

            ExecutableReference exeRef = engine.GetExecutable(exePath);
            if (exeRef != null) {
                ShowStatus("Stopping engine");
                response = node.DELETE(node.ToLocal(engine.CodeHostProcess.Uri), null, null, null);
                response.FailIfNotSuccessOr(404);

                ShowStatus("Starting engine");
                engineRef = new EngineReference();
                engineRef.Name = databaseName;
                engineRef.NoDb = args.ContainsFlag(Option.NoDb);
                engineRef.LogSteps = args.ContainsFlag(Option.LogSteps);

                response = node.POST(admin.FormatUri(uris.Engines), engineRef.ToJson(), null, null);
                response.FailIfNotSuccess();
                engine.PopulateFromJson(response.GetBodyStringUtf8_Slow());
            }

            // Go ahead and run the exe.
            // We could make this final step conditional, only allowing it
            // to succeed on an engine snapshot based on the one we have
            // from above (where we know for sure this executable is not
            // running). But right now, I find no real need to do so.

            string[] userArgs = null;
            if (args.CommandParameters != null) {
                int userArgsCount = args.CommandParameters.Count;

                // Check if we have more arguments than one. Remember that we
                // reserve the first argument as the name/path of the executable
                // and that we are really hiding a general "Exec exe [others]"
                // scenario.

                if (userArgsCount > 1) {
                    userArgsCount--;
                    userArgs = new string[userArgsCount];
                    args.CommandParameters.CopyTo(1, userArgs, 0, userArgsCount);
                }
            }

            ShowStatus("Starting executable");
            exe = new Executable();
            exe.Path = exePath;
            exe.StartedBy = ProgramNameAndContext;
            if (userArgs != null) {
                foreach (var arg in userArgs) {
                    exe.Arguments.Add().dummy = arg;
                }
            }

            response = node.POST(node.ToLocal(engine.Executables.Uri), exe.ToJson(), null, null);
            response.FailIfNotSuccess();
            exe.PopulateFromJson(response.GetBodyStringUtf8_Slow());
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
                detail.PopulateFromJson(response.GetBodyStringUtf8_Slow());
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

        static void ShowHeadline(string headline) {
            ConsoleUtil.ToConsoleWithColor(headline, ConsoleColor.DarkGray);
        }

        static void ShowStatus(string status) {
            ConsoleUtil.ToConsoleWithColor(string.Format("  - {0}", status), ConsoleColor.DarkGray);
        }

        static void ShowErrorAndSetExitCode(ErrorMessage msg, bool exit = false) {
            ConsoleColor red = ConsoleColor.Red;
            int exitCode = (int)msg.Code;
            Console.WriteLine();
            ConsoleUtil.ToConsoleWithColor(msg.ToString(), red);
            if (exit) Environment.Exit(exitCode);
            else Environment.ExitCode = exitCode;
        }

        static void ShowErrorAndSetExitCode(Exception e, bool showStackTrace = true, bool exit = false) {
            ErrorMessage msg;
            bool result;
            uint errorCode;

            result = ErrorCode.TryGetCodedMessage(e, out msg);
            if (result) {
                ShowErrorAndSetExitCode(msg, false);
                errorCode = msg.Code;
            } else {
                if (!ErrorCode.TryGetCode(e, out errorCode)) {
                    errorCode = Error.SCERRUNSPECIFIED;
                }
                Console.WriteLine();
                ConsoleUtil.ToConsoleWithColor(e.Message, ConsoleColor.Red);
                Environment.ExitCode = (int)errorCode;
            }

            if (showStackTrace) {
                var stackTraceColor = ConsoleColor.DarkGray;
                Console.WriteLine();
                ConsoleUtil.ToConsoleWithColor("Stack trace:", stackTraceColor);
                ConsoleUtil.ToConsoleWithColor(e.StackTrace, stackTraceColor);
            }
            
            if (exit) Environment.Exit((int)errorCode);
        }

        static void ShowResultAndSetExitCode(Engine engine, Executable exe, ApplicationArguments args) {
            var color = ConsoleColor.Green;
            ConsoleUtil.ToConsoleWithColor(
                string.Format("Successfully started \"{0}\" (engine PID:{1})", Path.GetFileName(exe.Path), engine.CodeHostProcess.PID), color);
            color = ConsoleColor.DarkGray;
            ConsoleUtil.ToConsoleWithColor(string.Format("Started by \"{0}\"", exe.StartedBy), color);
            //if (args.ContainsFlag(StarOption.AttatchCodeHostDebugger)) {
            //    Process.Start("vsjitdebugger.exe", "-p " + responseBody.DatabaseHostPID);
            //    Console.ReadLine();
            //}
            Environment.ExitCode = 0;
        }

        static void ShowSocketErrorAndSetExitCode(SocketException ex, Uri serverUri, string serverName) {
            
            // Map the socket level error code to a correspoding Starcounter
            // error code. Try to be as specific as possible.

            uint scErrorCode;
            switch(ex.SocketErrorCode) {
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

        static void ShowVersionInfo() {
            Console.WriteLine("Version=", CurrentVersion.Version);
        }

        static void ShowInfoAboutStarcounter() {
            Console.WriteLine("Installation directory={0}", Environment.GetEnvironmentVariable(StarcounterEnvironment.VariableNames.InstallationDirectory));
            Console.WriteLine("Default server={0}", StarcounterEnvironment.ServerNames.PersonalServer.ToLower());
            Console.WriteLine("Version=", CurrentVersion.Version);
        }

        static void Usage(IApplicationSyntax syntax, bool extended = false, bool unofficial = false) {
            string formatting;
            Console.WriteLine("Usage: star [options] executable [parameters]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            formatting = "  {0,-22}{1,25}";
            Console.WriteLine(formatting, string.Format("-h, --{0}", StarOption.Help), "Shows help about star.exe.");
            Console.WriteLine(formatting, string.Format("-hx, --{0}", StarOption.HelpEx), "Shows extended/unofficial help about star.exe.");
            Console.WriteLine(formatting, string.Format("-v, --{0}", StarOption.Version), "Prints the version of Starcounter.");
            Console.WriteLine(formatting, string.Format("-i, --{0}", StarOption.Info), "Prints information about the Starcounter installation.");
            Console.WriteLine(formatting, string.Format("-p, --{0} port", StarOption.Serverport), "The port to use to the admin server.");
            Console.WriteLine(formatting, string.Format("--{0} name", StarOption.Server), "Specifies the name of the server. If no port is");
            Console.WriteLine(formatting, "", "specified, star.exe use the known port of server.");
            Console.WriteLine(formatting, string.Format("--{0} host", StarOption.ServerHost), "Specifies the identity of the server host. If no");
            Console.WriteLine(formatting, "", "host is specified, 'localhost' is used.");
            Console.WriteLine(formatting, string.Format("-d, --{0} name", StarOption.Db), "The database to use. 'Default' is used if not given.");
            Console.WriteLine(formatting, string.Format("--{0}", StarOption.LogSteps), "Enables diagnostic logging.");
            Console.WriteLine(formatting, string.Format("--{0}", StarOption.NoDb), "Tells the host to load and run the executable");
            Console.WriteLine(formatting, "", "without loading any database into the process.");
            Console.WriteLine(formatting, string.Format("--{0}", StarOption.NoAutoCreateDb), "Prevents automatic creation of database.");
            if (extended) {
                Console.WriteLine(formatting, string.Format("--{0} level", StarOption.Verbosity), "Sets the verbosity level of star.exe (quiet, ");
                Console.WriteLine(formatting, "", "minimal, verbose, diagnostic). Minimal is the default.");
                Console.WriteLine(formatting, string.Format("--{0}", StarOption.Syntax), "Shows the parsing of the command-line, then exits.");
                Console.WriteLine(formatting, string.Format("--{0}", StarOption.NoColor), "Instructs star.exe to turn off colorizing output.");
                Console.WriteLine(formatting, string.Format("--{0}", StarOption.AttatchCodeHostDebugger), "Attaches a debugger to the code host process.");
            }
            if (unofficial) {
                Console.WriteLine(formatting, string.Format("--{0}", SharedCLI.UnofficialOptions.Debug), "Attaches a debugger to the star.exe process.");
            }
            Console.WriteLine();
            if (extended) {
                Console.WriteLine("TEMPORARY: Creating a repository.");
                Console.WriteLine("Run star.exe @@CreateRepo path [servername] to create a repository.");
                Console.WriteLine();
            }
            Console.WriteLine("Environment variables:");
            formatting = "{0,-30}{1,25}";
            Console.WriteLine(formatting, StarcounterEnvironment.VariableNames.DefaultServer, "Holds the server to use by default.");
            Console.WriteLine(formatting, StarcounterEnvironment.VariableNames.DefaultServerPersonalPort, "Personal server port used by default.");
            Console.WriteLine(formatting, StarcounterEnvironment.VariableNames.DefaultServerSystemPort, "System server port used by default.");
            Console.WriteLine();
            Console.WriteLine("For complete help, see {0}{1}.", StarcounterEnvironment.InternetAddresses.StarcounterWiki, "star.exe");
        }

        static IApplicationSyntax DefineCommandLineSyntax() {
            ApplicationSyntaxDefinition appSyntax;
            CommandSyntaxDefinition commandSyntax;

            appSyntax = new ApplicationSyntaxDefinition();
            appSyntax.ProgramDescription = "star.exe";
            appSyntax.DefaultCommand = "exec";
            SharedCLI.DefineWellKnownOptions(appSyntax, true);

            appSyntax.DefineFlag(
                StarOption.Help,
                "Prints the star.exe help message.",
                OptionAttributes.Default,
                new string[] { "h" }
                );
            appSyntax.DefineFlag(
                StarOption.HelpEx,
                "Prints the star.exe extended help message.",
                OptionAttributes.Default,
                new string[] { "hx" }
                );
            appSyntax.DefineFlag(
                StarOption.Version,
                "Prints the version of Starcounter.",
                OptionAttributes.Default,
                new string[] { "v" }
                );
            appSyntax.DefineFlag(
                StarOption.Info,
                "Prints information about Starcounter and star.exe",
                OptionAttributes.Default,
                new string[] { "i" }
                );

            // Extended, advanced functionality

            appSyntax.DefineProperty(
                StarOption.Verbosity,
                "Sets the verbosity of the program (quiet, minimal, verbose, diagnostic). Minimal is the default."
                );
            appSyntax.DefineFlag(
                StarOption.Syntax,
                "Instructs star.exe to just parse the command-line and show the result of that."
                );
            appSyntax.DefineFlag(
                StarOption.NoColor,
                "Instructs star.exe to turn off colorizing output."
                );
            appSyntax.DefineFlag(
                StarOption.AttatchCodeHostDebugger,
                "Attaches a debugger to the code host process after it has started."
                );

            // NOTE:
            // Although we will refuse to execute any EXEC command without at least one parameter,
            // we specify a minimum of 0. The reason is that the exec command is the default, and it
            // will be applied whenever a command is not explicitly given. This in turn means that
            // if we invoke star.exe with a single global option, like --help, the parser will fail
            // since it will apply exec as the default command and force it to have parameters.
            commandSyntax = appSyntax.DefineCommand("exec", "Executes an application", 0, int.MaxValue);

            return appSyntax.CreateSyntax();
        }

        static void SyntaxTreeToConsole(IApplicationSyntax syntax) {
            Console.WriteLine(syntax.ProgramDescription);
            Console.WriteLine("Default command: {0}, required: {1}", syntax.DefaultCommand, syntax.RequiresCommand ? bool.TrueString : bool.FalseString);

            foreach (var prop in syntax.Properties) {
                Console.WriteLine("  {0}  = value", string.Join(",", prop.AllNames));
            }

            foreach (var flag in syntax.Flags) {
                Console.WriteLine("  {0}", string.Join(",", flag.AllNames));
            }

            foreach (var command in syntax.Commands) {
                var supportOptions = command.Properties.Length > 0 || command.Flags.Length > 0;
                string parameterString = "[parameters]";    // TODO:
                Console.WriteLine("    {0}", command.Name);
                foreach (var prop in command.Properties) { 
                    Console.WriteLine("      {0}  = value", string.Join(",", prop.AllNames));
                }
                foreach (var flag in command.Flags) {
                    Console.WriteLine("      {0}", string.Join(",", flag.AllNames));
                }
                Console.WriteLine("      {0}", parameterString);
            }
        }

        // a = \"value\"
        // b = \"value\"
        // x = TRUE
        // exec
        //   d = value
        //   y = TRUE
        //   parameter1, parameter2, parameter3
        static void ParsedArgumentsToConsole(ApplicationArguments args, IApplicationSyntax syntax) {
            var section = CommandLineSection.GlobalOptions;
            string value;

            foreach (var prop in syntax.Properties) {
                if (args.TryGetProperty(prop.Name, section, out value)) {
                    Console.WriteLine("{0}=\"{1}\"", prop.Name, value);
                }
            }

            foreach (var flag in syntax.Flags) {
                if (args.ContainsFlag(flag.Name, section)) {
                    Console.WriteLine("{0}=TRUE", flag.Name);
                }
            }

            if (!args.HasCommmand)
                return;
            
            Console.WriteLine(args.Command);
            section = CommandLineSection.CommandParametersAndOptions;
            var commandSyntax = syntax.Commands.First<ICommandSyntax>((candidate) => {
                return candidate.Name.Equals(args.Command, StringComparison.InvariantCultureIgnoreCase);
            });

            foreach (var prop in commandSyntax.Properties) {
                if (args.TryGetProperty(prop.Name, out value)) {
                    Console.WriteLine("  {0}=\"{1}\"", prop.Name, value);
                }
            }

            foreach (var flag in commandSyntax.Flags) {
                if (args.ContainsFlag(flag.Name)) {
                    Console.WriteLine("  {0}=TRUE", flag.Name);
                }
            }

            Console.Write("  ");
            foreach (var param in args.CommandParameters) {
                Console.Write(param + " ");
            }

            Console.WriteLine();
        }

        static void CreateServerRepository(ApplicationArguments args) {
            if (args.CommandParameters.Count < 2) {
                ConsoleUtil.ToConsoleWithColor("Missing required path argument to @@CreateRepo.", ConsoleColor.Red);
                Console.WriteLine();
                Usage(null);
                return;
            }

            var repositoryPath = args.CommandParameters[1];
            string serverName;

            if (args.CommandParameters.Count > 2) {
                serverName = args.CommandParameters[2];
            } else {
                serverName = StarcounterEnvironment.ServerNames.PersonalServer;
            }

            var setup = RepositorySetup.NewDefault(repositoryPath, serverName);
            setup.Execute();

            ConsoleUtil.ToConsoleWithColor(
                string.Format("New repository \"{0}\" created at {1}", serverName, repositoryPath), ConsoleColor.Green);
        }
    }
}
