
using Starcounter;
using Starcounter.Internal;
using System;

namespace staradmin {
    class Program {
        static void Main(string[] args) {
            try {
                Console.ForegroundColor = ConsoleColor.Red;
                var e = ErrorCode.ToMessage(Error.SCERRNOTIMPLEMENTED);
                Console.WriteLine(e);
            } finally {
                Console.ResetColor();
            }
        }
    }
}
