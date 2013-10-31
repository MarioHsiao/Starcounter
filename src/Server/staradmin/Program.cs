
using Starcounter;
using Starcounter.Internal;
using System;

namespace staradmin {

    class Program {
        static void Main(string[] args) {
            try {
                string command = args.Length > 0 ? args[0] : string.Empty;
                command = command.ToLowerInvariant();
                command = command.TrimStart('-', '/');
                switch (command) {
                    case "killall":
                        ProcessUtilities.KillAllScProcesses();
                        break;
                    case "installservice":
                        SystemServiceInstall.Install();
                        break;
                    case "uninstallservice":
                        SystemServiceInstall.Uninstall();
                        break;
                    default:
                        throw ErrorCode.ToException(Error.SCERRNOTIMPLEMENTED);
                }

            } catch(Exception e) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(e.Message);
                Environment.ExitCode = 1;
            } finally {
                Console.ResetColor();
            }
        }
    }
}