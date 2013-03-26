
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter;
using Starcounter.CommandLine;
using Starcounter.CommandLine.Syntax;
using System.IO;
using System.Diagnostics;

namespace Weaver {

    class Program {

        static Verbosity OutputVerbosity = Verbosity.Default;
        static string ErrorParcelID = string.Empty;

        internal static bool IsCreatingParceledErrors {
            get { return !string.IsNullOrEmpty(Program.ErrorParcelID); }
        }

        static void Main(string[] args) {
            ApplicationArguments arguments;

            try {
                if (TryGetProgramArguments(args, out arguments))
                    ApplyOptionsAndExecuteGivenCommand(arguments);
            } finally {
                Console.ResetColor();
            }
        }

        static void ApplyOptionsAndExecuteGivenCommand(ApplicationArguments arguments) {
            string inputDirectory;
            string cacheDirectory;
            string outputDirectory;
            uint error;
            string fileName;
            string givenFilePath;
            
            ApplyGlobalProgramOptions(arguments);

            // All commands share the requirement to specify the executable as the first
            // parameter. All commands also support specifying the cache directory. Set
            // them up here.

            givenFilePath = arguments.CommandParameters[0];
            inputDirectory = null;
            outputDirectory = null;
            fileName = null;

            try {
                givenFilePath = Path.GetFullPath(givenFilePath);

                if (!File.Exists(givenFilePath)) {
                    error = Error.SCERRWEAVERFILENOTFOUND;
                    ReportProgramError(
                        error,
                        ErrorCode.ToMessage(error, string.Format("Path: {0}.", givenFilePath)));
                    return;
                }
            } catch (Exception e) {
                error = Error.SCERRWEAVERFILENOTFOUND;
                ReportProgramError(
                    error,
                    ErrorCode.ToMessage(error, string.Format("Failed resolving path: {0}, Path: {1}.", e.Message, givenFilePath)));
                return;
            }

            var supportedExtensions = new string[] { ".exe", ".dll" };
            var extension = Path.GetExtension(givenFilePath).ToLower();

            if (!supportedExtensions.Contains<string>(extension)) {
                error = Error.SCERRWEAVERFILENOTSUPPORTED;
                ReportProgramError(
                    error,
                    ErrorCode.ToMessage(error, string.Format("Supported extensions: {0}.", string.Join(",", supportedExtensions)))
                    );
                return;
            }

            fileName = Path.GetFileName(givenFilePath);
            inputDirectory = Path.GetDirectoryName(givenFilePath);
            inputDirectory = inputDirectory.Trim('"');

            // Resolve the output directory to use when transforming/weaving.

            if (!arguments.TryGetProperty("outdir", out outputDirectory)) {
                outputDirectory = Path.Combine(inputDirectory, CodeWeaver.DefaultOutputDirectoryName);
            } else {
                outputDirectory = Path.Combine(inputDirectory, outputDirectory);
            }

            if (!Directory.Exists(outputDirectory)) {
                try {
                    Directory.CreateDirectory(outputDirectory);
                } catch (Exception e) {
                    error = Error.SCERRCODELIBFAILEDNEWCACHEDIR;
                    ReportProgramError(
                        error,
                        ErrorCode.ToMessage(error, string.Format("Message: {0}.", e.Message)));
                    return;
                }
            }

            // Resolve a cache directory to use, either from the given input
            // or from using the default directory.

            if (!arguments.TryGetProperty("cachedir", out cacheDirectory)) {
                cacheDirectory = Path.Combine(outputDirectory, CodeWeaver.DefaultCacheDirectoryName);
            } else {
                cacheDirectory = Path.Combine(outputDirectory, cacheDirectory);
            }

            if (!Directory.Exists(cacheDirectory)) {
                try {
                    Directory.CreateDirectory(cacheDirectory);
                } catch (Exception e) {
                    error = Error.SCERRCODELIBFAILEDNEWCACHEDIR;
                    ReportProgramError(
                        error,
                        ErrorCode.ToMessage(error, string.Format("Message: {0}.", e.Message)));
                    return;
                }
            }

            // Decide what command to run and invoke the proper method.

            switch (arguments.Command) {

                case ProgramCommands.Weave:
                    ExecuteWeaveCommand(inputDirectory, outputDirectory, cacheDirectory, fileName, arguments);
                    break;

                case ProgramCommands.Verify:
                    ExecuteVerifyCommand(inputDirectory, cacheDirectory, fileName, arguments);
                    break;

                case ProgramCommands.WeaveBootstrapper:
                    ExecuteWeaveBootstrapCommand(inputDirectory, cacheDirectory, fileName, arguments);
                    break;

                default:
                    error = Error.SCERRBADCOMMANDLINESYNTAX;
                    ReportProgramError(
                        error,
                        ErrorCode.ToMessage(error, string.Format("Command {0} not recognized.", arguments.Command))
                        );
                    break;
            }
        }

