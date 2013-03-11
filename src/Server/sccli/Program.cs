
using Starcounter;
using Starcounter.ABCIPC;
using Starcounter.ABCIPC.Internal;
using Starcounter.Internal;
using Starcounter.CommandLine;
using Starcounter.CommandLine.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using Starcounter.Server.Setup;
using Starcounter.Server.PublicModel;
using Starcounter.Server.PublicModel.Commands;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using Newtonsoft.Json;

namespace star {

    class Program {

        const string UnresolvedServerName = "N/A";
        const string DefaultAdminServerHost = "localhost";
        const string DefaultDatabaseName = StarcounterConstants.DefaultDatabaseName;

        static class Option {
            public const string Help = "help";
            public const string Version = "version";
            public const string Info = "info";
            public const string Serverport = "serverport";
            public const string Server = "server";
            public const string ServerHost = "serverhost";
            public const string Db = "db";
            public const string Verbosity = "verbosity";
            public const string LogSteps = "logsteps";
            public const string NoDb = "nodb";
            public const string NoAutoCreateDb = "noautocreate";
        }

        static class EnvironmentVariable {
            public const string ServerName = "STAR_SERVER";
        }

        static void GetAdminServerPortAndName(ApplicationArguments args, out int port, out string serverName) {
            string givenPort;

            // Use proper error code / message for unresolved server
            // TODO:

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

                if (!args.TryGetProperty(Option.Server, out serverName)) {
                    if (port == StarcounterConstants.NetworkPorts.DefaultPersonalServerSystemHttpPort) {
                        serverName = StarcounterEnvironment.ServerNames.PersonalServer;
                    } else if (port == StarcounterConstants.NetworkPorts.DefaultSystemServerSystemHttpPort) {
                        serverName = StarcounterEnvironment.ServerNames.SystemServer;
                    } else {
                        serverName = Environment.GetEnvironmentVariable(EnvironmentVariable.ServerName);
                        if (string.IsNullOrEmpty(serverName)) {
                            serverName = UnresolvedServerName;
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

                if (!args.TryGetProperty(Option.Server, out serverName)) {
                    serverName = Environment.GetEnvironmentVariable(EnvironmentVariable.ServerName);
                    if (string.IsNullOrEmpty(serverName)) {
                        serverName = StarcounterEnvironment.ServerNames.PersonalServer;
                    }
                }

                var comp = StringComparison.InvariantCultureIgnoreCase;

                if (serverName.Equals(StarcounterEnvironment.ServerNames.PersonalServer, comp)) {
                    port = StarcounterConstants.NetworkPorts.DefaultPersonalServerSystemHttpPort;
                } else if (serverName.Equals(StarcounterEnvironment.ServerNames.SystemServer, comp)) {
                    port = StarcounterConstants.NetworkPorts.DefaultSystemServerSystemHttpPort;
                } else {
                    throw ErrorCode.ToException(
                        Error.SCERRUNSPECIFIED,
                        string.Format("Unknown server name: {0}. Please specify the port using '{1}'.", 
                        serverName, 
                        Option.Serverport));
                }                
            }
        }
       
        static void Main(string[] args) {
            ApplicationArguments appArgs;

            if (args.Length == 0) {
                Usage(null);
                return;
            }

            var syntax = DefineCommandLineSyntax();

            // Make this a (non-documented) option.
            // TODO:

            var syntaxTests = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("STAR_CLI_TEST"));
            if (syntaxTests) {
                ConsoleUtil.ToConsoleWithColor(() => { SyntaxTreeToConsole(syntax); }, ConsoleColor.DarkGray);
            }

            // Parse and evaluate the given input

            var parser = new Parser(args);
            try {
                appArgs = parser.Parse(syntax);
            } catch (InvalidCommandLineException e) {
                ConsoleUtil.ToConsoleWithColor(e.ToString(), ConsoleColor.Red);
                Environment.ExitCode = (int)e.ErrorCode;
                return;
            }

            if (syntaxTests) {
                ConsoleUtil.ToConsoleWithColor(() => { ParsedArgumentsToConsole(appArgs, syntax); }, ConsoleColor.Green);
                // Exiting, because we were asked to test syntax only.
                return;
            }

            // Process global options that has precedence

            if (appArgs.ContainsFlag(Option.Help, CommandLineSection.GlobalOptions)) {
                Usage(syntax);
                return;
            }

            if (appArgs.ContainsFlag(Option.Info, CommandLineSection.GlobalOptions)) {
                ShowInfoAboutStarcounter();
                return;
            }

            if (appArgs.ContainsFlag(Option.Version, CommandLineSection.GlobalOptions)) {
                ShowVersionInfo();
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
            
            // First make sure we have a server/port to communicate with.
            int serverPort;
            string serverName;
            string serverHost;

            try {
                GetAdminServerPortAndName(appArgs, out serverPort, out serverName);
            } catch (Exception e) {
                uint errorCode;
                if (!ErrorCode.TryGetCode(e, out errorCode)) {
                    errorCode = Error.SCERRUNSPECIFIED;
                }
                ConsoleUtil.ToConsoleWithColor(e.Message, ConsoleColor.Red);
                Environment.ExitCode = (int)errorCode;
                return;
            }
            if (!appArgs.TryGetProperty(Option.ServerHost, out serverHost)) {
                serverHost = Program.DefaultAdminServerHost;
            } else {
                if (serverHost.StartsWith("http", true, null)) {
                    serverHost = serverHost.Substring(4);
                }
                serverHost = serverHost.TrimStart(new char[] { ':', '/' });
            }

            HttpClient client;
            HttpResponseMessage response;
            
            client = new HttpClient() { BaseAddress = new Uri(string.Format("http://{0}:{1}", serverHost, serverPort)) };
            var x = Exec(client, serverName, appArgs);
            x.Wait();
            response = x.Result;

            ShowResultAndSetExitCode(response, appArgs).Wait();
        }

        static async Task<HttpResponseMessage> Exec(HttpClient client, string serverName, ApplicationArguments args) {
            string database;
            string uri;
            string executable;
            string requestBody;
            Task<HttpResponseMessage> request;

            if (!args.TryGetProperty(Option.Db, out database)) {
                database = Program.DefaultDatabaseName;
            }
            uri = string.Format("/databases/{0}/executables", database);

            // Aware of the client transparency guideline stated previously,
            // we still do resolve the path of the given executable based on
            // the location of the client. It's most likely what the user
            // intended.
            executable = args.CommandParameters[0];
            executable = Path.GetFullPath(executable);

            // Create the request body, based on supplied arguments.

            requestBody = CreateRequestBody(executable, args);

            // Post the request.

            request = client.PostAsync(uri, new StringContent(requestBody, Encoding.UTF8, "application/json"));

            // After posting and while waiting for the result to become available,
            // lets give some feedback.

            ConsoleUtil.ToConsoleWithColor(
                string.Format("[Executing \"{0}\" in \"{1}\" on \"{2}\" ({3}:{4})]",
                Path.GetFileName(executable),
                database,
                serverName,
                client.BaseAddress.Host, 
                client.BaseAddress.Port), 
                ConsoleColor.DarkGray
                );

            // Await the requested result, then return the result.

            await request;
            return request.Result;
        }

        /// <summary>
        /// Creates the request body expected by the admin server when
        /// recieving requests to execute/host an executable.
        /// </summary>
        /// <remarks>
        /// See the ExecRequest class in Administrator for the format being
        /// used.
        /// </remarks>
        /// <param name="executable">The executable (required)</param>
        /// <param name="args">Arguments, possibly holding options to be
        /// part of the request string.</param>
        /// <returns>A JSON-formatted string representing a request to
        /// execute an executable, compatible with what is expected from
        /// the admin server.</returns>
        static string CreateRequestBody(string executable, ApplicationArguments args) {
            // We use a simple but probably slow technique to construct
            // the body. The upside is that we dont accidentally format
            // the string inproperly.
            // Revise and reconsider this when we redesign this and have
            // it usable from all contexts (like VS and the shell).

            var request = new {
                ExecutablePath = executable,
                CommandLineString = string.Empty,
                ResourceDirectoriesString = string.Empty,
                NoDb = args.ContainsFlag(Option.NoDb),
                LogSteps = args.ContainsFlag(Option.LogSteps),
                CanAutoCreateDb = !args.ContainsFlag(Option.NoAutoCreateDb)
            };
            return JsonConvert.SerializeObject(request);
        }

        static async Task ShowResultAndSetExitCode(
            HttpResponseMessage response, 
            ApplicationArguments args) {
            
            var content = response.Content.ReadAsStringAsync();
            var color = response.IsSuccessStatusCode ? ConsoleColor.Green : ConsoleColor.Red;
            Console.ForegroundColor = color;
            try {
                Console.WriteLine();
                Console.WriteLine("HTTP/{0} {1} {2}", response.Version, (int)response.StatusCode, response.ReasonPhrase);
                foreach (var item in response.Headers) {
                    Console.Write("{0}: ", item.Key);
                    foreach (var item2 in item.Value) {
                        Console.Write(item2 + " ");
                    }
                    Console.WriteLine();
                }
                Console.WriteLine();
                var body = await content;
                Console.WriteLine(body);

            } finally {
                Console.ResetColor();
                Environment.ExitCode = response.IsSuccessStatusCode ? 0 : (int)Error.SCERRUNSPECIFIED;
            }
        }

        static void ShowVersionInfo() {
            Console.WriteLine("Version={0}.{1}", StarcounterEnvironment.GetVersionInfo().Version.Major, StarcounterEnvironment.GetVersionInfo().Version.Minor);
        }

        static void ShowInfoAboutStarcounter() {
            Console.WriteLine("Installation directory={0}", Environment.GetEnvironmentVariable(StarcounterEnvironment.VariableNames.InstallationDirectory));
            Console.WriteLine("Default server={0}", StarcounterEnvironment.ServerNames.PersonalServer.ToLower());
            Console.WriteLine("Version={0}.{1}", StarcounterEnvironment.GetVersionInfo().Version.Major, StarcounterEnvironment.GetVersionInfo().Version.Minor);
        }

        static void Usage(IApplicationSyntax syntax) {
            string formatting;
            Console.WriteLine("Usage: star [options] executable [parameters]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            formatting = "  {0,-22}{1,25}";
            Console.WriteLine(formatting, string.Format("-h, --{0}", Option.Help), "Shows help about star.exe.");
            Console.WriteLine(formatting, string.Format("-v, --{0}", Option.Version), "Prints the version of Starcounter.");
            Console.WriteLine(formatting, string.Format("-i, --{0}", Option.Info), "Prints information about the Starcounter installation.");
            Console.WriteLine(formatting, string.Format("-p, --{0} port", Option.Serverport), "The port to use to the admin server.");
            Console.WriteLine(formatting, string.Format("--{0} name", Option.Server), "Specifies the name of the server. If no port is");
            Console.WriteLine(formatting, "", "specified, star.exe use the known port of server.");
            Console.WriteLine(formatting, string.Format("--{0} host", Option.ServerHost), "Specifies the identity of the server host. If no");
            Console.WriteLine(formatting, "", "host is specified, 'localhost' is used.");
            Console.WriteLine(formatting, string.Format("-d, --{0} name", Option.Db), "The database to use. 'Default' is used if not given.");
            Console.WriteLine(formatting, string.Format("--{0}", Option.LogSteps), "Enables diagnostic logging.");
            Console.WriteLine(formatting, string.Format("--{0}", Option.NoDb), "Tells the host to load and run the executable");
            Console.WriteLine(formatting, "", "without loading any database into the process.");
            Console.WriteLine(formatting, string.Format("--{0}", Option.NoAutoCreateDb), "Prevents automatic creation of database.");
            Console.WriteLine(formatting, string.Format("--{0} level", Option.Verbosity), "Sets the verbosity level of star.exe (quiet, ");
            Console.WriteLine(formatting, "", "minimal, verbose, diagnostic). Minimal is the default.");
            Console.WriteLine();
            Console.WriteLine("TEMPORARY: Creating a repository.");
            Console.WriteLine("Run star.exe @@CreateRepo path [servername] to create a repository.");
            Console.WriteLine();
            Console.WriteLine("Environment variables:");
            Console.WriteLine("{0}\t{1,8}", EnvironmentVariable.ServerName, "Sets the server to use by default.");
            Console.WriteLine();
            Console.WriteLine("For complete help, see http://www.starcounter.com/wiki/star.exe");
        }

        static IApplicationSyntax DefineCommandLineSyntax() {
            ApplicationSyntaxDefinition appSyntax;
            CommandSyntaxDefinition commandSyntax;

            appSyntax = new ApplicationSyntaxDefinition();
            appSyntax.ProgramDescription = "star.exe";
            appSyntax.DefaultCommand = "exec";
            appSyntax.DefineFlag(
                Option.Help,
                "Prints the star.exe help message.",
                OptionAttributes.Default,
                new string[] { "h" }
                );
            appSyntax.DefineFlag(
                Option.Version,
                "Prints the version of Starcounter.",
                OptionAttributes.Default,
                new string[] { "v" }
                );
            appSyntax.DefineFlag(
                Option.Info,
                "Prints information about Starcounter and star.exe",
                OptionAttributes.Default,
                new string[] { "i" }
                );
            appSyntax.DefineProperty(
                Option.Serverport,
                "The port of the server to use.",
                OptionAttributes.Default,
                new string[] { "p" }
                );
            appSyntax.DefineProperty(
                Option.Db,
                "The database to use for commands that support it.",
                OptionAttributes.Default,
                new string[] { "d" }
                );
            appSyntax.DefineProperty(
                Option.Verbosity,
                "Sets the verbosity of the program (quiet, minimal, verbose, diagnostic). Minimal is the default."
                );
            appSyntax.DefineProperty(
                Option.Server,
                "Sets the name of the server to use."
                );
            appSyntax.DefineProperty(
                Option.ServerHost,
                "Specifies identify of the server host. Default is 'localhost'."
                );
            appSyntax.DefineFlag(
                Option.LogSteps,
                "Enables diagnostic logging. When set, Starcounter will produce a set of diagnostic log entries in the log."
                );
            appSyntax.DefineFlag(
                Option.NoDb,
                "Specifies the code host should run the executable without loading any database data."
                );
            appSyntax.DefineFlag(
                Option.NoAutoCreateDb,
                "Specifies that a database can not be automatically created if it doesn't exist."
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
