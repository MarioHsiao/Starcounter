
using Starcounter.Configuration;

namespace Starcounter.Server {

    public sealed class DatabaseSetupProperties {
        /// <summary>
        /// Gets the name of the database to which the current
        /// property set applies.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// Gets the resolved management configuration of the database
        /// to which the current property set applies.
        /// </summary>
        public readonly DatabaseConfiguration Configuration;

        /// <summary>
        /// Gets the database storage configuration of the database
        /// to which the current property set applies.
        /// </summary>
        public readonly DatabaseStorageConfiguration StorageConfiguration;

        /// <summary>
        /// Initializes a <see cref="DatabaseSetupProperties"/> instance.
        /// </summary>
        /// <param name="engine">The engine from which default configuration
        /// should be fetched.</param>
        /// <param name="databaseName">The name of the database the current
        /// <see cref="DatabaseSetupProperties"/> are to reprsent.</param>
        internal DatabaseSetupProperties(ServerEngine engine, string databaseName) {
            this.Name = databaseName;

            // Deep clone the default server management configuration and do
            // replacement strategies and apply defaults.
            this.Configuration = (DatabaseConfiguration)engine.Configuration.DefaultDatabaseConfiguration.Clone();
            this.ExpandPlaceholdersInConfigurationStrings();

            // Create a new storage configuration based on the established
            // server defaults (fetched either from default configuration or
            // from computed/static server defaults).
            this.StorageConfiguration = new DatabaseStorageConfiguration();
            this.StorageConfiguration.MaxImageSize = engine.DatabaseDefaultValues.MaxImageSize;
            this.StorageConfiguration.TransactionLogSize = engine.DatabaseDefaultValues.TransactionLogSize;
            this.StorageConfiguration.SupportReplication = false;
            this.StorageConfiguration.CollationFile = engine.DatabaseDefaultValues.CollationFile;
        }

        void ExpandPlaceholdersInConfigurationStrings() {
            this.Configuration.Runtime.ImageDirectory = this.Configuration.Runtime.ImageDirectory.Replace("[DatabaseName]", this.Name);
            this.Configuration.Runtime.TransactionLogDirectory = this.Configuration.Runtime.TransactionLogDirectory.Replace("[DatabaseName]", this.Name);
            this.Configuration.Runtime.TempDirectory = this.Configuration.Runtime.TempDirectory.Replace("[DatabaseName]", this.Name);
        }
    }
}
