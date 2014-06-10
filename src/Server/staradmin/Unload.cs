
using Starcounter.CLI;
using Starcounter.CommandLine;
using Starcounter.CommandLine.Syntax;
using Starcounter.Internal;
using System.Collections.Generic;
using System.IO;

namespace staradmin {

    internal sealed class Unload {
        const string UnloadFileName = "UnloadDatabase.cs";

        string UnloadApplicationName {
            get {
                return "Starcounter_Internal_Unload_Utility";
            }
        }

        /// <summary>
        /// Gets or sets the name of the database to unload.
        /// </summary>
        public string Database { get; set; }

        /// <summary>
        /// Gets or sets the filepath to unload to.
        /// </summary>
        public string FilePath { get; set; }

        public void Run() {
            string exeFile;

            var appFile = Path.Combine(
                StarcounterEnvironment.InstallationDirectory,
                SharedCLI.CLIAppFolderName,
                UnloadFileName
                );
            SourceCodeCompiler.CompileSingleFileToExecutable(appFile, out exeFile);

            var unload = ApplicationCLICommand.Create(appFile, exeFile, CreateArguments()) as StartApplicationCommand;
            unload.JobDescription = string.Format("Unloading {0}", unload.DatabaseName);
            unload.JobCompletionDescription = "done";
            unload.ApplicationStartingDescription = "unloading";

            unload.Execute();
        }

        ApplicationArguments CreateArguments() {
            var syntaxDef = new ApplicationSyntaxDefinition();
            syntaxDef.ProgramDescription = "staradmin.exe Unload";
            syntaxDef.DefaultCommand = "unload";
            SharedCLI.DefineWellKnownOptions(syntaxDef, true);
            syntaxDef.DefineCommand("unload", "Unloads a database");
            var syntax = syntaxDef.CreateSyntax();

            var cmdLine = new List<string>();
            cmdLine.Add(string.Format("--{0}", SharedCLI.Option.NoAutoCreateDb));
            cmdLine.Add(string.Format("--{0}={1}", SharedCLI.Option.AppName, UnloadApplicationName));
            if (!string.IsNullOrEmpty(Database)) {
                cmdLine.Add(string.Format("--{0}={1}", SharedCLI.Option.Db, Database));
            }
            if (!string.IsNullOrEmpty(FilePath)) {
                var file = Path.GetFullPath(FilePath);
                cmdLine.Add(string.Format("{0}", file));
            }

            ApplicationArguments args;
            SharedCLI.TryParse(cmdLine.ToArray(), syntax, out args);
            return args;
        }
    }
}