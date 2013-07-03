
using System;

namespace Starcounter.Server.PublicModel.Commands {
    /// <summary>
    /// Defines the command to use when a host want to issue a delete
    /// of a database.
    /// </summary>
    public sealed class DeleteDatabaseCommand : ServerCommand {
        /// <summary>
        /// Gets the name of the database to delete.
        /// </summary>
        public readonly string DatabaseName;

        /// <summary>
        /// Gets a value indicating if the data files (i.e image-
        /// and transaction log files) are to be deleted too.
        /// </summary>
        public readonly bool DeleteDataFiles;

        public DeleteDatabaseCommand(ServerEngine engine, string databaseName, bool deleteDataFiles = false) : base(engine, "Deleting database {0}", databaseName) {
            if (string.IsNullOrEmpty(databaseName)) {
                throw new ArgumentNullException("databaseName");
            }

            this.DatabaseName = databaseName;
            this.DeleteDataFiles = deleteDataFiles;
        }
    }
}