        /// <summary>
        /// Executes the command "Weave".
        /// </summary>
        /// <param name="inputDirectory">The input directory.</param>
        /// <param name="outputDirectory">The output directory.</param>
        /// <param name="cacheDirectory">The cache directory.</param>
        /// <param name="fileName">The name of the file to give the weaver.</param>
        /// <param name="arguments">Parsed and verified program arguments.</param>
        static void ExecuteWeaveCommand(
            string inputDirectory,
            string outputDirectory,
            string cacheDirectory,
            string fileName,
            ApplicationArguments arguments) {
            CodeWeaver weaver;

            // Create the code weaver facade and configure it properly. Then
            // execute the underlying weaver engine.

            weaver = new CodeWeaver(inputDirectory, fileName, outputDirectory, cacheDirectory);
            weaver.OutputDirectory = outputDirectory;
            weaver.RunWeaver = true;
            weaver.WeaveForIPC = true;//!arguments.ContainsFlag("noipc");
            weaver.DisableWeaverCache = arguments.ContainsFlag("nocache");
            weaver.WeaveToCacheOnly = arguments.ContainsFlag("tocache");
            weaver.UseStateRedirect = arguments.ContainsFlag("UseStateRedirect".ToLower());

            // Invoke the weaver subsystem. If it fails, it will report the
            // error itself.

            weaver.Execute();
        }

        /// <summary>
        /// Executes the command "Verify".
        /// </summary>
        /// <param name="inputDirectory">The input directory.</param>
        /// <param name="cacheDirectory">The cache directory.</param>
        /// <param name="fileName">The name of the file to give the weaver.</param>
        /// <param name="arguments">Parsed and verified program arguments.</param>
        static void ExecuteVerifyCommand(
            string inputDirectory,
            string cacheDirectory,
            string fileName,
            ApplicationArguments arguments) {
            CodeWeaver weaver;
            
            // Create the code weaver facade and configure it properly. Then
            // execute the underlying weaver engine.

            weaver = new CodeWeaver(inputDirectory, fileName, null, cacheDirectory);
            weaver.RunWeaver = false;
            weaver.WeaveForIPC = true;
            weaver.DisableWeaverCache = arguments.ContainsFlag("nocache");

            // Invoke the weaver subsystem. If it fails, it will report the
            // error itself.

            weaver.Execute();
        }

        /// <summary>
        /// Weaves an executable to support bootstraping it from the OS shell.
        /// </summary>
        /// <param name="inputDirectory">
        /// The directory where we expect to find the executable.</param>
        /// <param name="cacheDirectory">The cache directory.</param>
        /// <param name="fileName">The name of the executable file.</param>
        /// <param name="arguments">Arguments to the command, parsed from the
        /// command-line.</param>
        static void ExecuteWeaveBootstrapCommand(
            string inputDirectory,
            string cacheDirectory,
            string fileName,
            ApplicationArguments arguments) {
            
            BootstrapWeaver.WeaveExecutable(Path.Combine(inputDirectory, fileName));

#if false
            // Implementation using the PostSharp-based weaver
            CodeWeaver weaver;
            weaver = new CodeWeaver(inputDirectory, fileName, cacheDirectory);
            weaver.RunWeaver = true;
            weaver.WeaveBootstrapperCode = true;
            weaver.WeaveForIPC = true;//!arguments.ContainsFlag("noipc");
            weaver.DisableWeaverCache = arguments.ContainsFlag("nocache");
            weaver.WeaveToCacheOnly = false;

            // Invoke the weaver subsystem. If it fails, it will report the
            // error itself.

            weaver.Execute();

#endif
        }

