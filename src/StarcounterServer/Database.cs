
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter;
using Starcounter.Configuration;
using StarcounterServer.PublicModel;

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
        internal readonly DatabaseConfiguration Configuration;

        /// <summary>
        /// Gets the simple name of this database.
        /// </summary>
        internal readonly string Name;

        /// <summary>
        /// Gets the URI of this database.
        /// </summary>
        internal readonly string Uri;

        /// <summary>
        /// Intializes a <see cref="Database"/>.
        /// </summary>
        /// <param name="server">The server to which the current database belong.</param>
        /// <param name="configuration">The configuration applied.</param>
        internal Database(ServerNode server, DatabaseConfiguration configuration) {
            this.Server = server;
            this.Configuration = configuration;
            this.Name = this.Configuration.Name;
            this.Uri = ScUri.MakeDatabaseUri(ScUri.GetMachineName(), server.Name, this.Name).ToString();
        }

        /// <summary>
        /// Creates a snapshot of this <see cref="Database"/> in the
        /// form of a public model <see cref="DatabaseInfo"/>.
        /// </summary>
        /// <returns>A <see cref="DatabaseInfo"/> representing the current state
        /// of this database.</returns>
        internal DatabaseInfo ToPublicModel() {
            var info = new DatabaseInfo() {
                CollationFile = null,
                Configuration = this.Configuration.Clone(this.Configuration.ConfigurationFilePath),
                Name = this.Name,
                MaxImageSize = 0,   // TODO: Backlog
                SupportReplication = false,
                TransactionLogSize = 0, // TODO: Backlog
                Uri = this.Uri
            };
            return info;
        }
    }
}