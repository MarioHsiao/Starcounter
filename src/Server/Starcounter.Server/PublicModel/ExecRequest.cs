
namespace Starcounter.Server.PublicModel {
    /// <summary>
    /// Represents a request to execute user code in a Starcounter
    /// host code process.
    /// </summary>
    public sealed class ExecRequest {

        /// <summary>
        /// Gets or sets the path to the executable the request
        /// concerns.
        /// </summary>
        public string ExecutablePath {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the identity of the database the code
        /// host should connect to.
        /// </summary>
        /// <remarks>
        /// We probably want to support both a simple name and
        /// a full Starcounter URI for this property.
        /// </remarks>
        public string Database {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the arguments the hosting infrastructure
        /// should pass to the executable entrypoint.
        /// </summary>
        /// <remarks>
        /// This set of arguments are opaque to Starcounter and never
        /// parsed or evaluated.
        /// </remarks>
        public string[] CommandLineArguments {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets an array specifying resource directories that
        /// should be considered by the REST server in the host, if the
        /// services of such is utilized.
        /// </summary>
        public string[] ResourceDirectories {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value that indicates if the host should
        /// omit connecting to a database.
        /// </summary>
        public bool NoDb {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating if the server should apply
        /// the "LogSteps" switch to the code host process in which the
        /// executable represented by this command is to be hosted.
        /// </summary>
        public bool LogSteps {
            get;
            set;
        }
    }
}