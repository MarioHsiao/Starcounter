using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.CommandLine {

    /// <summary>
    /// Utility class exposing functionality that support splitting of
    /// strings into string arrays, similar to how it's done by the CRT/CLR.
    /// </summary>
    public static class CommandLineStringParser {
        /// <summary>
        /// Splits a command-line string into arguments, similar to how it's
        /// done by the CRT/CLR.
        /// </summary>
        /// <param name="commandLine">The command-line to split.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> that can be used
        /// to iterate over all entries that was the result of the split.</returns>
        public static IEnumerable<string> SplitCommandLine(string commandLine) {
            if (commandLine == null)
                throw new ArgumentNullException("commandLine");

            bool inQuotes = false;

            return Split(commandLine,
                c => {
                    if (c == '\"')
                        inQuotes = !inQuotes;
                    return !inQuotes && c == ' ';
                }).Select(arg => TrimMatchingQuotes(arg.Trim(), '\"')).Where(arg => !string.IsNullOrEmpty(arg));
        }

        /// <summary>
        /// Allows a string to be split up in unique entities based on the
        /// decision of a given delegate.
        /// </summary>
        /// <param name="s">The string to split.</param>
        /// <param name="requiresSplit">The delegate that rules if a given
        /// character should require a split.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> that can be used
        /// to iterate over all entries that was the result of the split.
        /// </returns>
        public static IEnumerable<string> Split(string s, Func<char, bool> requiresSplit) {
            int next = 0;
            for (int c = 0; c < s.Length; c++) {
                if (requiresSplit(s[c])) {
                    yield return s.Substring(next, c - next);
                    next = c + 1;
                }
            }

            yield return s.Substring(next);
        }

        /// <summary>
        /// Trims a string based on a pair of matching quotes. Only if the
        /// match is exact (i.e. there is a starting and an ending quote),
        /// the trim is executed.
        /// </summary>
        /// <param name="input">The string to trim.</param>
        /// <param name="quote">The qouting character.</param>
        /// <returns>The trimmed result.</returns>
        public static string TrimMatchingQuotes(string input, char quote) {
            if ((input.Length >= 2) &&
                (input[0] == quote) && (input[input.Length - 1] == quote))
                return input.Substring(1, input.Length - 2);

            return input;
        }
    }
}
