
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

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
        /// been configured.
        /// </summary>
        internal void Execute() {
            string databaseName;
            string serverTempDirectory;
            string temporaryDatabaseDirectoryName;

            databaseName = SetupProperties.Name;
            serverTempDirectory = this.Engine.TempDirectory;
            temporaryDatabaseDirectoryName =
                Path.Combine(Environment.ExpandEnvironmentVariables(serverTempDirectory), databaseName + "-" + Guid.NewGuid());
            Directory.CreateDirectory(temporaryDatabaseDirectoryName);

            // Executes the setup, as it has been configured.
        }
    }
}