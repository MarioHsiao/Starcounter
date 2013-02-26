
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
using System.IO;

namespace star {

    class Program {
        
        static Dictionary<string, Action<Client, string[]>> supportedCommands;
        static Program() {
            supportedCommands = new Dictionary<string, Action<Client, string[]>>();
            supportedCommands.Add("help", (c, args) => {
                var s = "Supported commands:" + Environment.NewLine;
                foreach (var item in supportedCommands) {
                    s += "  *" + item.Key + Environment.NewLine;
                }
                s += Environment.NewLine;
                ToConsoleWithColor(s, ConsoleColor.Yellow);
            });
            supportedCommands.Add("ping", Program.Ping);
            supportedCommands.Add("getdatabase", Program.GetDatabase);
            supportedCommands.Add("getdatabases", Program.GetDatabases);
            supportedCommands.Add("getserver", Program.GetServerInfo);
            supportedCommands.Add("createdatabase", Program.CreateDatabase);
            supportedCommands.Add("startdatabase", Program.StartDatabase);
            supportedCommands.Add("stopdatabase", Program.StopDatabase);
            supportedCommands.Add("exec", Program.ExecApp);
            supportedCommands.Add("createrepo", Program.CreateServerRepository);
        }

        static void Main(string[] args) {
            string pipeName;
            string command;
            Action<Client, string[]> action;

            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NEW_CLI"))) {
                ApplicationArguments appArgs;

                if (args.Length == 0) {
                    Usage(null);
                    return;
                }

                var syntaxTests = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("STAR_CLI_TEST"));
                var syntax = DefineCommandLineSyntax();

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

                // Currently, nothing more than syntax tests and global switches are
                // supported when using the new syntax.
                // TODO:

                ConsoleUtil.ToConsoleWithColor(
                    string.Format("Support for command \"{0}\" is not yet implemented using the new syntax.", appArgs.Command), ConsoleColor.Red);

                return;
            }

            pipeName = Environment.GetEnvironmentVariable("STAR_SERVER");
            if (string.IsNullOrEmpty(pipeName)) {
                pipeName = StarcounterEnvironment.ServerNames.PersonalServer.ToLower();
            }
            pipeName = ScUriExtensions.MakeLocalServerPipeString(pipeName);
            
            ToConsoleWithColor(string.Format("[Using server \"{0}\"]", pipeName), ConsoleColor.DarkGray);
            var client = ClientServerFactory.CreateClientUsingNamedPipes(pipeName);
            
            command = args.Length == 0 ? string.Empty : args[0].ToLowerInvariant();
            if (command.StartsWith("@")) {
                command = command.Substring(1);
            } else if (!command.Equals(string.Empty)){
                var args2 = new string[args.Length + 1];
                Array.Copy(args, args2, args.Length);
                args2[args2.Length - 1] = "@@Synchronous";
                args = args2;
            }

            if (!supportedCommands.TryGetValue(command, out action)) {
                ToConsoleWithColor(string.Format("Unknown command: {0}", command), ConsoleColor.Red);
                action = supportedCommands["help"];
            }

