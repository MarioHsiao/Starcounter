
using Starcounter.Internal;

namespace staradmin {
    /// <summary>
    /// Provides information about the context of the current
    /// staradmin sesssion.
    /// </summary>
    internal class Context {
        /// <summary>
        /// Gets or sets the name of the database.
        /// </summary>
        public string Database { get; set; }

        /// <summary>
        /// Gets the resolved Starcounter installation directory.
        /// </summary>
        public string InstallationDirectory {
            get {
                return StarcounterEnvironment.InstallationDirectory;
            }
        }
    }
}
