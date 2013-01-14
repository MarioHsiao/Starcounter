// ***********************************************************************
// <copyright file="ProgramCommandLine.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter.CommandLine;
using Starcounter.CommandLine.Syntax;
using Starcounter.Internal;

namespace StarcounterInternal.Bootstrap {

    /// <summary>
    /// Contains a set of utility methods responsible for defining, parsing
    /// and handling errors for the command line parameters given to this
    /// program.
    /// </summary>
    public static class ProgramCommandLine {

        /// <summary>
        /// Defines the commands this program accepts.
        /// </summary>
        public static class CommandNames {
            /// <summary>
            /// Defines the name of the Start command.
            /// </summary>
            /// <remarks>
            /// The Start command is the default command and can hence
            /// be omitted on the command line.
            /// </remarks>
            public const string Start = "Start";
        }

        /// <summary>
        /// Defines the options the "Start" command accepts.
        /// </summary>
        public static class OptionNames {
            /// <summary>
            /// Specifies the database directory to use.
            /// </summary>
            public const string DatabaseDir = "DatabaseDir";

            /// <summary>
            /// Specifies the output directory to use.
            /// </summary>
            public const string OutputDir = "OutputDir";

            /// <summary>
            /// Specifies the temporary directory to use.
            /// </summary>
            public const string TempDir = "TempDir";

            /// <summary>
            /// Specifies the path to the compiler to use when generating code.
            /// </summary>
            public const string CompilerPath = "CompilerPath";

            /// <summary>
            /// Specifies the name of Starcounter server which started the database.
            /// </summary>
            public const string ServerName = "ServerName";

            /// <summary>
            /// Specifies the total number of chunks used for shared memory communication.
            /// </summary>
            public const string ChunksNumber = "ChunksNumber";

            /// <summary>
            /// Specifies TCP/IP port to be used by SQL parser.
            /// </summary>
            public const string SQLProcessPort = "SQLProcessPort";

            /// <summary>
            /// Specifies the number of schedulers.
            /// </summary>
            public const string SchedulerCount = "SchedulerCount";

            /// <summary>
            /// Gets the string to use to apply the switch that tells the host process
            /// not to connect to the database nor utilize the SQL engine.
            /// </summary>
            public const string NoDb = "NoDb";

            /// <summary>
            /// Indicates if this host Apps is not utilizing the network gateway.
            /// </summary>
            public const string NoNetworkGateway = "NoNetworkGateway";

            /// <summary>
            /// Gets the string we support as a flag on the command-line to allow
            /// the host process to accept management input on standard streams/console
            /// rather than named pipes (with named pipes being the default).
            /// </summary>
            public const string UseConsole = "UseConsole";

            /// <summary>
            /// Specifies the path to executable that should be run on startup.
            /// </summary>
            public const string AutoStartExePath = "AutoStartExePath";

            /// <summary>
            /// User command line arguments.
            /// </summary>
            public const string UserArguments = "UserArguments";

            /// <summary>
            /// Explicit working directory.
            /// </summary>
            public const string WorkingDir = "WorkingDir";
        }

        /// <summary>
        /// Tries the get program arguments.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <param name="arguments">The arguments.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        internal static bool TryGetProgramArguments(string[] args, out ApplicationArguments arguments) {
            ApplicationSyntaxDefinition syntaxDefinition;
            CommandSyntaxDefinition commandDefinition;
            IApplicationSyntax syntax;
            Parser parser;

            // Define the general program syntax and specify the
            // default command.

            syntaxDefinition = new ApplicationSyntaxDefinition();
            syntaxDefinition.ProgramDescription = "Runs the database user code worker process";
            syntaxDefinition.DefaultCommand = CommandNames.Start;

            // Define the global flag allowing a debugger to be attached
            // to the process when starting. Undocumented, internal flag.

            syntaxDefinition.DefineFlag(
                "attachdebugger",
                "Attaches a debugger to the process during startup."
                );

            // Define the Start command. Exactly one parameter - the database identity - is
            // expected. From this, the minimum command line will be:
            // > prog.exe mydatabase
            // (where we have omitted Start, since its the default).

            commandDefinition = syntaxDefinition.DefineCommand(CommandNames.Start, "Starts the named database", 1);

            // Specifies the property set we accept.
            // A full command line could look like
            // > prog.exe mydatabase --DatabaseDir "C:\MyDatabase" --OutputDir "C:\Out" --TempDir "C:\Temp" --CompilerPath "C:\bin\x86_64-w64-mingw32-gcc.exe"
            // --AutoStartExePath "c:\github\Orange\bin\Debug\NetworkIoTest\NetworkIoTest.exe" --ServerName PERSONAL --ChunksNumber 1024

            commandDefinition.DefineProperty(OptionNames.DatabaseDir, "Specifies the database directory to use.");
            commandDefinition.DefineProperty(OptionNames.OutputDir, "Specifies the output directory to use.");
            commandDefinition.DefineProperty(OptionNames.TempDir, "Specifies the temporary directory to use.");
            commandDefinition.DefineProperty(OptionNames.CompilerPath, "Specifies the path to the compiler to use when generating code.");
            commandDefinition.DefineProperty(OptionNames.ServerName, "Specifies the name of Starcounter server which started the database.");
            commandDefinition.DefineProperty(OptionNames.ChunksNumber, "Specifies the total number of chunks used for shared memory communication.");
            commandDefinition.DefineProperty(OptionNames.AutoStartExePath, "Specifies the path to executable that should be run on startup.");
            commandDefinition.DefineProperty(OptionNames.SQLProcessPort, "Specifies TCP/IP port to be used by " + StarcounterConstants.ProgramNames.ScSqlParser + ".exe.");
            commandDefinition.DefineProperty(OptionNames.SchedulerCount, "Specifies the number of schedulers.");
            commandDefinition.DefineProperty(OptionNames.UserArguments, "User command line arguments.");

            commandDefinition.DefineFlag(OptionNames.NoDb, "Instructs the program not to connect to the database nor use the SQL engine.");
            commandDefinition.DefineFlag(OptionNames.NoNetworkGateway, "Indicates that the host does not need to utilize with network gateway.");
            commandDefinition.DefineFlag(OptionNames.UseConsole, "Instructs the host to use the console to expose management features, like booting executables.");

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

        /// <summary>
        /// Usages the specified syntax.
        /// </summary>
        /// <param name="syntax">The syntax.</param>
        /// <param name="argumentException">The argument exception.</param>
        internal static void Usage(
            IApplicationSyntax syntax,
            InvalidCommandLineException argumentException) {
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
            Console.Write(StarcounterConstants.ProgramNames.ScCode + ".exe");
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
    }
}
