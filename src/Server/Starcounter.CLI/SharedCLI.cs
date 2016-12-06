
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter.CommandLine;
using Starcounter.CommandLine.Syntax;
using Starcounter.Internal;
using System.Net;
using System.Diagnostics;
using System.IO;
using Starcounter.Server;
using Starcounter.Server.Rest;
using Starcounter.Server.Rest.Representations.JSON;

namespace Starcounter.CLI {
    using Severity = Sc.Tools.Logging.Severity;

    /// <summary>
    /// Provides a set of utilities that can be used by applications
    /// and tools that offer a command-line interface to Starcounter.
    /// </summary>
    /// <remarks>
    /// Examples of standard components and tools that will use this
    /// is star.exe, staradmin.exe and the Visual Studio extension,
    /// the later supporting customization when debugging executables
    /// via the Debug | Command Line project property.
    /// </remarks>
    public static class SharedCLI {
        /// <summary>
        /// Provides the server name used when any of the known server
        /// names doesn't apply.
        /// </summary>
        public const string UnresolvedServerName = "N/A";
        /// <summary>
        /// Provides the default admin server host.
        /// </summary>
        public static string DefaultAdminServerHost = IPAddress.Loopback.ToString();
        /// <summary>
        /// Provides the name of the default database being used when
        /// none is explicitly given.
        /// </summary>
        public const string DefaultDatabaseName = StarcounterConstants.DefaultDatabaseName;
        /// <summary>
        /// Provides the name of the folder storing CLI templates.
        /// </summary>
        public const string CLITemplateFolderName = "cli-templates";
        /// <summary>
        /// Provides the name of the folder storing CLI utility applications.
        /// </summary>
        public const string CLIAppFolderName = "cli-apps";
        
        /// <summary>
        /// Assures a given CLI context is properly initialized when it
        /// has been established it is to be used (i.e. when the calling
        /// code needs to use the CLI services of this assembly from a
        /// CLI client process).
        /// </summary>
        public static void InitCLIContext(string client = KnownClientContexts.UnknownContext) {
            ClientContext.InitCurrent(client);

            // Install custom assembly resolver to be able to resolve
            // third-party web socket library
            // TODO:
        }

        /// <summary>
        /// Defines well-known options, offered by most CLI tools.
        /// </summary>
        public static class Option {
            /// <summary>
            /// Gets the option name of the server port.
            /// </summary>
            public const string Serverport = "serverport";
            /// <summary>
            /// Gets the option name of the user-friendly server name.
            /// </summary>
            public const string Server = "server";
            /// <summary>
            /// Gets the option name of the server host.
            /// </summary>
            public const string ServerHost = "serverhost";
            /// <summary>
            /// Gets the option name of the database.
            /// </summary>
            public const string Db = "database";
            /// <summary>
            /// Gets the option name of the application name.
            /// </summary>
            public const string AppName = "name";
            /// <summary>
            /// Gets the option name of the parameter that specifies where
            /// to look for static resources by default.
            /// </summary>
            public const string ResourceDirectory = "resourcedir";
            /// <summary>
            /// Gets the option name of the paremeter indicating boot
            /// sequence steps to be logged.
            /// </summary>
            public const string LogSteps = "logsteps";
            /// <summary>
            /// Gets the option name of the parameter specifying no
            /// database should be connected to.
            /// </summary>
            public const string NoDb = "nodb";
            /// <summary>
            /// Gets the option name of the parameter that instructs the
            /// infrastructure never to automatically create a database
            /// when such does not exist with a given name.
            /// </summary>
            public const string NoAutoCreateDb = "noautocreate";
            /// <summary>
            /// Gets the option name of the parameter that specifies that
            /// restarting is not allowed, if the about-to-be started object
            /// is already running.
            /// </summary>
            public const string NoRestart = "norestart";
            /// <summary>
            /// Gets the option name of the parameter that specifies that
            /// the executable should be stopped rather than started.
            /// </summary>
            public const string Stop = "stop";
            /// <summary>
            /// Gets the option name of the parameter that instructs the
            /// client to turn on verbose output.
            /// </summary>
            public const string Verbose = "verbose";
            /// <summary>
            /// Gets the option name of the parameter that instructs the
            /// client to turn on detailed output.
            /// </summary>
            public const string Detailed = "detailed";
            /// <summary>
            /// Gets the option name of the parameter that instructs the
            /// client to output matching logs from the server log at the
            /// end of an operation
            /// </summary>
            public const string Logs = "logs";
            /// <summary>
            /// Gets the option name of the parameter that instructs the
            /// client to return as soon as the executable has been passed
            /// to the host, not awaiting the full entrypoint of the
            /// executable to return.
            /// </summary>
            public const string Async = "async";

