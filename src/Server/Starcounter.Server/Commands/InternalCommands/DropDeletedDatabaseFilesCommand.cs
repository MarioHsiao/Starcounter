
using Starcounter.Server.PublicModel.Commands;
using System;

namespace Starcounter.Server.Commands.InternalCommands {
    /// <summary>
    /// Allows the server to dispatch a task that will try to drop orphaned,
    /// deleted database files.
    /// </summary>
    internal sealed class DropDeletedDatabaseFilesCommand : DatabaseCommand {
        /// <summary>
        /// The file pattern used by the server to find database files that
        /// have been marked deleted/removed.
        /// </summary>
        public const string DeletedFilesPattern = "*+deleted";

        /// <summary>
        /// The file extension used by the server to mark a database as
        /// "removed", i.e. hiding it from the server when exposing state
        /// but where files should be left intact.
        /// </summary>
        public const string RemovedDatabaseFileExtension = "+deleted";

        /// <summary>
        /// The file extension used by the server to mark a database as
        /// deleted, i.e. its configuration should be deleted but database
        /// data files should be left untouched.
        /// </summary>
        public const string DeletedDatabaseFileExtension = "++deleted";

        /// <summary>
        /// The file extension used by the server to mark a database as
        /// deleted, including the deletion of it's referenced data files.
        /// </summary>
        public const string DeletedDatabaseAndDataFileExtension = "+++deleted";

        /// <summary>
        /// Initializes a new <see cref="DropDeletedDatabaseFilesCommand"/>.
        /// </summary>
        /// <param name="engine">The server engine the command runs in.</param>
        /// <param name="databaseName">The name of the database.</param>
        public DropDeletedDatabaseFilesCommand(ServerEngine engine, string databaseName)
            : base(engine, CreateDatabaseUri(engine, databaseName), "Removing orphaned database files of deleted database {0}", databaseName) {
            if (string.IsNullOrEmpty(databaseName)) {
                throw new ArgumentNullException("databaseName");
            }
        }
    }
}