        static void ApplyGlobalProgramOptions(ApplicationArguments arguments) {
            string propertyValue;

            // Consult global/shared parameters and apply them as specified by
            // their specification.

            if (arguments.ContainsFlag("attachdebugger")) {
                if (Debugger.IsAttached)
                    WriteDebug("A debugger is already attached to the process.");
                else {
                    bool debuggerAttached;
                    try {
                        debuggerAttached = Debugger.Launch();
                    } catch {
                        debuggerAttached = false;
                    }

                    if (!debuggerAttached)
                        WriteWarning("Unable to attach a debugger.");
                }
            }

            if (arguments.TryGetProperty("verbosity", out propertyValue)) {
                switch (propertyValue.ToLowerInvariant()) {
                    case "quiet":
                        Program.OutputVerbosity = Verbosity.Quiet;
                        break;

                    case "verbose":
                        Program.OutputVerbosity = Verbosity.Verbose;
                        break;

                    case "diagnostic":
                        Program.OutputVerbosity = Verbosity.Diagnostic;
                        break;

                    default:
                        // Don't change the default verbosity, initialized
                        // statically.
                        break;
                }
            }

            if (arguments.TryGetProperty("errorparcelid", out propertyValue)) {
                Program.ErrorParcelID = propertyValue;
            }
        }

        static bool TryGetProgramArguments(string[] args, out ApplicationArguments arguments) {
            ApplicationSyntaxDefinition syntaxDefinition;
            CommandSyntaxDefinition commandDefinition;
            IApplicationSyntax syntax;
            Parser parser;

            // Define the general program syntax

            syntaxDefinition = new ApplicationSyntaxDefinition();
            syntaxDefinition.ProgramDescription = "Weaves a given executable and it's dependencies.";
            syntaxDefinition.DefaultCommand = ProgramCommands.Weave;

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

            // Define the global flag allowing callers to specify that all
            // errors written to standard error output should be wrapped in
            // a given parcel ID. This feature is intended for outer tools
            // only, not for end users.

            syntaxDefinition.DefineProperty(
                "errorparcelid",
                "Specifies an id that should be used when errors are written to wrap them in parcels."
                );

            // Define the "WEAVE" command, used to weave assemblies, but omit creating
            // a code library archive.

            // Define the command. Exactly one parameter - the executable - is
            // expected.
            commandDefinition = syntaxDefinition.DefineCommand(ProgramCommands.Weave, "Weaves user code.", 1);

            // Optional specification of the output directory to use. If no output
            // directory is explicitly given, a default output directory will be used,
            // based on the input directory.
            commandDefinition.DefineProperty("outdir", "Specifies the output directory to use.");

            // Optional specification of the cache directory to use. The cache is used
            // to cache assembly analysis results and possibly transformed/weaved binaries.
            // If no cache directory is given, a default cache directory will be used.
            commandDefinition.DefineProperty("cachedir", "Specifies the cache directory to use.");

            // Optional flag instructing the program not to weave the code for IPC. If
            // this flag is set, the code will be weaved as it would be weaved inside the
            // database, only nonsense random field indexes will be used. This can be
            // usefull to diagnose or test the database weaver.
            //
            // This flag is no longer supported in PeeDee, at least not now. The only
            // valid option is to weave "no ipc", i.e. doing no Lucent Objects stuff,
            // and hence it seems logical to disable/hide this flag for now.
            // commandDefinition.DefineFlag("noipc", "Instructs the weaver not to weave the code for IPC usage.");

            // Optional flag instructing the program not to use the weaver cache when
            // analyzing/weaving code. The effect of this flag is that any up-to-date
            // cached content will always be recreated, i.e. weaving will always occur.
            commandDefinition.DefineFlag("nocache", "Instructs the weaver not to use the weaver cache.");

            commandDefinition.DefineFlag("UseStateRedirect".ToLower(),
                "Instructs the weaver to weave against a slower, redirected database state layer.");

            // Allows the weave command to be ran just weaving to the cache, not touching
            // the input.
            commandDefinition.DefineFlag(
                "tocache", 
                "Instructs the weaver to leave the input intact and weave only to the weaver cache.");

            // Define the "Verify" command, used to analyze and verify user code.

            // Define the command. Exactly one parameter - the executable - is
            // expected.
            commandDefinition = syntaxDefinition.DefineCommand(ProgramCommands.Verify, "Verifies user code.", 1);

            // Optional specification of the cache directory to use. The cache is used
            // to cache assembly analysis results. If no cache directory is given, a
            // default cache directory will be used.
            commandDefinition.DefineProperty("cachedir", "Specifies the cache directory to use.");

            // Optional flag instructing the program not to use the weaver cache when
            // analyzing/weaving code.
            commandDefinition.DefineFlag("nocache", "Instructs the weaver not to use the weaver cache.");

            // Define the "WeaveBootstrapper" command, used to analyze and verify user code.

            // Define the command. Exactly one parameter - the executable - is
            // expected.
            commandDefinition = syntaxDefinition.DefineCommand(
                ProgramCommands.WeaveBootstrapper, "Enables the executable to be bootstraped in Starcounter from the OS shell.", 1);

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
                ReportProgramError(invalidCommandLine.ErrorCode, invalidCommandLine.Message);
                arguments = null;
                return false;
            }

