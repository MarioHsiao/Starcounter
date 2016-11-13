// ***********************************************************************
// <copyright file="DatabaseSetup.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Starcounter.Advanced.Configuration;
using System.Diagnostics;
using Starcounter.Internal;

namespace Starcounter.Server {

    /// <summary>
    /// Implements the core setup / database creation service, utilized
    /// by components (such as command processors) that need to create
    /// databases.
    /// </summary>
    internal sealed class DatabaseSetup {
        /// <summary>
        /// Gets the <see cref="ServerEngine"/> whose repository we are
        /// targeting.
        /// </summary>
        internal readonly ServerEngine Engine;

        /// <summary>
        /// Gets the <see cref="DatabaseSetupProperties"/> that will be
        /// used when this setup executes.
        /// </summary>
        internal readonly DatabaseSetupProperties SetupProperties;

        /// <summary>
        /// Initializes a new <see cref="DatabaseSetup"/>.
        /// </summary>
        /// <param name="engine"></param>
        /// <param name="properties"></param>
        internal DatabaseSetup(ServerEngine engine, DatabaseSetupProperties properties) {
            this.Engine = engine;
            this.SetupProperties = properties;
        }

        /// <summary>
        /// Executes the given <see cref="DatabaseSetup"/> as it has
        /// been configured, creating all files (configuration + storage
        /// files) needed to represent a database under a certain server.
        /// </summary>
        /// <remarks>
        /// When a <see cref="DatabaseSetup"/> is executed, it expects
        /// all configuration values to be present and neither any defaults
        /// are applied/used nor is any checking/validation of configuration
        /// done.
        /// <para>
        /// The database created is NOT materialized as a server side domain
        /// object, only a proper file setup is created. Materializing the
        /// database domain object, and eventually making it part of the server
        /// runtime, is the responsability of the caller, or can be achieved
        /// by using <see cref="DatabaseSetup.CreateDatabase"/>.
        /// </para>
        /// </remarks>
        /// <returns>
        /// The full path to the database configuration file of the database
        /// that was just created.
        /// </returns>
        internal string CreateFiles() {
            string databaseName;
            string serverTempDirectory;
            string temporaryDatabaseDirectoryName;
            string databaseConfigFileName;
            string imageDirectory;
            string transactionLogDirectory;
            string liveDatabaseDirectory;
            string databaseTempDirectory;

            // Create a temporary directory for the configuration and such.
            // The idea is that we never let the database reach the server repository
            // until we have completed the full setup, assuring we will never
            // have the server in a faulty state.

            databaseName = SetupProperties.Name;
            databaseConfigFileName = databaseName + DatabaseConfiguration.FileExtension;
            serverTempDirectory = this.Engine.TempDirectory;
            temporaryDatabaseDirectoryName =
                Path.Combine(Environment.ExpandEnvironmentVariables(serverTempDirectory), databaseName + "-" + Guid.NewGuid());
            Directory.CreateDirectory(temporaryDatabaseDirectoryName);

            // Create storage files, based on the configuration.
            // Assure configurated values match existing directories by
            // creating the directories if they dont.

            imageDirectory = SetupProperties.Configuration.Runtime.ImageDirectory;
            if (!Directory.Exists(imageDirectory)) {
                Directory.CreateDirectory(imageDirectory);
            }
            transactionLogDirectory = SetupProperties.Configuration.Runtime.TransactionLogDirectory;
            if (!Directory.Exists(transactionLogDirectory)) {
                Directory.CreateDirectory(transactionLogDirectory);
            }
            databaseTempDirectory = SetupProperties.Configuration.Runtime.TempDirectory;
            if (!Directory.Exists(databaseTempDirectory)) {
                Directory.CreateDirectory(databaseTempDirectory);
            }

            this.Engine.StorageService.CreateStorage(
                databaseName, 
                imageDirectory, 
                this.SetupProperties.StorageConfiguration
                );

            // Write the full database configuration to the temporary directory.

            this.SetupProperties.Configuration.Save(Path.Combine(temporaryDatabaseDirectoryName, databaseConfigFileName));

            // Go "live" by renaning the temporary directory into the server
            // database repository.

            liveDatabaseDirectory = Path.Combine(Engine.DatabaseDirectory, databaseName);
            Directory.Move(temporaryDatabaseDirectoryName, liveDatabaseDirectory);

            // Return the live configuration file path.

            return Path.Combine(liveDatabaseDirectory, databaseConfigFileName);
        }

        /// <summary>
        /// Creates a database, including all its dependent files (i.e. it's
        /// configuration and storage files).
        /// </summary>
        /// <returns>A <see cref="Database"/> representing the newly created
        /// database.</returns>
        internal Database CreateDatabase() {
            var databaseConfigPath = CreateFiles();

            // Copying apps autostart config.
            String autostartJsonFile = Path.Combine("Configuration", StarcounterConstants.AutostartAppsJson);
            if (File.Exists(autostartJsonFile)) {
                File.Copy(autostartJsonFile,
                    Path.Combine(Path.GetDirectoryName(databaseConfigPath), StarcounterConstants.AutostartAppsJson), true);
            }

            var config = DatabaseConfiguration.Load(databaseConfigPath);
            return new Database(this.Engine, config);
        }
    }
}