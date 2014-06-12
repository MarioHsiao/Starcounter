
using Starcounter.CommandLine;
using Starcounter.CommandLine.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace staradmin {

    internal static class CommandLine {
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