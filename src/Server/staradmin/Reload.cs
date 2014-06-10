
using Starcounter.CLI;
using Starcounter.CommandLine;
using Starcounter.CommandLine.Syntax;
using Starcounter.Internal;
using System.Collections.Generic;
using System.IO;

namespace staradmin {

    internal sealed class Reload {
        const string ReloadFileName = "ReloadDatabase.cs";

        string ReloadApplicationName {
            get {
                return "Starcounter_Internal_Reload_Utility";
            }
        }

        /// <summary>
        /// Gets or sets the name of the database to reload
        /// into.
        /// </summary>
        public string Database { get; set; }

        /// <summary>
        /// Gets or sets the filepath to reload from.
        /// </summary>
        public string FilePath { get; set; }

        public void Run() {
            string exeFile;

            var appFile = Path.Combine(
                StarcounterEnvironment.InstallationDirectory,
                SharedCLI.CLIAppFolderName,
                ReloadFileName
                );
            SourceCodeCompiler.CompileSingleFileToExecutable(appFile, out exeFile);

            var reload = ApplicationCLICommand.Create(appFile, exeFile, CreateArguments()) as StartApplicationCommand;
            reload.JobDescription = string.Format("Reloading {0}", reload.DatabaseName);
            reload.JobCompletionDescription = "done";
            reload.ApplicationStartingDescription = "reloading";

            reload.Execute();
        }

        ApplicationArguments CreateArguments() {
            var syntaxDef = new ApplicationSyntaxDefinition();
            syntaxDef.ProgramDescription = "staradmin.exe Reload";
            syntaxDef.DefaultCommand = "reload";
            SharedCLI.DefineWellKnownOptions(syntaxDef, true);
            syntaxDef.DefineCommand("reload", "Reloads a database");
            var syntax = syntaxDef.CreateSyntax();

            var cmdLine = new List<string>();
            cmdLine.Add(string.Format("--{0}={1}", SharedCLI.Option.AppName, ReloadApplicationName));
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