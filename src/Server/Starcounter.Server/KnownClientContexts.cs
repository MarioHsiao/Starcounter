
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
    }
}
