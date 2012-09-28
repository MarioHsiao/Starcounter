
using System.Xml.Serialization;
using Starcounter.Configuration;

namespace StarcounterServer.PublicModel {

    /// <summary>
    /// Represents a snapshot of the public state of a server.
    /// </summary>
    internal sealed class ServerInfo {

        /// <summary>
        /// Initializes a <see cref="ServerInfo"/> message object.
        /// </summary>
        public ServerInfo() {
        }

        /// <summary>
        /// Gets the URI of the server.
        /// </summary>
        /// <remarks>
        /// The URI is a logical URI of type <see cref="ScUriKind.Server"/>
        /// </remarks>
        public string Uri {
            get;
            set;
        }

        /// <summary>
        /// Configuration of the server.
        /// </summary>
        public ServerConfiguration Configuration {
            get;
            set;
        }

        /// <summary>
        /// Gets a value indicating if monitoring is supported
        /// by this server.
        /// </summary>
        public bool IsMonitoringSupported {
            get;
            set;
        }

        /// <summary>
        /// The servers default maximum image size, used when creating
        /// databases if no maximum size is explicitly given.
        /// </summary>
        public long DefaultMaxImageSize {
            get;
            set;
        }

        /// <summary>
        /// The servers default transaction log size, used when creating
        /// databases if no maximum size is explicitly given.
        /// </summary>
        public long DefaultTransactionLogSize {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the user name the server represented by the
        /// current information object runs under.
        /// </summary>
        public string UserName {
            get;
            set;
        }

        /// <summary>
        /// The full path to the server configuration file whose
        /// database repository this server maintains.
        /// </summary>
        public string ServerConfigurationPath {
            get;
            set;
        }
    }
}