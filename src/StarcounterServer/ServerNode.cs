
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter.Configuration;
using Starcounter;

namespace StarcounterServer {

    /// <summary>
    /// Representing the running server, hosted in a server program.
    /// </summary>
    internal sealed class ServerNode {

        /// <summary>
        /// Gets the server configuration.
        /// </summary>
        internal readonly ServerConfiguration Configuration;

        /// <summary>
        /// Gets the URI of this server.
        /// </summary>
        internal readonly string Uri;

        /// <summary>
        /// Gets the dictionary with databases maintained by this server,
        /// keyed by their name.
        /// </summary>
        internal Dictionary<string, Database> Databases { get; private set; }

        /// <summary>
        /// Initializes a <see cref="ServerNode"/>.
        /// </summary>
        /// <param name="configuration"></param>
        internal ServerNode(ServerConfiguration configuration) {
            this.Configuration = configuration;
            this.Uri = ScUri.MakeServerUri(ScUri.GetMachineName(), configuration.Name);
            this.Databases = new Dictionary<string, Database>();
        }

        internal void Setup() {
        }

        internal void Start() {
        }

        internal void Stop() {
        }
    }
}
