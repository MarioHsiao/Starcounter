
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter.Configuration;

namespace Starcounter.Server.PublicModel.Commands {
    /// <summary>
    /// Encapsulates the parameters used when creating a new database.
    /// </summary>
    public sealed class CreateDatabaseCommand : ServerCommand {
        /// <summary>
        /// Gets the name of the database to be created.
        /// </summary>
        public readonly string DatabaseName;

        /// <summary>
        /// Gets the <see cref="DatabaseStorageConfiguration"/> specifying how
        /// the storage for the database will be created.
        /// </summary>
        public readonly DatabaseStorageConfiguration StorageConfiguration;

        /// <summary>
        /// Gets the management configuration for the database about to be
        /// created.
        /// </summary>
        public readonly DatabaseConfiguration Configuration;

        public CreateDatabaseCommand(ServerEngine engine, string databaseName) : base(engine, "Creating database {0}", databaseName) {
            if (string.IsNullOrEmpty(databaseName)) {
                throw new ArgumentNullException("name");
            }

            this.DatabaseName = databaseName;
            this.Configuration = (DatabaseConfiguration) engine.Configuration.DefaultDatabaseConfiguration.Clone();
            this.StorageConfiguration = (DatabaseStorageConfiguration) engine.Configuration.DefaultDatabaseStorageConfiguration.Clone();
        }
    }
}
