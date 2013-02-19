
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
                IApplicationSyntax syntax;

                var result = TryGetProgramArguments(args, out appArgs, out syntax);
                
                // No matter the result, display the syntax tree.
                // TODO:

                if (result) {
                    // Visualize the given arguments, how they are parsed.
                    // TODO:
                }

                return;
            }

            pipeName = Environment.GetEnvironmentVariable("star_servername");
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

        static bool TryGetProgramArguments(string[] args, out ApplicationArguments arguments, out IApplicationSyntax syntax) {
            ApplicationSyntaxDefinition syntaxDefinition;
            CommandSyntaxDefinition commandDefinition;
            Parser parser;

            // Define the general program syntax

            syntaxDefinition = new ApplicationSyntaxDefinition();
            syntaxDefinition.ProgramDescription = "The Starcounter command-line interface.";
            syntaxDefinition.DefaultCommand = "exec";

            // Define the global property allowing the verbosity level
            // of the program to be changed.

            syntaxDefinition.DefineProperty(
                "verbosity",
                "Sets the verbosity of the program (quiet, minimal, verbose, diagnostic). Minimal is the default."
                );

            // Define the global flag allowing a debugger to be attached
            // to the process when starting. Undocumented, internal flag.

            syntaxDefinition.DefineFlag(
                "attachdebugger",
                "Attaches a debugger to the process during startup."
                );

            commandDefinition = syntaxDefinition.DefineCommand("exec", "Executes an application", 1, int.MaxValue);
            commandDefinition.DefineProperty(
                "db", 
                "Specifies the database to run the application in.",
                OptionAttributes.Default,
                new string[] { "d" }
                );

            syntax = syntaxDefinition.CreateSyntax();

            // If no arguments are given, use the syntax to create a Usage
            // message, just as is expected when giving /help or /? to a program.
            if (args.Length == 0) {
                // TODO: Usage(syntax, null);
                arguments = null;
                return false;
            }

            // Parse and evaluate the given input
            parser = new Parser(args);
            try {
                arguments = parser.Parse(syntax);
            } catch (InvalidCommandLineException e) {
                Console.Error.WriteLine(e);
                //Usage(syntax, invalidCommandLine);
                //ReportProgramError(invalidCommandLine.ErrorCode, invalidCommandLine.Message);
                arguments = null;
                return false;
            }

            return true;
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
            try {
                Console.ForegroundColor = color;
                Console.WriteLine(text);
            } finally {
                Console.ResetColor();
            }
        }
    }
}
