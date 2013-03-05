
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

namespace star {

    class Program {
       
        static void Main(string[] args) {
            string pipeName;
            
            ApplicationArguments appArgs;

            if (args.Length == 0) {
                Usage(null);
                return;
            }

            // Change to port (default 8181).
            // The different options to specify port and/or server and
            // what has precedence.
            //   If NOTHING is given, star.exe should detect the configured
            // default server and use the default port of that.
            //   If a port is specified (as an option or as set in the
            // environment), star.exe should utilize that port to communicate.
            //   If a server name is given,
            //     and no port is given, star.exe should use the default port
            // for the specified server. If the name is not recognized, it
            // should emit an error.
            //     and a port is given, the port takes precedence. The server
            // name will be queried from said port, and compared to the name
            // given.
            // TODO:

            pipeName = Environment.GetEnvironmentVariable("STAR_SERVER");
            if (string.IsNullOrEmpty(pipeName)) {
                pipeName = StarcounterEnvironment.ServerNames.PersonalServer.ToLower();
            }

            // If not silent or suppressed banner, and only after we know
            // we actually need to communicate with the server.
            ConsoleUtil.ToConsoleWithColor(string.Format("[Using server \"{0}\"]", pipeName), ConsoleColor.DarkGray);

            // Make this a (non-documented) option.
            // TODO:

            var syntax = DefineCommandLineSyntax();

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

            if (appArgs.ContainsFlag("help", CommandLineSection.GlobalOptions)) {
                Usage(syntax);
                return;
            }

            if (appArgs.ContainsFlag("info", CommandLineSection.GlobalOptions)) {
                ShowInfoAboutStarcounter();
                return;
            }

            if (appArgs.ContainsFlag("version", CommandLineSection.GlobalOptions)) {
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

            var executable = appArgs.CommandParameters[0];

            // Aware of the above paragraph, we still do resolve the path of the
            // given executable based on the location of the client.
            executable = Path.GetFullPath(executable);

            // Craft a ExecRequest and POST it to the server.

            var execRequestString = string.Format("{{ \"ExecutablePath\": \"{0}\" }}", executable);

            // We need the base URI and the relative one for the database
            // executable collection.
            // TODO:

            var client = new HttpClient() { BaseAddress = new Uri("http://localhost:8181") };
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            client.PostAsync(
                "/databases/default/executables", 
                new StringContent(execRequestString, Encoding.UTF8, "application/json")).ContinueWith((posttask) => {
                    Console.WriteLine(posttask.Result.StatusCode);
                });

            Console.ReadLine();
            // new NotImplementedException();
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
            Console.WriteLine(formatting, "-h, --help", "Shows help about star.exe.");
            Console.WriteLine(formatting, "-v, --version", "Prints the version of Starcounter.");
            Console.WriteLine(formatting, "-i, --info", "Prints information about the Starcounter installation.");
            Console.WriteLine(formatting, "-p, --serverport port", "The port to use to the admin server.");
            Console.WriteLine(formatting, "-d, --db name|uri", "The database to use. 'Default' is used if not given.");
            Console.WriteLine(formatting, "--server name", "Specifies the name of the server. If no port is");
            Console.WriteLine(formatting, "", "specified, star.exe use the known port of server.");
            Console.WriteLine(formatting, "--logsteps", "Enables diagnostic logging.");
            Console.WriteLine(formatting, "--verbosity level", "Sets the verbosity level of star.exe (quiet, ");
            Console.WriteLine(formatting, "", "minimal, verbose, diagnostic). Minimal is the default.");
            Console.WriteLine();
            Console.WriteLine("TEMPORARY: Creating a repository.");
            Console.WriteLine("Run star.exe @@CreateRepo path [servername] to create a repository.");
            Console.WriteLine();
            Console.WriteLine("Environment variables:");
            Console.WriteLine("STAR_SERVER\t{0,8}", "Sets the server to use by default.");
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
                "help",
                "Prints the star.exe help message.",
                OptionAttributes.Default,
                new string[] { "h" }
                );
            appSyntax.DefineFlag(
                "version",
                "Prints the version of Starcounter.",
                OptionAttributes.Default,
                new string[] { "v" }
                );
            appSyntax.DefineFlag(
                "info",
                "Prints information about the Starcounter and star.exe",
                OptionAttributes.Default,
                new string[] { "i" }
                );
            appSyntax.DefineProperty(
                "serverport",
                "The port of the server to use.",
                OptionAttributes.Default,
                new string[] { "p" }
                );
            appSyntax.DefineProperty(
                "db",
                "The database to use for commands that support it.",
                OptionAttributes.Default,
                new string[] { "d" }
                );
            appSyntax.DefineProperty(
                "verbosity",
                "Sets the verbosity of the program (quiet, minimal, verbose, diagnostic). Minimal is the default."
                );
            appSyntax.DefineProperty(
                "server",
                "Sets the name of the server to use."
                );
            appSyntax.DefineFlag(
                "logsteps",
                "Enables diagnostic logging. When set, Starcounter will produce a set of diagnostic log entries in the log."
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
