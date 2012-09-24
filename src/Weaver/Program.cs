
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter;
using Starcounter.CommandLine;
using Starcounter.CommandLine.Syntax;

namespace Weaver {

    class Program {

        static void Main(string[] args) {
            ApplicationArguments arguments;

            try {
                if (TryGetProgramArguments(args, out arguments))
                    ExecuteCommand(arguments);
            } finally {
                Console.ResetColor();
            }
        }

        static void ExecuteCommand(ApplicationArguments arguments) {
            // Implement.
            // TODO:
        }

        static bool TryGetProgramArguments(string[] args, out ApplicationArguments arguments) {
            arguments = null;
            return false;
        }
    }
}
