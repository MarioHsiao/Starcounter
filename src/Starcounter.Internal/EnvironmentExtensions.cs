
using System;

namespace Starcounter.Internal {
    /// <summary>
    /// Provides a set of extented utility methods to work with the current
    /// environment and platform.
    /// </summary>
    /// <remarks>
    /// <see cref="System.Environment"/>
    /// </remarks>
    public static class EnvironmentExtensions {
        /// <summary>
        /// Gets an environment variable value, parsed as an integer. If the
        /// environment variable is not set or parsing it to an integer fails,
        /// the <paramref name="fallback"/> is returned.
        /// </summary>
        /// <param name="variable">The name of the environment variable.</param>
        /// <param name="fallback">The fallback to use if the variable was not
        /// defined or was not possible to parse to an integer.</param>
        /// <returns>The value of the given variable, if defined and numeric;
        /// the <paramref name="fallback"/> otherwise.</returns>
        public static int GetEnvironmentInteger(string variable, int fallback = -1) {
            int result;
            var x = Environment.GetEnvironmentVariable(variable);
            if (x == null || !int.TryParse(x, out result)) {
                result = fallback;
            }
            return result;
        }
    }
}