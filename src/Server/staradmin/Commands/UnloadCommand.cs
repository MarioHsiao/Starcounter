
using Starcounter.CLI;
using Starcounter.CommandLine;
using Starcounter.CommandLine.Syntax;
using System;
using System.Collections.Generic;
using System.IO;

namespace staradmin.Commands {

    internal class UnloadCommand : ContextAwareCommand {
        class Sources {
            public const string Database = "db";
        }

        readonly string source;

        public UnloadCommand(string dataSource = null) {
            source = string.IsNullOrEmpty(dataSource) ? Sources.Database : dataSource;
        }

        /// <inheritdoc />
        public override void Execute() {
            switch (source.ToLowerInvariant()) {
                case Sources.Database:
                    UnloadDatabase();
                    break;
                default:
                    ReportUnrecognizedSource();
                    break;
            }
        }

        void UnloadDatabase() {
            var unloadSourceCodeFile = "UnloadDatabase.cs";
            string exeFile;

            var appFile = Path.Combine(
                Context.InstallationDirectory,
                SharedCLI.CLIAppFolderName,
                unloadSourceCodeFile
                );
            SourceCodeCompiler.CompileSingleFileToExecutable(appFile, out exeFile);

            var unload = ApplicationCLICommand.Create(appFile, exeFile, CreateUnloadApplicationArguments()) as StartApplicationCommand;
            unload.JobDescription = string.Format("Unloading {0}", unload.DatabaseName);
            unload.JobCompletionDescription = "done";
            unload.ApplicationStartingDescription = "unloading";

            unload.Execute();
        }

        ApplicationArguments CreateUnloadApplicationArguments() {
            var appName = "Starcounter_Internal_Unload_Utility";

            var syntaxDef = new ApplicationSyntaxDefinition();
            syntaxDef.ProgramDescription = "staradmin.exe Unload";
            syntaxDef.DefaultCommand = "unload";
            SharedCLI.DefineWellKnownOptions(syntaxDef, true);
            syntaxDef.DefineCommand("unload", "Unloads a database");
            var syntax = syntaxDef.CreateSyntax();

            var cmdLine = new List<string>();
            cmdLine.Add(string.Format("--{0}", SharedCLI.Option.NoAutoCreateDb));
            cmdLine.Add(string.Format("--{0}={1}", SharedCLI.Option.AppName, appName));
            if (!string.IsNullOrEmpty(Context.Database)) {
                cmdLine.Add(string.Format("--{0}={1}", SharedCLI.Option.Db, Context.Database));
            }

            string file;
            if (Context.TryGetCommandProperty("file", out file)) {
                file = Path.GetFullPath(file);
                cmdLine.Add(string.Format("{0}", file));
            }
            
            ApplicationArguments args;
            SharedCLI.TryParse(cmdLine.ToArray(), syntax, out args);
            return args;
        }

        void ReportUnrecognizedSource() {
            var helpOnUnload = new ShowHelpCommand(CommandLine.Commands.Unload.Name) { SupressHeader = true };
            var badInput = new ReportBadInputCommand(string.Format("Don't know how to unload '{0}'.", source), helpOnUnload);
            badInput.Execute();
        }
    }
}
