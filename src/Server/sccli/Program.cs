
using Starcounter;
using Starcounter.CommandLine;
using Starcounter.CommandLine.Syntax;
using Starcounter.Internal;
using Starcounter.Server;
using Starcounter.Server.Rest.MessageTypes;
using Starcounter.Server.Setup;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace star {

    class Program {
       
        static void Main(string[] args) {
            ApplicationArguments appArgs;
            int serverPort;
            string serverName;
            string serverHost;

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

            try {
                SharedCLI.ResolveAdminServer(appArgs, out serverHost, out serverPort, out serverName);
            } catch (Exception e) {
                uint errorCode;
                if (!ErrorCode.TryGetCode(e, out errorCode)) {
                    errorCode = Error.SCERRUNSPECIFIED;
                }
                ConsoleUtil.ToConsoleWithColor(e.Message, ConsoleColor.Red);
                Environment.ExitCode = (int)errorCode;
                return;
            }

            HttpClient client;
            HttpResponseMessage response;
            ExecRequest execRequest;
            string database;
            string relativeUri;

            execRequest = GatherAndCreateExecRequest(appArgs, out database, out relativeUri);

            client = new HttpClient() { BaseAddress = new Uri(string.Format("http://{0}:{1}", serverHost, serverPort)) };
            try {
                var x = Exec(client, execRequest, serverName, database, relativeUri);
                x.Wait();
                response = x.Result;
            }
            catch (SocketException se) {
                ShowSocketErrorAndSetExitCode(se, client.BaseAddress, serverName);
                return;
            } catch (AggregateException ae) {
                var cause = ae.GetBaseException();
                while (!(cause is SocketException)) {
                    if (cause.InnerException != null) {
                        cause = cause.InnerException;
                        continue;
                    }
                    throw;
                }

                // We got a socket level exception. Check if it's one we
                // can provide some better information for and/or map to
                // any of our well-known error codes.

                ShowSocketErrorAndSetExitCode((SocketException)cause, client.BaseAddress, serverName);
                return;
            }

            ShowResultAndSetExitCode(execRequest, response, appArgs).Wait();
        }


        static async Task<HttpResponseMessage> Exec(
            HttpClient client,
            ExecRequest execRequest,
            string serverName,
            string database,
            string uri) {
            string requestBody;
            Task<HttpResponseMessage> request;

            // Create the request body, based on supplied arguments.

            requestBody = CreateRequestBody(execRequest);

            // Post the request.

            request = client.PostAsync(uri, new StringContent(requestBody, Encoding.UTF8, "application/json"));

            // After posting and while waiting for the result to become available,
            // lets give some feedback.

            ConsoleUtil.ToConsoleWithColor(
                string.Format("[Starting \"{0}\" in \"{1}\" on \"{2}\" ({3}:{4})]",
                Path.GetFileName(execRequest.ExecutablePath),
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

        static ExecRequest GatherAndCreateExecRequest(
            ApplicationArguments args,
            out string database,
            out string relativeUri) {
            ExecRequest request;
            string executable;

            database = SharedCLI.ResolveDatabase(args);
            relativeUri = string.Format("/databases/{0}/executables", database);

            // Aware of the client transparency guideline stated previously,
            // we still do resolve the path of the given executable based on
            // the location of the client. It's most likely what the user
            // intended.
            // On top of that, we check if we can find the file once resolved
            // and if we can't, we do a try on the given name + the .exe file
            // extension. If we find such file, we assume the user meant that
            // to be the file. In any case, we always let the final word be up
            // to the server.
            executable = args.CommandParameters[0];
            executable = Path.GetFullPath(executable);
            if (!File.Exists(executable)) {
                var executableEx = args.CommandParameters[0] + ".exe";
                executableEx = Path.GetFullPath(executableEx);
                if (File.Exists(executableEx)) {
                    executable = executableEx;
                }
            }

            request = new ExecRequest() {
                ExecutablePath = executable,
                CommandLineString = string.Empty,
                ResourceDirectoriesString = string.Empty,
                NoDb = args.ContainsFlag(StarOption.NoDb),
                LogSteps = args.ContainsFlag(StarOption.LogSteps),
                CanAutoCreateDb = !args.ContainsFlag(StarOption.NoAutoCreateDb)
            };

            // Check if we have any arguments we ultimately must pass on
            // to a user code entrypoint.

            if (args.CommandParameters != null) {
                int userArgsCount = args.CommandParameters.Count;

                // Check if we have more arguments than one. Remember that we
                // reserve the first argument as the name/path of the executable
                // and that we are really hiding a general "Exec exe [others]"
                // scenario.

                if (userArgsCount > 1) {
                    userArgsCount--;
                    var userArgs = new string[userArgsCount];
                    args.CommandParameters.CopyTo(1, userArgs, 0, userArgsCount);
                    var binaryArgs = KeyValueBinary.FromArray(userArgs);
                    request.CommandLineString = binaryArgs.Value;
                }
            }

            return request;
        }

        /// <summary>
        /// Creates the request body expected by the admin server when
        /// recieving requests to execute/host an executable.
        /// </summary>
        /// <param name="request">The exec request POCO object that is
        /// serialized in the returned string.
        /// </param>
        /// <returns>A JSON-formatted string representing a request to
        /// execute an executable, compatible with what is expected from
        /// the admin server.</returns>
        static string CreateRequestBody(ExecRequest request) {
            return request.ToJson();
        }

        static async Task ShowResultAndSetExitCode(
            ExecRequest execRequest,
            HttpResponseMessage response, 
            ApplicationArguments args) {
            var content = response.Content.ReadAsStringAsync();
            var body = await content;

            var showHttp = args.ContainsFlag(StarOption.ShowHttp);

            if (showHttp) {
                var request = response.RequestMessage;
                ConsoleUtil.ToConsoleWithColor(() => {
                    Console.WriteLine();
                    Console.WriteLine("HTTP/{0} {1} {2}", request.Version, request.Method, request.RequestUri);
                    foreach (var item in request.Headers) {
                        Console.Write("{0}: ", item.Key);
                        foreach (var item2 in item.Value) {
                            Console.Write(item2 + " ");
                        }
                        Console.WriteLine();
                    }
                    Console.WriteLine();

                }, ConsoleColor.DarkGray);
            }

            int statusCode = (int)response.StatusCode;
            if (statusCode == 201) {
                var responseBody = new ExecResponse201();
                responseBody.PopulateFromJson(body);

                var dbUri = ScUri.FromString(responseBody.DatabaseUri);

                var output = string.Format(
                    "Started \"{0}\" in {1}\"{2}\" (Process:{3})",
                    Path.GetFileName(execRequest.ExecutablePath),
                    responseBody.DatabaseCreated ? "(new database) " : "",
                    dbUri.DatabaseName,
                    responseBody.DatabaseHostPID);
                ConsoleUtil.ToConsoleWithColor(output, ConsoleColor.Green);
                if (args.ContainsFlag(StarOption.AttatchCodeHostDebugger)) {
                    Process.Start("vsjitdebugger.exe", "-p " + responseBody.DatabaseHostPID);
                    Console.ReadLine();
                }
                Environment.ExitCode = 0;
            }
            else if (statusCode == 422) {
                ConsoleUtil.ToConsoleWithColor(body, ConsoleColor.Red);
                Console.WriteLine();
                Environment.ExitCode = (int)Error.SCERREXECUTABLENOTFOUND;

            } else if (statusCode == 404) {
                ConsoleUtil.ToConsoleWithColor(body, ConsoleColor.Red);
                Console.WriteLine();
                if (args.ContainsFlag(StarOption.NoAutoCreateDb)) {
                    Console.WriteLine("To allow automatic creation of the database, remove the --{0} option.", StarOption.NoAutoCreateDb);
                    Console.WriteLine();
                }
                Environment.ExitCode = (int)Error.SCERRDATABASENOTFOUND;

            } else if (!response.IsSuccessStatusCode) {
                // Some error we have no custom formatting for. Just dump
                // out the entire HTTP message.
                showHttp = true;
                Environment.ExitCode = (int)Error.SCERRUNSPECIFIED;

            } else {
                // A successfull response we have custom formatting for. Just
                // dump out the entire HTTP message;
                showHttp = true;
                Environment.ExitCode = 0;
            }

            if (showHttp) {
                var color = response.IsSuccessStatusCode ? ConsoleColor.Green : ConsoleColor.Red;
                ConsoleUtil.ToConsoleWithColor(() => {
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
                    Console.WriteLine(body);

                }, color);
            }
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
            Console.WriteLine("Version={0}.{1}", StarcounterEnvironment.GetVersionInfo().Version.Major, StarcounterEnvironment.GetVersionInfo().Version.Minor);
        }

        static void ShowInfoAboutStarcounter() {
            Console.WriteLine("Installation directory={0}", Environment.GetEnvironmentVariable(StarcounterEnvironment.VariableNames.InstallationDirectory));
            Console.WriteLine("Default server={0}", StarcounterEnvironment.ServerNames.PersonalServer.ToLower());
            Console.WriteLine("Version={0}.{1}", StarcounterEnvironment.GetVersionInfo().Version.Major, StarcounterEnvironment.GetVersionInfo().Version.Minor);
        }

        static void Usage(IApplicationSyntax syntax, bool extended = false) {
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
                Console.WriteLine(formatting, string.Format("--{0}", StarOption.ShowHttp), "Displays underlying HTTP messages.");
                Console.WriteLine(formatting, string.Format("--{0}", StarOption.AttatchCodeHostDebugger), "Attaches a debugger to the code host process.");
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
            SharedCLI.DefineWellKnownOptions(appSyntax);

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
                StarOption.ShowHttp,
                "Displays underlying HTTP request/response messages to/from the admin server."
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