            /// <summary>
            /// Gets the option name of the parameter that specifies the
            /// starting applications entrypoint should run in a write
            /// transactions, as opposed to a readonly transaction, which
            /// is the default.
            /// </summary>
            public const string TransactMain = "transact";

            /// <summary>
            /// Indicates the starting application should be hosted in an
            /// independent process rather than in the shared host.
            /// </summary>
            public const string SelfHosted = "selfhosted";
        }

        /// <summary>
        /// Defines a set of unofficial options, optionally supported by
        /// our clients, but not neccessary part of the standard documentation.
        /// </summary>
        /// <remarks>
        /// All unofficial options should begin with sc-*.
        /// </remarks>
        public static class UnofficialOptions {
            /// <summary>
            /// Gets the option name of the debug switch, allowing the
            /// debugger to be attached to the active CLI client.
            /// </summary>
            public const string Debug = "sc-debug";

            /// <summary>
            /// Gets the option name of the option that allows customized,
            /// extra parametes to be sent to the code host processes when
            /// spawned by the admin server.
            /// </summary>
            public const string CodeHostCommandLineOptions = "sc-codehostargs";
        }

        static string[] StandardHints = new string[] {
            "Type \"star -h\" to see help about star.exe",
            "Type \"star {0}\" to launch the help page for error {0}"
        };

        static string HintHelp = "Type \"star -h\" to see help";
        static string HintShowErrorPage = "Type \"star {0}\" to launch the help page for error {0}";

        /// <summary>
        /// Defines the verbosity of the current CLI context.
        /// </summary>
        public static OutputLevel Verbosity = CLI.OutputLevel.Minimal;

        /// <summary>
        /// Gets or sets a value indicating if the current client/host
        /// should display verbose output.
        /// </summary>
        public static bool Verbose {
            get {
                return Verbosity == CLI.OutputLevel.Verbose;
            }
        }

        /// <summary>
        /// Gets or sets a value instructing the shared CLI library to
        /// display server logs at the end of a application command.
        /// </summary>
        public static bool ShowLogs = false;

        /// <summary>
        /// Gets or sets a value controlling the minimum severity of
        /// logs to be displayed in case <see cref="ShowLogs"/> is set.
        /// </summary>
        public static Severity LogDisplaySeverity = Severity.Notice;

        /// <summary>
        /// Defines and includes the well-known, shared CLI options in
        /// the given <see cref="SyntaxDefinition"/>.
        /// </summary>
        /// <param name="definition">The <see cref="SyntaxDefinition"/>
        /// in which well-known, shared options should be included.</param>
        /// <param name="includeUnofficial">Indicates if unofficial options
        /// should be included in the definition.</param>
        public static void DefineWellKnownOptions(SyntaxDefinition definition, bool includeUnofficial = false) {
            definition.DefineProperty(
                Option.Serverport,
                "The port of the server to use.",
                OptionAttributes.Default,
                new string[] { "p" }
                );
            definition.DefineProperty(
                Option.Db,
                "The database to use.",
                OptionAttributes.Default,
                new string[] { "d" }
                );
            definition.DefineProperty(
                Option.AppName,
                "A custom name for the application."
                );
            definition.DefineProperty(
                Option.Server,
                "Sets the name of the server to use."
                );
            definition.DefineProperty(
                Option.ServerHost,
                "Specifies identify of the server host."
                );
            definition.DefineProperty(
                Option.ResourceDirectory,
                "Specifies the default directory for static resources."
                );
            definition.DefineFlag(
                Option.LogSteps,
                "Enables diagnostic logging. When set, Starcounter will produce a set of diagnostic log entries in the log."
                );
            definition.DefineFlag(
                Option.NoDb,
                "Specifies the code host should run the executable without loading any database data."
                );
            definition.DefineFlag(
                Option.NoAutoCreateDb,
                "Specifies that a database can not be automatically created if it doesn't exist."
                );
            definition.DefineFlag(
                Option.NoRestart,
                "Specifies that the application can't be restarted if it's already running."
                );
            definition.DefineFlag(
                Option.Stop,
                "Specifies that the application should be stopped."
                );
            definition.DefineFlag(
                Option.Async,
                "Specifies that starting should be considered completed without waiting for the entrypoint."
                );
            definition.DefineFlag(
                Option.Verbose,
                "Specifies that verbose output is to be written."
                );
            definition.DefineFlag(
                Option.Detailed,
                "Specifies that detailed output is to be written."
                );
            definition.DefineFlag(
                Option.Logs,
                "Specifies that log entries are to be displayed."
                );
            definition.DefineFlag(
                Option.SelfHosted,
                "Specifies that the application should be started in a separate process"
                );

            if (includeUnofficial) {
                definition.DefineFlag(
                    UnofficialOptions.Debug,
                    "Attaches a debugger to the target program just after the parsing of the command-line is complete."
                    );
                definition.DefineProperty(
                    UnofficialOptions.CodeHostCommandLineOptions,
                    "Allows for the passing of custom code host command-line arguments"
                    );
            }
        }

