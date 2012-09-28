
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter.Configuration;

namespace StarcounterServer {

    /// <summary>
    /// A database maintained by a certain server (represented by
    /// the <see cref="Database.Server"/> property).
    /// </summary>
    internal sealed class Database {
        
        /// <summary>
        /// The server to which this database belongs.
        /// </summary>
        internal readonly ServerNode Server;

        /// <summary>
        /// The configuration of this <see cref="Database"/>.
        /// </summary>
        internal readonly DatabaseRuntimeConfiguration Configuration;

        /// <summary>
        /// Intializes a <see cref="Database"/>.
        /// </summary>
        /// <param name="server"></param>
        /// <param name="configuration"></param>
        internal Database(ServerNode server, DatabaseRuntimeConfiguration configuration) {
            this.Server = server;
            this.Configuration = configuration;
        }
    }
}