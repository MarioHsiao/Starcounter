
using System.Collections.Generic;

namespace Starcounter.Internal {
    /// <summary>
    /// Provides a set of extension methods for the commonly used
    /// <see cref="List<string>"/> type.
    /// </summary>
    public static class StringListExtensions {
        /// <summary>
        /// Adds a string to the list after first making sure it is
        /// formatted using the given arguments.
        /// </summary>
        /// <param name="list">The list to add the formatted result to.</param>
        /// <param name="format">The formatting string.</param>
        /// <param name="args">The arguments to use when formatting the
        /// string.</param>
        public static void AddFormat(this List<string> list, string format, params object[] args) {
            list.Add(string.Format(format, args));
        }

        /// <summary>
        /// Returns a string based on the values stored in <paramref name="list"/>.
        /// </summary>
        /// <param name="list">The list whose values should be formatted as a
        /// string.</param>
        /// <returns>A string with all values in the given list.</returns>
        public static string ToStringFromValues(this List<string> list) {
            return string.Join(" ", list);
        }
    }
}