        /// <summary>
        /// Parses the given argument set in <paramref name="args"/> using
        /// the given <paramref name="syntax"/>.
        /// </summary>
        /// <param name="args">The command line arguments to parse.</param>
        /// <param name="syntax">The syntax to use</param>
        /// <param name="appArgs">Parsed arguments.</param>
        /// <returns><c>true</c> if the parsing succeeded; <c>false</c>
        /// otherwise.
        /// </returns>
        /// <remarks>
        /// If parsing fails, this method will write a message to the console
        /// and set the environment exit code accordingly.
        /// </remarks>
        public static bool TryParse(string[] args, IApplicationSyntax syntax, out ApplicationArguments appArgs) {
            try {
                appArgs = new Parser(args).Parse(syntax);
            } catch (InvalidCommandLineException e) {
                ConsoleUtil.ToConsoleWithColor(e.Message, ConsoleColor.Red);
                Environment.ExitCode = (int)e.ErrorCode;
                appArgs = null;
                return false;
            }

            if (appArgs.ContainsFlag(Option.Verbose)) {
                Verbosity = OutputLevel.Verbose;
            } else if (appArgs.ContainsFlag(Option.Detailed)) {
                Verbosity = OutputLevel.Verbose;
            }

            if (appArgs.ContainsFlag(Option.Logs)) {
                ShowLogs = true;
            }
            else if (appArgs.ContainsFlag(Option.LogSteps)) {
                ShowLogs = true;
            }

            if (ShowLogs) {
                LogDisplaySeverity = Verbose || appArgs.ContainsFlag(Option.LogSteps)
                    ? Severity.Debug 
                    : Severity.Notice;
            }

            return true;
        }

        /// <summary>
        /// Resolves the admin server host, port and well-known name from a given
        /// set of command-line arguments.
        /// </summary>
        /// <remarks>
        /// For arguments that are not explicitly given, this method uses environment
        /// defaults as a first fallback and finally constants, in case there is no
        /// environment data available.
        /// </remarks>
        /// <param name="args">Command-line arguments, possibly including shared
        /// options.</param>
        /// <param name="host">The host of the admin server.</param>
        /// <param name="port">The admin server port.</param>
        /// <param name="name">The display name of the admin server, e.g. "Personal".
        /// </param>
        public static void ResolveAdminServer(ApplicationArguments args, out string host, out int port,  out string name) {
            string givenPort;
            int personalDefault;
            int systemDefault;

            personalDefault = EnvironmentExtensions.GetEnvironmentInteger(
                StarcounterEnvironment.VariableNames.DefaultServerPersonalPort,
                StarcounterConstants.NetworkPorts.DefaultPersonalServerSystemHttpPort
                );

            systemDefault = EnvironmentExtensions.GetEnvironmentInteger(
                StarcounterEnvironment.VariableNames.DefaultServerSystemPort,
                StarcounterConstants.NetworkPorts.DefaultSystemServerSystemHttpPort
                );

            if (args.TryGetProperty(Option.Serverport, out givenPort)) {
                port = int.Parse(givenPort);

                // If a port is specified, that always have precedence.
                // If it is, we try to pair it with a server name based on
                // the following priorities:
                //   1) Getting a given name on the command-line
                //   2) Trying to pair the port with a default server based
                // on known server port defaults.
                //   3) Finding a server name configured in the environment.
                //   4) Using a const string (e.g. "N/A")

                if (!args.TryGetProperty(Option.Server, out name)) {
                    if (port == personalDefault) {
                        name = StarcounterEnvironment.ServerNames.PersonalServer;
                    } else if (port == systemDefault) {
                        name = StarcounterEnvironment.ServerNames.SystemServer;
                    } else if (port == StarcounterConstants.NetworkPorts.DefaultPersonalServerSystemHttpPort) {
                        name = StarcounterEnvironment.ServerNames.PersonalServer;
                    } else if (port == StarcounterConstants.NetworkPorts.DefaultSystemServerSystemHttpPort) {
                        name = StarcounterEnvironment.ServerNames.SystemServer;
                    } else {
                        name = Environment.GetEnvironmentVariable(StarcounterEnvironment.VariableNames.DefaultServer);
                        if (string.IsNullOrEmpty(name)) {
                            name = UnresolvedServerName;
                        }
                    }
                }
            } else {

                // No port given. See if a server was specified by name and try
                // to figure out a port based on that, or a port based on a server
                // name given in the environment.
                //   If a server name in fact IS specified (and no port is), we
                // must match it against one of the known server names. If it is
                // not part of them, we refuse it.
                //   If no server is specified either on the command line or in the
                // environment, we'll assume personal and the default port for that.

                if (!args.TryGetProperty(Option.Server, out name)) {
                    name = Environment.GetEnvironmentVariable(StarcounterEnvironment.VariableNames.DefaultServer);
                    if (string.IsNullOrEmpty(name)) {
                        name = StarcounterEnvironment.ServerNames.PersonalServer;
                    }
                }

                var comp = StringComparison.InvariantCultureIgnoreCase;

                if (name.Equals(StarcounterEnvironment.ServerNames.PersonalServer, comp)) {
                    port = personalDefault;
                } else if (name.Equals(StarcounterEnvironment.ServerNames.SystemServer, comp)) {
                    port = systemDefault;
                } else {
                    throw ErrorCode.ToException(
                        Error.SCERRUNSPECIFIED,
                        string.Format("Unknown server name: {0}. Please specify the port using '{1}'.",
                        name,
                        Option.Serverport));
                }
            }

            if (!args.TryGetProperty(Option.ServerHost, out host)) {
                host = SharedCLI.DefaultAdminServerHost;
            } else {
                if (host.StartsWith("http", true, null)) {
                    host = host.Substring(4);
                }
                host = host.TrimStart(new char[] { ':', '/' });
            }
        }

