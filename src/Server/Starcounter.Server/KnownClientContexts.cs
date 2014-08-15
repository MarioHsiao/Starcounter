
namespace Starcounter.Server {
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
            int index = contextInfo.IndexOf(",");
            if (index == -1) return KnownClientContexts.UnknownContext;
            return contextInfo.Substring(0, index);
        }
    }
}
