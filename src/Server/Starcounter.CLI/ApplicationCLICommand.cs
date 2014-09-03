
namespace Starcounter.CLI {
    /// <summary>
    /// Provides the principal entrypoint to use when a CLI client
    /// want to use the common way to start or stop an application.
    /// </summary>
    public abstract class ApplicationCLICommand : CLIClientCommand {
        /// <summary>
        /// Gets or sets the application name.
        /// </summary>
        internal string ApplicationName { get; private set; }

        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        protected ApplicationCLICommand(string applicationName) {
            ApplicationName = applicationName;
        }
    }
}