        /// <summary>
        /// Resolves the named database to use from a given set of
        /// command-line arguments.
        /// </summary>
        /// <remarks>
        /// If database argument are not explicitly given, this method uses environment
        /// defaults as a first fallback and finally constants, in case there is no
        /// environment data available.
        /// </remarks>
        /// <param name="args">Command-line arguments to consult.</param>
        /// <param name="name">The name of the database.</param>
        public static bool ResolveDatabase(ApplicationArguments args, out string name) {
            string database;
            var stated = args.TryGetProperty(Option.Db, out database);
            if (!stated) {
                database = SharedCLI.DefaultDatabaseName;
            }
            name = database;
            return stated;
        }

        /// <summary>
        /// Resolves the name of the application from a given set of
        /// command-line arguments, and/or defaults.
        /// </summary>
        /// <param name="args">Command-line arguments to consult.</param>
        /// <param name="applicationFilePath">The full path to the logical application
        /// file.</param>
        /// <param name="name">The name of the application.</param>
        public static void ResolveApplication(
            ApplicationArguments args, 
            string applicationFilePath,
            out string name) {

            string application;
            if (!args.TryGetProperty(Option.AppName, out application)) {
                application = Path.GetFileName(applicationFilePath);
            }
            name = application;
        }

        /// <summary>
        /// Resolves any specified resource directories for a given application.
        /// </summary>
        /// <param name="args">CLI arguments</param>
        /// <param name="relativeBasePath">The directory path to qualify relative
        /// resource directories from.</param>
        /// <param name="dirs">Resource directories</param>
        public static void ResolveResourceDirectories(ApplicationArguments args, string relativeBasePath, out string[] dirs) {
            var paths = new List<string>();
            
            string spec;
            if (args.TryGetProperty(SharedCLI.Option.ResourceDirectory, out spec)) {

                var tokens = spec.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var token in tokens) {
                    var candidate = token.Trim('"');

                    try {
                        candidate = Path.Combine(relativeBasePath, candidate);
                        paths.Add(Path.GetFullPath(candidate));
                    }
                    catch (ArgumentException e) {
                        var detail = string.Format(
                            "Parameter --{0} contains an illegal element: '{1}', error: {2}", 
                            SharedCLI.Option.ResourceDirectory, 
                            candidate, 
                            e.Message
                        );
                        throw ErrorCode.ToException(Error.SCERRBADARGUMENTS, e, detail);
                    }
                }
            }

            dirs = paths.ToArray();
        }

        /// <summary>
        /// Writes the given output if the CLI context indicates
        /// it's in verbose mode.
        /// </summary>
        /// <param name="output">The output to write.</param>
        /// <param name="color">The color to write it with.</param>
        public static void ShowVerbose(string output, ConsoleColor color = ConsoleColor.Yellow) {
            if (SharedCLI.Verbose) {
                ConsoleUtil.ToConsoleWithColor(output, color);
            }
        }

