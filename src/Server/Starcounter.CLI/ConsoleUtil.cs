
using System;

namespace Starcounter.CLI {
    /// <summary>
    /// Provides a set of utility methods for the console.
    /// </summary>
    public static class ConsoleUtil {
        /// <summary>
        /// Gets or sets a value if the CLI client should disable
        /// colorization.
        /// </summary>
        public static bool DisableColors { get; set; }

        /// <summary>
        /// Writes <paramref name="text"/> to the console using the given
        /// <see cref="ConsoleColor"/>.
        /// </summary>
        /// <param name="text">The text to write.</param>
        /// <param name="color">The color to use.</param>
        public static void ToConsoleWithColor(string text, ConsoleColor color) {
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

        /// <summary>
        /// Executes an action where all console output written will use
        /// the given <see cref="ConsoleColor"/> when writing to the console.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <param name="color">The color to use.</param>
        public static void ToConsoleWithColor(Action action, ConsoleColor color) {
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
