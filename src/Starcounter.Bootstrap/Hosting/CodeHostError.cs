
using Starcounter.Internal;
using System;
using System.Text;

namespace Starcounter.Hosting {
    /// <summary>
    /// Expose utility code used when propagating and consuming
    /// error data relating to errors in the code host.
    /// </summary>
    public static class CodeHostError {
        /// <summary>
        /// Gets a value used by hosting components to propagate errors via
        /// a simple parcelling mechanism.
        /// </summary>
        public const string ErrorParcelID = "7EDE1D87-8181-4B1C-B1DF-CB939706F8CF--FC8F607939BC-FD1B-C1B4-1818-78D1EDE7";

        /// <summary>
        /// Reports <paramref name="error"/> as a code host error.
        /// </summary>
        /// <param name="error">The error to report.</param>
        internal static void Report(string error) {
            Console.Error.Write(ParcelledError.Format(CodeHostError.ErrorParcelID, error));
        }

        /// <summary>
        /// Reports <paramref name="exception"/> as a code host error.
        /// </summary>
        /// <param name="exception">The error to report.</param>
        /// <param name="includeStackTrace"><c>true</c> if the stacktrace should be
        /// part of the report; <c>false</c> otherwise.</param>
        internal static void Report(Exception exception, bool includeStackTrace = false) {
            var sb = new StringBuilder();
            sb.AppendLine(exception.Message);
            if (exception.StackTrace != null && includeStackTrace) {
                sb.AppendLine(exception.StackTrace);
            }
            Report(sb.ToString());
        }
    }
}
