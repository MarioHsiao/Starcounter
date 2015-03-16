
using System;
using System.Diagnostics;

namespace Starcounter.Server {

    /// <summary>
    /// Provides information about the calling client context.
    /// </summary>
    public static class ClientContext {
        /// <summary>
        /// The current context. Set by client applications.
        /// </summary>
        public static string Current = KnownClientContexts.UnknownContext;

        /// <summary>
        /// Creates a string containing the current client context
        /// information, including information about the current client
        /// and the user.
        /// </summary>
        /// <returns>A string representing the current context.</returns>
        public static string GetCurrentContextInfo() {
            return Make(Current);
        }

        static string Make(string context) {
            // Note:
            // Don't change this format unless also changing the parser
            // method KnownClientContext.FromContextInfo() in Starcounter.Server.
            // Example contexts:
            //  * "star.exe, per@per-asus (via star.exe)"
            //  * "VS, per@per-asus (via devenv.exe)"
            var program = Process.GetCurrentProcess().MainModule.ModuleName;
            try {
                return string.Format("{0}, {1}@{2} (via {3})",
                    context,
                    Environment.UserName.ToLowerInvariant(),
                    Environment.MachineName.ToLowerInvariant(), program
                    );
            } catch {
                return string.Format("{0}, (via {1})", context, program);
            }
        }
    }

    /// <summary>
    /// Enum-like class with constants of known management
    /// client contexts.
    /// </summary>
    public static class KnownClientContexts {
        /// <summary>
        /// Visual Studio context.
        /// </summary>
        public const string VisualStudio = "VS";
        /// <summary>
        /// The star.exe context.
        /// </summary>
        public const string Star = "star.exe";
        /// <summary>
        /// The staradmin.exe context.
        /// </summary>
        public const string StarAdmin = "staradmin.exe";
        /// <summary>
        /// Context of the administration server.
        /// </summary>
        public const string Admin = "Admin";

        /// Used when the context is unknown.
        /// </summary>
        public const string UnknownContext = "Unknown";

        /// <summary>
        /// Extracts the known context identifier from the given
        /// contextInfo string (previously produced by 
        /// ClientContext.GetCurrentContextInfo).
        /// </summary>
        /// <param name="contextInfo">The context info string to parse.</param>
        /// <returns>The context identifier</returns>
        public static string ParseFromContextInfo(string contextInfo) {
            if (string.IsNullOrWhiteSpace(contextInfo)) return KnownClientContexts.UnknownContext;
            int index = contextInfo.IndexOf(",");
            if (index == -1) return KnownClientContexts.UnknownContext;
            return contextInfo.Substring(0, index);
        }
    }
}
