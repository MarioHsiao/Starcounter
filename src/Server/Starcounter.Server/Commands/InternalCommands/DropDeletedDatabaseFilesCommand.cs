
using Starcounter.Server.PublicModel.Commands;
using System;

namespace Starcounter.Server.Commands.InternalCommands {
    /// <summary>
    /// Allows the server to dispatch a task that will try to drop orphaned,
    /// deleted database files.
    /// </summary>
    internal sealed class DropDeletedDatabaseFilesCommand : DatabaseCommand {
        /// <summary>
        /// Gets an optional key that can be used to scope this command
        /// to a particular identified database instance, uniquely named
        /// by the given key.
        /// </summary>
        /// <remarks>
        /// The default is to process any orphaned database file collection
        /// for the specified database.
        /// </remarks>
        public readonly string DatabaseKey;

        /// <summary>
        /// Initializes a new <see cref="DropDeletedDatabaseFilesCommand"/>.
        /// </summary>
        /// <param name="engine">The server engine the command runs in.</param>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="key">Optional database key to scope this command to.</param>
        public DropDeletedDatabaseFilesCommand(ServerEngine engine, string databaseName, string key = "")
            : base(engine, CreateDatabaseUri(engine, databaseName), "Removing orphaned database files of deleted database {0}", databaseName) {
            if (string.IsNullOrEmpty(databaseName)) {
                throw new ArgumentNullException("databaseName");
            }
            this.DatabaseKey = key;
        }
    }
}