
using Starcounter.CLI;
using Starcounter.CommandLine;
using Starcounter.CommandLine.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace staradmin {

    internal static class CommandLine {
        
        public static void PreParse(ref string[] args) {
            if (args.Length > 0) {
                var first = args[0].TrimStart('-');
                if (first.Equals(SharedCLI.UnofficialOptions.Debug, StringComparison.InvariantCultureIgnoreCase)) {
                    Debugger.Launch();
                    var stripped = new string[args.Length - 1];
                    Array.Copy(args, 1, stripped, 0, args.Length - 1);
                    args = stripped;
                }
            }
        }

        public static ApplicationArguments Parse(string[] args) {
            if (args.Length == 0) {
                return ApplicationArguments.Empty;
            }

            throw new NotImplementedException();
        }

        public static void WriteUsage(TextWriter writer) {
            throw new NotImplementedException();
        }

        static IApplicationSyntax Define() {
            throw new NotImplementedException();
        }
    }
}