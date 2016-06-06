
using Starcounter.CLI;
using Starcounter.CommandLine;
using Starcounter.Internal;
using System.Collections.Generic;

namespace staradmin {
    /// <summary>
    /// Provides information about the context of the current
    /// staradmin sesssion.
    /// </summary>
    internal class Context {
        readonly ApplicationArguments args;

        /// <summary>
        /// Gets or sets the name of the database.
        /// </summary>
        public string Database { get; set; }

        /// <summary>
        /// Gets or sets a value indicating if the database was explicitiy
        /// stated on the command line.
        /// </summary>
        public bool IsExcplicitDatabase { get; set; }

        /// <summary>
        /// Gets or sets the admin server host.
        /// </summary>
        public string ServerHost { get; set; }

        /// <summary>
        /// Gets or sets the admin server port.
        /// </summary>
        public int ServerPort { get; set; }

        /// <summary>
        /// Gets a <see cref="ServerReference"/> corresponding to
        /// the values of the current instance.
        /// </summary>
        public ServerReference ServerReference {
            get {
                return new ServerReference(ServerHost, ServerPort);
            }
        }

        /// <summary>
        /// Initialize a new <see cref="Context"/> based on the
        /// given set of application arguments.
        /// </summary>
        /// <param name="appArgs">Arguments to base the context on.</param>
        public Context(ApplicationArguments appArgs) {
            args = appArgs;
        }

        /// <summary>
        /// Gets the resolved Starcounter installation directory.
        /// </summary>
        public string InstallationDirectory {
            get {
                return StarcounterEnvironment.InstallationDirectory;
            }
        }

        /// <summary>
        /// Gets the command parameters.
        /// </summary>
        public List<string> CommandParameters {
            get {
                return args.CommandParameters;
            }
        }

        /// <summary>
        /// Gets a value indicating if a flag with the given name was
        /// specified for the given command.
        /// </summary>
        /// <param name="name">The name of the flag.</param>
        /// <returns><c>true</c> if set; <c>false</c> otherwise.</returns>
        public bool ContainsCommandFlag(string name) {
            return args.ContainsFlag(name, CommandLineSection.CommandParametersAndOptions);
        }

        /// <summary>
        /// Gets a command property by name.
        /// </summary>
        /// <param name="name">The property to get.</param>
        /// <param name="value">The value of the property if found.</param>
        /// <returns><c>true</c> if found; <c>false</c> otherwise.</returns>
        public bool TryGetCommandProperty(string name, out string value) {
            return args.TryGetProperty(name, CommandLineSection.CommandParametersAndOptions, out value);
        }

        /// <summary>
        /// Gets a global property by name.
        /// </summary>
        /// <param name="name">The property to get.</param>
        /// <param name="value">The value of the property if found.</param>
        /// <returns><c>true</c> if found; <c>false</c> otherwise.</returns>
        public bool TryGetGlobalProperty(string name, out string value) {
            return args.TryGetProperty(name, CommandLineSection.GlobalOptions, out value);
        }
    }
}