            try {
                action(client, args);
            } catch (TimeoutException timeout) {
                if (timeout.TargetSite.Name.Equals("Connect")) {
                    ToConsoleWithColor(string.Format("Unable to connect to {0}. Have you started the server?", pipeName), ConsoleColor.Red);
                    return;
                }
                throw;
            }

#if false
            ToConsoleWithColor(
                string.Format(
                    "Command executed in {0} ms.",
                    (DateTime.Now - System.Diagnostics.Process.GetCurrentProcess().StartTime).TotalMilliseconds
                    ),
                ConsoleColor.Green
                );
#endif
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
            Console.WriteLine("Usage: star [options] [command] [[options] parameters]");
            Console.WriteLine();
            Console.WriteLine("\"exec\" is the default command.");
            // Console.WriteLine("To get help about a command, use star [command] -h");
            Console.WriteLine();
            Console.WriteLine("Options:");
            formatting = "  {0,-22}{1,25}";
            Console.WriteLine(formatting, "-h, --help", "Shows help about star.exe.");
            Console.WriteLine(formatting, "-v, --version", "Prints the version of Starcounter.");
            Console.WriteLine(formatting, "-i, --info", "Prints information about the Starcounter installation.");
            Console.WriteLine(formatting, "-d, --db name|uri", "The database to use for commands that support it.");
            Console.WriteLine(formatting, "--logsteps", "Enables diagnostic logging.");
            // Console.WriteLine(formatting, "--verbosity level", "Sets the verbosity level of star.exe (quiet, minimal, verbose, diagnostic). Minimal is the default.");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  exec file [arguments to main]");
            Console.WriteLine("  create name|uri");
            Console.WriteLine("  show name|uri");
            Console.WriteLine();
            Console.WriteLine("Environment variables:");
            Console.WriteLine("STAR_SERVER\t{0,8}", "Sets the server to use by default.");
            Console.WriteLine("STAR_CLI_TEST\t{0,8}", "Used for tests. If set, validates the command-line and return.");
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
                "db",
                "The database to use for commands that support it.",
                OptionAttributes.Default,
                new string[] { "d" }
                );
            appSyntax.DefineProperty(
                "verbosity",
                "Sets the verbosity of the program (quiet, minimal, verbose, diagnostic). Minimal is the default."
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

            commandSyntax = appSyntax.DefineCommand("create", "Creates a database", 1);
            commandSyntax = appSyntax.DefineCommand("show", "Prints info about an object, e.g. a database.", 1);

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

        static void Ping(Client client, string[] args) {
            client.Send("Ping", (Reply reply) => WriteReplyToConsole(reply));
        }

        static void GetServerInfo(Client client, string[] args) {
            client.Send("GetServerInfo", (Reply reply) => WriteReplyToConsole(reply));
        }

        static void CreateDatabase(Client client, string[] args) {
            var props = new Dictionary<string, string>();
            props["Name"] = args[1];
            if (args.Contains<string>("@@Synchronous")) {
                props["@@Synchronous"] = bool.TrueString;
            }
            client.Send("CreateDatabase", props, (Reply reply) => WriteReplyToConsole(reply));
        }

        static void StartDatabase(Client client, string[] args) {
            var props = new Dictionary<string, string>();
            props["Name"] = args[1];
            if (args.Contains<string>("@@Synchronous")) {
                props["@@Synchronous"] = bool.TrueString;
            }
            client.Send("StartDatabase", props, (Reply reply) => WriteReplyToConsole(reply));
        }

        static void StopDatabase(Client client, string[] args) {
            var props = new Dictionary<string, string>();
            props["Name"] = args[1];
            if (args.Contains<string>("stopdb")) {
                props["StopDb"] = bool.TrueString;
            }
            if (args.Contains<string>("@@Synchronous")) {
                props["@@Synchronous"] = bool.TrueString;
            }
            client.Send("StopDatabase", props, (Reply reply) => WriteReplyToConsole(reply));
        }

        static void ExecApp(Client client, string[] args) {
            var props = new Dictionary<string, string>();
            props["AssemblyPath"] = args[1];
            if (args.Contains<string>("@@Synchronous")) {
                props["@@Synchronous"] = bool.TrueString;
            }
            client.Send("ExecApp", props, (Reply reply) => WriteReplyToConsole(reply));
        }

        static void GetDatabase(Client client, string[] args) {
            client.Send("GetDatabase", args[1], (Reply reply) => WriteReplyToConsole(reply));
        }

        static void GetDatabases(Client client, string[] args) {
            client.Send("GetDatabases", (Reply reply) => WriteReplyToConsole(reply));
        }

        static void CreateServerRepository(Client client, string[] args) {
            string repositoryPath = args[1];
            string serverName;

            // Three arguments assume [command] [repo path] [@@Synchronous]. If
            // its more, we'll use the 3rd one as the name of the server.
            if (args.Length > 3) {
                serverName = args[2];
            } else {
                serverName = StarcounterEnvironment.ServerNames.PersonalServer;
            }

            var setup = RepositorySetup.NewDefault(repositoryPath, serverName);
            setup.Execute();

            ToConsoleWithColor(
                string.Format("New repository \"{0}\" created at {1}", serverName, repositoryPath), ConsoleColor.Green);
        }

        static void WriteReplyToConsole(Reply reply) {
            if (reply.IsResponse) {
                ToConsoleWithColor(reply.ToString(), reply.IsSuccess ? ConsoleColor.Green : ConsoleColor.Red);
            }
        }

        static void ToConsoleWithColor(string text, ConsoleColor color) {
            ConsoleUtil.ToConsoleWithColor(text, color);
        }
    }
}
