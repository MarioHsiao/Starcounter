
using System;

namespace Starcounter.Server.PublicModel.Commands {
    /// <summary>
    /// Defines the command to use when a host want to issue a delete
    /// of a database.
    /// </summary>
    public sealed class DeleteDatabaseCommand : DatabaseCommand {
        /// <summary>
        /// Gets a value indicating if the data files (i.e image-
        /// and transaction log files) are to be deleted too.
        /// </summary>
        public readonly bool DeleteDataFiles;

        /// <summary>
        /// Initialize a new <see cref="DeleteDatabaseCommand"/>.
        /// </summary>
        /// <param name="engine">The engine to which the command belong.</param>
        /// <param name="databaseName">The name of the database to delete.</param>
        /// <param name="deleteDataFiles">A value indicating if data files should
        /// be deleted too.</param>
        public DeleteDatabaseCommand(ServerEngine engine, string databaseName, bool deleteDataFiles = false) : 
            base(engine, CreateDatabaseUri(engine, databaseName), "Deleting database {0}{1}", databaseName, deleteDataFiles ? " (including data files)" : string.Empty) {
            if (string.IsNullOrEmpty(databaseName)) {
                throw new ArgumentNullException("databaseName");
            }

            this.DeleteDataFiles = deleteDataFiles;
        }
    }
}