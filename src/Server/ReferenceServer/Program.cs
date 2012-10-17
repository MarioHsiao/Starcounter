
using Starcounter.ABCIPC.Internal;
using Starcounter.CommandLine;
using Starcounter.CommandLine.Syntax;
using Starcounter.Server.Setup;
using System;
using System.Diagnostics;

namespace Starcounter.Server {

    /// <summary>
    /// Implements a reference server, not intended for production use,
    /// but rather to show how to host the server engine.
    /// </summary>
    class ReferenceServer {
        static ReferenceServer serverProgram;
        
        static void Main(string[] args) {
            serverProgram = new ReferenceServer();
            serverProgram.Run(args);
        }

        void Run(string[] args) {
            ApplicationArguments arguments;

            if (TryGetProgramArguments(args, out arguments)) {

                if (arguments.ContainsFlag("attachdebugger")) {
                    if (!Debugger.IsAttached)
                        try { 
                            Debugger.Launch();
                        } catch { }
                }

                if (arguments.Command.Equals("CreateRepo", StringComparison.InvariantCultureIgnoreCase)) {
                    string repositoryPath = arguments.CommandParameters[0];
                    string serverName;

                    if (!arguments.TryGetProperty("name", out serverName)) {
                        serverName = "Personal";
                    }

                    var setup = RepositorySetup.NewDefault(repositoryPath, serverName);
                    setup.Execute();
                    return;
                }

                // Start is utilized. Bootstrap the server.

                var engine = new ServerEngine(arguments.CommandParameters[0]);
                engine.Setup();

                // Setup the service interface. It can be one of three: either
                // redirected standard streams, local named pipe or the in-process
                // command prompt.

                ServerServices services;
                string pipeName;

                if (arguments.TryGetProperty("Pipe", out pipeName)) {
                    var ipcServer = ClientServerFactory.CreateServerUsingNamedPipes(pipeName);
                    ipcServer.ReceivedRequest += OnIPCServerReceivedRequest;
                    services = new ServerServices(engine, ipcServer);
                    ToConsoleWithColor(string.Format("Accepting service calls on pipe '{0}'...", pipeName), ConsoleColor.DarkGray);
                } else {
                    services = new ServerServices(engine);
                }
                services.Setup();

                // Start the engine and the configured services.
                engine.Start();
                services.Start();
            }
        }

        void OnIPCServerReceivedRequest(object sender, string request) {
            ToConsoleWithColor(string.Format("Request: {0}", request), ConsoleColor.Yellow);
        }

        bool TryGetProgramArguments(string[] args, out ApplicationArguments arguments) {
            ApplicationSyntaxDefinition syntaxDefinition;
            CommandSyntaxDefinition commandDefinition;
            IApplicationSyntax syntax;
            Parser parser;

            // Define the general program syntax

            syntaxDefinition = new ApplicationSyntaxDefinition();
            syntaxDefinition.ProgramDescription = "Runs the Starcounter server.";
            syntaxDefinition.DefaultCommand = "Start";

            // Define the global flag allowing a debugger to be attached
            // to the process when starting. Undocumented, internal flag.

            syntaxDefinition.DefineFlag(
                "attachdebugger",
                "Attaches a debugger to the process during startup."
                );

            // Define the "Start" command, used to start the server. A single, mandatory
            // parameter - the path to the server configuration file - is expected.

            commandDefinition = syntaxDefinition.DefineCommand("Start", "Starts the Starcounter server.", 1);

            // Define the "Pipe" property, allowing the reference server to be run
            // using named pipes.
            commandDefinition.DefineProperty("Pipe", 
                "Allows the reference server to run using named pipes. The value should be the name of the pipe.");

            // Define the "CreateRepo" command, used to create a server repository
            // using built-in defaults. One parameter - the server repository path -
            // is expected.

            commandDefinition = syntaxDefinition.DefineCommand("CreateRepo", "Creates a server repository with default configuration.", 1);

            // Optional property specifying the name of the server. If not given,
            // the default ("Personal") is used.
            commandDefinition.DefineProperty("name", "Specifies the name of the server.");

            // Create the syntax, validating it
            syntax = syntaxDefinition.CreateSyntax();

            // If no arguments are given, use the syntax to create a Usage
            // message, just as is expected when giving /help or /? to a program.
            if (args.Length == 0) {
                Usage(syntax, null);
                arguments = null;
                return false;
            }

            // Parse and evaluate the given input
            parser = new Parser(args);
            try {
                arguments = parser.Parse(syntax);
            } catch (InvalidCommandLineException invalidCommandLine) {
                Usage(syntax, invalidCommandLine);
                arguments = null;
                return false;
            }

            return true;
        }

        void Usage(IApplicationSyntax syntax, InvalidCommandLineException argumentException) {
            // Print a usage message, based on the syntax.
            // If the argument exception given is not null, print a header with
            // the message.

            if (argumentException != null) {
                Console.WriteLine(argumentException.Message);
                Console.WriteLine();
            } else {
                Console.WriteLine(syntax.ProgramDescription);
                Console.WriteLine();
            }

            Console.WriteLine("Usage:");
            Console.WriteLine();
            Console.Write("StarcounterServer.exe");
            foreach (var globalFlag in syntax.Flags) {
                Console.Write(" --FLAG:{0}", globalFlag.Name);
            }

            foreach (var globalProperty in syntax.Properties) {
                Console.Write(" --{0}=(value)", globalProperty.Name);
            }

            for (int i = 0; i < syntax.Commands.Length; i++) {
                var currentCommand = syntax.Commands[i];
                if (i > 0)
                    Console.Write(" or");
                Console.Write(" {0}", currentCommand.Name);
                if (currentCommand.MinParameterCount == 0 && currentCommand.MaxParameterCount == 0) {
                    // No parameters
                } else if (currentCommand.MinParameterCount == currentCommand.MaxParameterCount) {
                    Console.Write(" ({0} parameter(s))", currentCommand.MinParameterCount);
                } else {
                    Console.Write(" ({0}-{1} parameters)", currentCommand.MinParameterCount, currentCommand.MaxParameterCount);
                }

                foreach (var flag in currentCommand.Flags) {
                    Console.Write(" --FLAG:{0}", flag.Name);
                }

                foreach (var property in currentCommand.Properties) {
                    Console.Write(" --{0}=(value)", property.Name);
                }
            }

            Console.WriteLine();
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