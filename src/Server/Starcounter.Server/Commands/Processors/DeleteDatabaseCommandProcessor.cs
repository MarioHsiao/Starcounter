﻿
using Starcounter.Server.Commands.InternalCommands;
using Starcounter.Server.PublicModel.Commands;
using System;
using System.IO;

namespace Starcounter.Server.Commands {

    [CommandProcessor(typeof(DeleteDatabaseCommand))]
    internal sealed class DeleteDatabaseCommandProcessor : CommandProcessor {
        /// <summary>
        /// Initializes a new <see cref="DeleteDatabaseCommandProcessor"/>.
        /// </summary>
        /// <param name="server">The server in which the processor executes.</param>
        /// <param name="command">The <see cref="DeletedDatabaseCommand"/> the
        /// processor should exeucte.</param>
        public DeleteDatabaseCommandProcessor(ServerEngine server, ServerCommand command)
            : base(server, command) {
        }

        /// <inheritdoc />
        protected override void Execute() {
            Database database;
            DeleteDatabaseCommand command;

            CheckPrerequisites(out database, out command);

            // 1. Rename file.
            // 2. Remove database from public and internal state
            // 3. Remove file from public state
            // 4. Try deleting files (attempt 1). Schedule command
            // if it doesn't work.

            // Rename the configuration file, effectively marking it
            // deleted and not considered by the server the next time
            // it refresh its state from disk
            var key = this.Engine.StorageService.NewKey();
            MarkDatabaseDeletedInFileSystem(database, key, command.DeleteDataFiles);

            // Remove database from public and internal state
            // After this succeeds, the database delete is considered
            // a success. If we later fail to delete files, deleting
            // them will be retried until it eventually succeeds, and
            // they will no longer be visible. Creating a new database
            // with the same name should succeed, even before deleting
            // orphaned files is completed.
            this.Engine.CurrentPublicModel.RemoveDatabase(database);
            this.Engine.Databases.Remove(database.Name);

            // Just try this particular instance, and then schedule for all
            // occurances if this does not work.
            // TODO:

            // DropDeletedDatabaseFilesCommandProcessor.RunOnce(database.Name, key);

            // Until the above is in place, we always end with saying that we
            // failed to delete the files.
            throw ErrorCode.ToException(Error.SCERRDELETEDBFILESPOSTPONED, 
                string.Format("Database: '{0}'.", command.DatabaseUri));
        }

        void CheckPrerequisites(out Database database, out DeleteDatabaseCommand command) {
            command = (DeleteDatabaseCommand)this.Command;

            if (!this.Engine.Databases.TryGetValue(command.Name, out database)) {
                throw ErrorCode.ToException(Error.SCERRDATABASENOTFOUND, string.Format("Database: '{0}'.", command.DatabaseUri));
            }

            var running = this.Engine.DatabaseEngine.IsCodeHostProcessRunning(database) ||
                this.Engine.DatabaseEngine.IsDatabaseProcessRunning(database);
            if (running) {
                throw ErrorCode.ToException(Error.SCERRDATABASERUNNING, string.Format("Database: '{0}'.", command.DatabaseUri));
            }
        }

        void MarkDatabaseDeletedInFileSystem(Database database, string key, bool deleteDataFiles) {
            var kind = deleteDataFiles ? DeletedDatabaseFile.Kind.DeletedFully : DeletedDatabaseFile.Kind.Deleted;
            DeletedDatabaseFile.MarkDeleted(database.Configuration.ConfigurationFilePath, key, kind); 
        }
    }
}