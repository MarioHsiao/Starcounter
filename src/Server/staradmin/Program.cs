
using staradmin.Commands;
using Starcounter;
using Starcounter.CLI;
using Starcounter.Internal;
using Starcounter.Server;
using System;

namespace staradmin {

    class Program {
        static void Main(string[] args) {
            CommandLine.PreParse(ref args);
            StarcounterEnvironment.SetInstallationDirectoryFromEntryAssembly();
            SharedCLI.InitCLIContext(KnownClientContexts.StarAdmin);

            try {
                var appArgs = CommandLine.Parse(args);

                var factory = new CommandFactory();
                var command = factory.CreateCommand(appArgs);

                command.Execute();

            } catch (Exception e) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(e.Message);
                
                uint exitCode;
                if (!ErrorCode.TryGetCode(e, out exitCode)) {
                    exitCode = Error.SCERRUNSPECIFIED;
                }
                Environment.ExitCode = (int)exitCode;

            } finally {
                Console.ResetColor();
            }
        }
    }
}