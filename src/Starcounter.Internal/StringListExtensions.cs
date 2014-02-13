
using System.Collections.Generic;
using System.Text;

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

        /// <summary>
        /// Returns reverse order of names in a dot separated string.
        /// For example, use to reverse order of full class name, which includes
        /// namespace names.
        /// </summary>
        /// <param name="fullName">The input full name string.</param>
        /// <returns>New string with dot separated names in reverse order</returns>
        public static string ReverseOrderDotWords(this string fullName) {
            if (fullName.Length == 0)
                return fullName;
            StringBuilder reversed = new StringBuilder(fullName.Length, fullName.Length);
            int curEnd = fullName.Length - 1;
            int lastDot = fullName.LastIndexOf('.');
            while (lastDot > -1) {
                reversed.Append(fullName.Substring(lastDot + 1, curEnd - lastDot));
                reversed.Append('.');
                curEnd = lastDot - 1;
                lastDot = fullName.LastIndexOf('.', curEnd);

            }
            reversed.Append(fullName.Substring(0, curEnd + 1));
            return reversed.ToString();
        }

        /// <summary>
        /// Returns first name, which is before first dot, in full class name.
        /// </summary>
        /// <param name="fullName">The full name.</param>
        /// <returns>The first name without dots.</returns>
        public static string LastDotWord(this string fullName) {
            int dotPos = fullName.LastIndexOf('.');
            if (dotPos > -1)
                return fullName.Substring(dotPos + 1);
            return fullName;
        }
    }
}