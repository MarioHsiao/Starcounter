
using Sc.Server.Weaver.Schema;
using Starcounter.CommandLine;
using Starcounter.CommandLine.Syntax;
using Starcounter.Internal;
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Starcounter.Weaver
{
    using Error = Starcounter.Error;

    class Program {

        /// <summary>
        /// The name of the default output directory, utilized by the weaver
        /// when no output directory is explicitly given.
        /// </summary>
        /// <remarks>
        /// If no output directory is given, the output directory will be a
        /// subdirectory of the input directory, with this name.
        /// </remarks>
        const string DefaultOutputDirectoryName = ".starcounter";

        /// <summary>
        /// The name of the default cache directory, if no cache directory
        /// is given.
        /// </summary>
        /// <remarks>
        /// If no cache directory is given, the cache directory will be a
        /// subdirectory of the output directory, with this name.
        /// </remarks>
        const string DefaultCacheDirectoryName = "cache";

        static void Main(string[] args) {
            StarcounterEnvironment.SetInstallationDirectoryFromEntryAssembly();

            var host = new WeaverHost();

            ApplicationArguments arguments;
            try {
                if (TryGetProgramArguments(host, args, out arguments))
                    ApplyOptionsAndExecuteGivenCommand(host, arguments);
            }
            finally {
                Console.ResetColor();
            }
        }

        static void ApplyOptionsAndExecuteGivenCommand(WeaverHost host, ApplicationArguments arguments) {
            string inputDirectory;
            string cacheDirectory;
            string outputDirectory;
            uint error;
            string fileName;
            string givenFilePath;
            
            ApplyGlobalProgramOptions(host, arguments);

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
                    host.WriteError(error, ErrorCode.ToMessage(error, string.Format("Path: {0}.", givenFilePath)));
                    return;
                }
            } catch (Exception e) {
                error = Error.SCERRWEAVERFILENOTFOUND;
                host.WriteError(
                    error,
                    ErrorCode.ToMessage(error, string.Format("Failed resolving path: {0}, Path: {1}.", e.Message, givenFilePath)));
                return;
            }

            var supportedExtensions = new string[] { ".exe", ".dll" };
            var extension = Path.GetExtension(givenFilePath).ToLower();

            if (!supportedExtensions.Contains<string>(extension)) {
                error = Error.SCERRWEAVERFILENOTSUPPORTED;
                host.WriteError(
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
                outputDirectory = Path.Combine(inputDirectory, DefaultOutputDirectoryName);
            } else {
                outputDirectory = Path.Combine(inputDirectory, outputDirectory);
            }

            if (!Directory.Exists(outputDirectory)) {
                try {
                    Directory.CreateDirectory(outputDirectory);
                } catch (Exception e) {
                    error = Error.SCERRCODELIBFAILEDNEWCACHEDIR;
                    host.WriteError(
                        error,
                        ErrorCode.ToMessage(error, string.Format("Message: {0}.", e.Message)));
                    return;
                }
            }

            // Resolve a cache directory to use, either from the given input
            // or from using the default directory.

            if (!arguments.TryGetProperty("cachedir", out cacheDirectory)) {
                cacheDirectory = Path.Combine(outputDirectory, DefaultCacheDirectoryName);
            } else {
                cacheDirectory = Path.Combine(outputDirectory, cacheDirectory);
            }

            if (!Directory.Exists(cacheDirectory)) {
                try {
                    Directory.CreateDirectory(cacheDirectory);
                } catch (Exception e) {
                    error = Error.SCERRCODELIBFAILEDNEWCACHEDIR;
                    host.WriteError(
                        error,
                        ErrorCode.ToMessage(error, string.Format("Message: {0}.", e.Message)));
                    return;
                }
            }

            // Decide what command to run and invoke the proper method.

            var cmd = arguments.Command;
            var caseInsensitive = StringComparison.InvariantCultureIgnoreCase;
            
            if (cmd.Equals(ProgramCommands.Weave, caseInsensitive)) {
                ExecuteWeaveCommand(host, inputDirectory, outputDirectory, cacheDirectory, fileName, arguments);

            } else if (cmd.Equals(ProgramCommands.Verify, caseInsensitive)) {
                ExecuteVerifyCommand(host, inputDirectory, outputDirectory, cacheDirectory, fileName, arguments);

            } else if (cmd.Equals(ProgramCommands.ShowSchema, caseInsensitive)) {
                ExecuteSchemaCommand(inputDirectory, outputDirectory, cacheDirectory, fileName, arguments);

            } else if (cmd.Equals(ProgramCommands.Test, caseInsensitive)) {
                ExecuteTestCommand(inputDirectory, outputDirectory, cacheDirectory, fileName, arguments);

            } else {
                error = Error.SCERRBADCOMMANDLINESYNTAX;
                host.WriteError(
                    error,
                    ErrorCode.ToMessage(error, string.Format("Command {0} not recognized.", arguments.Command))
                    );
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
            WeaverHost host,
            string inputDirectory,
            string outputDirectory,
            string cacheDirectory,
            string fileName,
            ApplicationArguments arguments) {

            var setup = new WeaverSetup()
            {
                InputDirectory = inputDirectory,
                AssemblyFile = fileName,
                OutputDirectory = outputDirectory,
                CacheDirectory = cacheDirectory
            };

            setup.DisableWeaverCache = arguments.ContainsFlag("nocache");
            setup.WeaveToCacheOnly = arguments.ContainsFlag("tocache");
            setup.UseStateRedirect = arguments.ContainsFlag("UseStateRedirect".ToLower());
            setup.DisableEditionLibraries = arguments.ContainsFlag("DisableEditionLibraries".ToLower());
            setup.EmitBootAndFinalizationDiagnostics = host.OutputVerbosity == Verbosity.Diagnostic;
            setup.IncludeLocationInErrorMessages = host.ShouldCreateParceledErrors;
            setup.EnableTracing = host.OutputVerbosity == Verbosity.Diagnostic;

            var weaver = WeaverFactory.CreateWeaver(setup, host);
            weaver.Execute();
        }

        /// <summary>
        /// Executes the command "Verify".
        /// </summary>
        /// <param name="inputDirectory">The input directory.</param>
        /// <param name="outputDirectory">The output directory.</param>
        /// <param name="cacheDirectory">The cache directory.</param>
        /// <param name="fileName">The name of the file to give the weaver.</param>
        /// <param name="arguments">Parsed and verified program arguments.</param>
        static void ExecuteVerifyCommand(
            WeaverHost host,
            string inputDirectory,
            string outputDirectory,
            string cacheDirectory,
            string fileName,
            ApplicationArguments arguments) {

            var setup = new WeaverSetup()
            {
                InputDirectory = inputDirectory,
                AssemblyFile = fileName,
                OutputDirectory = outputDirectory,
                CacheDirectory = cacheDirectory
            };
            setup.AnalyzeOnly = true;
            setup.DisableWeaverCache = arguments.ContainsFlag("nocache");

            var weaver = WeaverFactory.CreateWeaver(setup, host);
            weaver.Execute();
        }

        static void ExecuteSchemaCommand(
            string inputDirectory,
            string outputDirectory,
            string cacheDirectory,
            string fileName,
            ApplicationArguments arguments) {

            var schema = DatabaseSchema.DeserializeFrom(new DirectoryInfo(outputDirectory));
            schema.DebugOutput(new IndentedTextWriter(Console.Out));
        }

        static void ExecuteTestCommand(
            string inputDirectory,
            string outputDirectory,
            string cacheDirectory,
            string fileName,
            ApplicationArguments arguments) {
            
            var exe = Path.Combine(inputDirectory, fileName);
            var loaded = Assembly.LoadFrom(exe);
            var ep = loaded.EntryPoint;
            if (ep == null) {
                throw ErrorCode.ToException(Error.SCERRBADARGUMENTS, string.Format("{0} cant be used as a test - it defines no entrypoint", exe));
            }

            if (ep.GetParameters().Length == 0) {
                ep.Invoke(null, null);
            } else {
                var args = new string[] { "WEAVERTEST", inputDirectory, fileName, outputDirectory };
                ep.Invoke(null, new object[] { args });
            }
        }

        // Rename to ApplyHostOptions or ConfigureHost
        // TODO:

        static void ApplyGlobalProgramOptions(WeaverHost host, ApplicationArguments arguments) {
            string propertyValue;

            // Consult global/shared parameters and apply them as specified by
            // their specification.

            if (arguments.ContainsFlag("sc-debug")) {
                if (Debugger.IsAttached)
                    host.WriteDebug("A debugger is already attached to the process.");
                else {
                    bool debuggerAttached;
                    try {
                        debuggerAttached = Debugger.Launch();
                    } catch {
                        debuggerAttached = false;
                    }

                    if (!debuggerAttached)
                        host.WriteWarning("Unable to attach a debugger.");
                }
            }

            if (arguments.TryGetProperty("verbosity", out propertyValue)) {
                switch (propertyValue.ToLowerInvariant()) {
                    case "quiet":
                        host.OutputVerbosity = Verbosity.Quiet;
                        break;

                    case "verbose":
                        host.OutputVerbosity = Verbosity.Verbose;
                        break;

                    case "diagnostic":
                        host.OutputVerbosity = Verbosity.Diagnostic;
                        break;

                    default:
                        throw ErrorCode.ToException(Error.SCERRBADCOMMANDLINESYNTAX, string.Format("Unknown verbosity: {0}", propertyValue));
                }
            }
            
            if (arguments.TryGetProperty("errorparcelid", out propertyValue)) {
                host.ErrorParcelID = propertyValue;
            }

            if (arguments.TryGetProperty("maxerrors", out propertyValue)) {
                int maxErrors = 0;
                try {
                    maxErrors = int.Parse(propertyValue);
                    if (maxErrors < 0) maxErrors = 0;
                } catch { }
                host.MaxErrors = maxErrors;
            }
        }

        static bool TryGetProgramArguments(WeaverHost host, string[] args, out ApplicationArguments arguments) {
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
                "sc-debug",
                "Attaches a debugger to the process during startup."
                );

            // Define a property that allows instructing the weaver to
            // exit after a specified number of errors have occured. If
            // not set, or if the value is 0 or negative, the weaver will
            // run until all errors have been detected.

            syntaxDefinition.DefineProperty(
                "MaxErrors".ToLowerInvariant(),
                "Specifies the maximum number of errors the weaver should detect before exiting."
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

            commandDefinition.DefineFlag("DisableEditionLibraries".ToLower(),
                "Instructs the weaver to ignore any edition libraries part of the installation.");

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

            // Display schema command
            syntaxDefinition.DefineCommand(ProgramCommands.ShowSchema, "Displays the schema of the given application", 1);

            // Treats the given application as an application that are to be
            // tested against a weaved version of itself; invokes the entrypoint
            // with a certain set of arguments
            syntaxDefinition.DefineCommand(
                ProgramCommands.Test, "Runs the given application as a test application", 1);

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
                host.WriteError(invalidCommandLine.ErrorCode, invalidCommandLine.Message);
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
    }
}
