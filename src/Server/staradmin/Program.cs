
using Starcounter;
using Starcounter.Internal;
using System;

namespace staradmin {

    class Program {
        static void Main(string[] args) {
            try {
                string command = args.Length > 0 ? args[0] : string.Empty;
                command = command.ToLowerInvariant();
                switch (command) {
                    case "-killall":
                        ProcessUtilities.KillAllScProcesses();
                        break;
                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        var e = ErrorCode.ToMessage(Error.SCERRNOTIMPLEMENTED);
                        Console.WriteLine(e);
                        break;
                }

            } finally {
                Console.ResetColor();
            }
        }

    }
}
