
using staradmin.Commands;
using Starcounter.CLI;
using Starcounter.Internal;
using System;

namespace staradmin {

    class Program {
        static void Main(string[] args) {
            CommandLine.PreParse(ref args);
            StarcounterEnvironment.SetInstallationDirectoryFromEntryAssembly();
            SharedCLI.InitCLIContext(SharedCLI.ClientContext.KnownContexts.StarAdmin);

            try {
                var appArgs = CommandLine.Parse(args);

                var factory = new CommandFactory();
                var command = factory.CreateCommand(appArgs);

                command.Execute();

            } catch (Exception e) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(e.Message);
                Environment.ExitCode = 1;
            } finally {
                Console.ResetColor();
            }
        }
    }
}