            return true;
        }

        static void Usage(
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
            Console.Write(Starcounter.Internal.StarcounterConstants.ProgramNames.ScWeaver + ".exe");
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

        /// <summary>
        /// Sets the environment exit code to the given Starcounter error
        /// code, after first writing its message to the console.
        /// </summary>
        /// <param name="errorCode">The code we should report as the exit
        /// code of the program.</param>
        /// <returns>True if the code was set as the exit code. False if
        /// a previous exit code was already set. In this case, only the
        /// error message is written to the console.</returns>
        internal static bool ReportProgramError(uint errorCode) {
            return ReportProgramError(errorCode, ErrorCode.ToMessage(errorCode));
        }

        /// <summary>
        /// Sets the environment exit code to the given Starcounter error
        /// code, after first writing the supplied message to the console.
        /// </summary>
        /// <param name="errorCode">The code we should report as the exit
        /// code of the program.</param>
        /// <param name="message">Error message that should be written to
        /// the console.</param>
        /// <param name="parameters">Possible message arguments.</param>
        /// <returns>True if the code was set as the exit code. False if
        /// a previous exit code was already set. In this case, only the
        /// error message is written to the console.</returns>
        internal static bool ReportProgramError(
            uint errorCode,
            string message,
            params object[] parameters) {
            WriteError(message, parameters);
            if (Environment.ExitCode == 0) {
                Environment.ExitCode = (int)errorCode;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Writes a debug message to the console, if the verbosity level of
        /// the program is letting it through.
        /// </summary>
        /// <param name="message">Message to write.</param>
        /// <param name="parameters">Possible message arguments.</param>
        internal static void WriteDebug(string message, params object[] parameters) {
            if (Program.OutputVerbosity < Verbosity.Diagnostic)
                return;

            WriteWithColor(Console.Out, ConsoleColor.DarkGray, message, parameters);
        }

        /// <summary>
        /// Writes an information message to the console, if the verbosity level of
        /// the program is letting it through.
        /// </summary>
        /// <param name="message">Message to write.</param>
        /// <param name="parameters">Possible message arguments.</param>
        internal static void WriteInformation(string message, params object[] parameters) {
            if (Program.OutputVerbosity < Verbosity.Verbose)
                return;

            WriteWithColor(Console.Out, ConsoleColor.White, message, parameters);
        }

        /// <summary>
        /// Writes a warning message to the console, if the verbosity level of
        /// the program is letting it through.
        /// </summary>
        /// <param name="message">Message to write.</param>
        /// <param name="parameters">Possible message arguments.</param>
        internal static void WriteWarning(string message, params object[] parameters) {
            if (Program.OutputVerbosity < Verbosity.Minimal)
                return;

            WriteWithColor(Console.Out, ConsoleColor.Yellow, message, parameters);
        }

        /// <summary>
        /// Writes a error message to the console, if the verbosity level of
        /// the program is letting it through.
        /// </summary>
        /// <param name="message">Message to write.</param>
        /// <param name="parameters">Possible message arguments.</param>
        internal static void WriteError(string message, params object[] parameters) {
            if (Program.OutputVerbosity < Verbosity.Minimal)
                return;

            if (!string.IsNullOrEmpty(Program.ErrorParcelID)) {
                WriteParcel(Program.ErrorParcelID, Console.Error, message, parameters);
            }

            WriteWithColor(Console.Error, ConsoleColor.Red, message, parameters);
        }

        private static void WriteParcel(
            string parcelID,
            TextWriter stream,
            string message,
            params object[] parameters) {
            message = string.Format("{0}{1}{2}", parcelID, message, parcelID);
            WriteWithColor(
                stream,
                ConsoleColor.DarkBlue,
                message,
                parameters
                );
        }

        /// <summary>
        /// Writes a message to the console, to the specific stream, using
        /// a specified color.
        /// </summary>
        /// <param name="stream">The console stream to write to.</param>
        /// <param name="color">The color to use.</param>
        /// <param name="message">Message to write.</param>
        /// <param name="parameters">Possible message arguments.</param>
        private static void WriteWithColor(TextWriter stream, ConsoleColor color, string message, params object[] parameters) {
            Console.ForegroundColor = color;
            try {
                stream.WriteLine(message, parameters);
            } finally {
                Console.ResetColor();
            }
        }
    }
}
