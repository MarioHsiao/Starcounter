using Starcounter.CommandLine.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace star {

    internal static class ConsoleUtil {

        internal static bool DisableColors = false;

        internal static void ToConsoleWithColor(string text, ConsoleColor color) {
            if (DisableColors) {
                Console.WriteLine(text);
            }
            else {
                try {
                    Console.ForegroundColor = color;
                    Console.WriteLine(text);
                } finally {
                    Console.ResetColor();
                }
            }
        }

        internal static void ToConsoleWithColor(Action action, ConsoleColor color) {
            if (DisableColors) {
                action();
            }
            else {
                try {
                    Console.ForegroundColor = color;
                    action();
                } finally {
                    Console.ResetColor();
                }
            }
        }
    }
}
