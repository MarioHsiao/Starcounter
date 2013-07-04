// ***********************************************************************
// <copyright file="DatabaseSetupProperties.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Configuration;
using System.IO;

namespace Starcounter.Server {

    /// <summary>
    /// Class DatabaseSetupProperties
    /// </summary>
    public sealed class DatabaseSetupProperties {
        /// <summary>
        /// Gets the name of the database to which the current
        /// property set applies.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// Gets the key used by the server when resolving and creating
        /// the image- and log directories respectively.
        /// </summary>
        public readonly string Key;

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
            this.Key = string.Format("{0}-{1}", this.Name, engine.StorageService.NewKey());

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

        /// <summary>
        /// Wraps up the editing of the current database setup properties,
        /// applying values enforced by the server library.
        /// </summary>
        internal void MakeFinal() {
            var config = this.Configuration.Runtime;
            config.ImageDirectory = Path.Combine(config.ImageDirectory, this.Key);
            config.TransactionLogDirectory = Path.Combine(config.TransactionLogDirectory, this.Key);
            config.TempDirectory = Path.Combine(config.TempDirectory, this.Key);
        }

        void ExpandPlaceholdersInConfigurationStrings() {
            var config = this.Configuration.Runtime;
            config.ImageDirectory = config.ImageDirectory.Replace("[DatabaseName]", this.Name);
            config.TransactionLogDirectory = config.TransactionLogDirectory.Replace("[DatabaseName]", this.Name);
            config.TempDirectory = config.TempDirectory.Replace("[DatabaseName]", this.Name);
        }
    }
}
