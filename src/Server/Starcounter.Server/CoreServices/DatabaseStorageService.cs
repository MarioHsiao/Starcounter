using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter.Configuration;
using System.IO;

namespace Starcounter.Server {
    
    /// <summary>
    /// Exposes a set of methods that can be used to easily work with
    /// database storages, i.e. sets of image- and transaction log files.
    /// </summary>
    internal sealed class DatabaseStorageService {
        readonly ServerEngine engine;
        string creationToolPath;

        /// <summary>
        /// Intializes a new <see cref="DatabaseStorageService"/>.
        /// </summary>
        /// <param name="engine">The engine in which the current service
        /// runs.</param>
        internal DatabaseStorageService(ServerEngine engine) {
            this.engine = engine;
        }

        /// <summary>
        /// Executes setup of the <see cref="DatabaseStorageService"/>.
        /// </summary>
        internal void Setup() {
            creationToolPath = Path.Combine(engine.InstallationDirectory, "scdbc.exe");
            if (!File.Exists(creationToolPath)) {
                throw ErrorCode.ToException(Error.SCERRUNSPECIFIED, string.Format("Couldn't find creation tool: {0}", creationToolPath));
            }
        }

        /// <summary>
        /// Creates a new storage for with the given values.
        /// </summary>
        /// <param name="name">The name of database to create a new storage for.</param>
        /// <param name="imagePath">The path to where image files should be created.</param>
        /// <param name="logPath">The path to where transaction logs should be created.</param>
        /// <param name="configuration">The <see cref="DatabaseStorageConfiguration"/> to use
        /// </param>
        internal void CreateStorage(string name, string imagePath, string logPath, DatabaseStorageConfiguration configuration) {
            // Construct the command line.
            // TODO:

            // Execute the tool.
            // TODO:

            throw new NotImplementedException();
        }
    }
}