        /// <summary>
        /// Writes <paramref name="msg"/> to the console using the default
        /// shared CLI error color and formatting, setting the exit code to
        /// the error given in the strongly typed error message. Possibly
        /// also exits the process, depending on the <paramref name="exit"/>
        /// flag.
        /// </summary>
        /// <param name="msg">The message to write.</param>
        /// <param name="exit">If <c>true</c>, exits the process with the exit code
        /// fetched from the strongly typed error message.</param>
        public static void ShowErrorAndSetExitCode(ErrorMessage msg, bool exit = false) {
            ConsoleColor red = ConsoleColor.Red;
            int exitCode = (int)msg.Code;
            Console.WriteLine();
            ConsoleUtil.ToConsoleWithColor(msg.ToString(), red);
            Console.WriteLine();
            ShowHints(msg.Code);
            if (exit) Environment.Exit(exitCode);
            else Environment.ExitCode = exitCode;
        }

        /// <summary>
        /// Writes <paramref name="detail"/> to the console using the default
        /// shared CLI error color and formatting, setting the exit code to
        /// the error given in the strongly typed error detail. Possibly
        /// also exits the process, depending on the <paramref name="exit"/>
        /// flag.
        /// </summary>
        /// <param name="detail">The message to write.</param>
        /// <param name="exit">If <c>true</c>, exits the process with the exit code
        /// fetched from the strongly typed error detail.</param>
        public static void ShowErrorAndSetExitCode(ErrorDetail detail, bool exit = false) {
            ConsoleColor red = ConsoleColor.Red;
            uint errorCode = (uint)detail.ServerCode;

            Console.WriteLine();

            var text = detail.Text;
            try {
                text = ErrorCode.ToMessage(errorCode).Header + ErrorMessage.HeaderBodyDelimiter + " " + text;
            } catch {}

            ConsoleUtil.ToConsoleWithColor(text, red);
            Console.WriteLine();
            ShowHints(errorCode);
            
            int exitCode = (int)detail.ServerCode;
            if (exit) Environment.Exit(exitCode);
            else Environment.ExitCode = exitCode;
        }

        /// <summary>
        /// Writes the given information to the console and sets the process
        /// exit code.
        /// </summary>
        /// <param name="info">The information to write.</param>
        /// <param name="code">The process exit code to assign the process.</param>
        /// <param name="hint">An optional hint.</param>
        /// <param name="showStandardHints">Indicates if standard hints should
        /// be shown too.</param>
        /// <param name="exit">A boolean specifying if the process should be exited.</param>
        /// <param name="color">An optional console color.</param>
        /// <param name="hintColor">An optional hint console color.</param>
        public static void ShowInformationAndSetExitCode(
            string info, 
            uint code, 
            string hint = null,
            bool showStandardHints = true,
            bool exit = false,
            ConsoleColor color = ConsoleColor.Yellow, 
            ConsoleColor hintColor = ConsoleColor.Yellow) {
            int exitCode = (int)code;
            Console.WriteLine();
            ConsoleUtil.ToConsoleWithColor(info, color);
            Console.WriteLine();
            ShowHints(code, hint, showStandardHints, hintColor);
            if (exit) Environment.Exit(exitCode);
            else Environment.ExitCode = exitCode;
        }

        /// <summary>
        /// Writes the exception <paramref name="e"/> to the console, after first
        /// formatting it to an error message. Sets the exit code to according to
        /// the exception.
        /// </summary>
        /// <param name="e">The exception to act upon.</param>
        /// <param name="showStackTrace">Pass <c>true</c> to have this method
        /// include the stacktrace when writing to the console.</param>
        /// <param name="exit">If <c>true</c>, exits the process with the exit code
        /// fetched from the strongly typed error message.</param>
        public static void ShowErrorAndSetExitCode(Exception e, bool showStackTrace = true, bool exit = false) {
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
                ConsoleUtil.ToConsoleWithColor($"{e.GetType().Name}: {e.Message}", ConsoleColor.Red);
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

        internal static void ShowHints(uint error, string specificHint = null, bool showStandardHints = true, ConsoleColor color = ConsoleColor.Yellow) {
            if (specificHint != null) {
                ConsoleUtil.ToConsoleWithColor(specificHint, color);
            }
            if (showStandardHints) {
                ConsoleUtil.ToConsoleWithColor(HintHelp, color);
                if (error > 0) {
                    ConsoleUtil.ToConsoleWithColor(string.Format(HintShowErrorPage, error), color);
                }
            }
        }
    }
}