using Starcounter.CommandLine.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace star {

    internal static class ConsoleUtil {

        internal static void ToConsoleWithColor(string text, ConsoleColor color) {
            try {
                Console.ForegroundColor = color;
                Console.WriteLine(text);
            } finally {
                Console.ResetColor();
            }
        }

        internal static void ToConsoleWithColor(Action action, ConsoleColor color) {
            try {
                Console.ForegroundColor = color;
                action();
            } finally {
                Console.ResetColor();
            }
        }
    }
}
