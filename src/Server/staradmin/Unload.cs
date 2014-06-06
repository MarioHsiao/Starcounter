
using Starcounter.CLI;
using Starcounter.CommandLine;
using Starcounter.Internal;
using System.IO;

namespace staradmin {

    internal sealed class Unload {
        const string UnloadFileName = "UnloadDatabase.cs";

        public void Run() {
            string exeFile;

            var appFile = Path.Combine(
                StarcounterEnvironment.InstallationDirectory,
                SharedCLI.CLIAppFolderName,
                UnloadFileName
                );
            SourceCodeCompiler.CompileSingleFileToExecutable(appFile, out exeFile);

            var unload = ApplicationCLICommand.Create(appFile, exeFile, ApplicationArguments.Empty) as StartApplicationCommand;
            unload.JobDescription = string.Format("Unloading {0}", unload.DatabaseName);
            unload.JobCompletionDescription = "done";
            unload.ApplicationStartingDescription = "unloading";

            unload.Execute();
        }
    }
}