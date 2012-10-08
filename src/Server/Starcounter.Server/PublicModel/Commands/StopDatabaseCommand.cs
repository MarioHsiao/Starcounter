﻿
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Server.PublicModel.Commands {
    /// <summary>
    /// Encapsulates a request to stop or suspend a database.
    /// </summary>
    public sealed class StopDatabaseCommand : DatabaseCommand {
        /// <summary>
        /// Gets or sets a value indicating if the command should not
        /// only stop the worker process for the given database, but
        /// shut down the database process too.
        /// </summary>
        /// <remarks>
        /// The default is false, meaning the database is suspended.
        /// </remarks>
        public bool StopDatabaseProcess { get; set; }

        /// <summary>
        /// Initializes a new <see cref="StopDatabaseCommand"/>.
        /// </summary>
        /// <param name="engine"></param>
        /// <param name="databaseName"></param>
        public StopDatabaseCommand(ServerEngine engine, string databaseName)
            : this(engine, databaseName, false) {
        }

        /// <summary>
        /// Initializes a new <see cref="StopDatabaseCommand"/>.
        /// </summary>
        /// <param name="engine"></param>
        /// <param name="databaseName"></param>
        /// <param name="stopDatabaseProcessToo"></param>
        public StopDatabaseCommand(ServerEngine engine, string databaseName, bool stopDatabaseProcessToo)
            : base(engine, CreateDatabaseUri(engine, databaseName), string.Format("{0} database {1}.", stopDatabaseProcessToo ? "Stopping" : "Suspending", databaseName)) {
                this.StopDatabaseProcess = stopDatabaseProcessToo;
        }
    }
}
