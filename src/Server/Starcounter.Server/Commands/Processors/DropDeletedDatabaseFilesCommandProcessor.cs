
using Starcounter.Server.Commands.InternalCommands;
using Starcounter.Server.PublicModel.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Server.Commands {

    [CommandProcessor(typeof(DropDeletedDatabaseFilesCommand), IsInternal = true)]
    internal sealed class DropDeletedDatabaseFilesCommandProcessor : CommandProcessor {
        /// <summary>
        /// Initializes a new <see cref="DropDeletedDatabaseFilesCommandProcessor"/>.
        /// </summary>
        /// <param name="server">The server in which the processor executes.</param>
        /// <param name="command">The <see cref="DropDeletedDatabaseFilesCommand"/> the
        /// processor should exeucte.</param>
        public DropDeletedDatabaseFilesCommandProcessor(ServerEngine server, ServerCommand command)
            : base(server, command, true) {
        }

        protected override void Execute() {
            try {
                ExecuteSafe();
            } catch (Exception e) {
                // TODO: Internal error describing this case, and on the wiki it should
                // say it's to no harm. It is considered "normal".
                var safe = ErrorCode.ToException(Error.SCERRUNSPECIFIED, e);
                Log.LogException(safe);
            }
        }

        void ExecuteSafe() {
            var command = (DropDeletedDatabaseFilesCommand)this.Command;
            var databaseDirectory = Path.Combine(this.Engine.DatabaseDirectory, command.Name);
            if (!Directory.Exists(databaseDirectory)) {
                Log.Debug("Database directory {0} for database {1} does not exist. File deletion not needed.", databaseDirectory, command.Name);
                return;
            }

            var files = DeletedDatabaseFile.GetAllFromDirectory(databaseDirectory);
            if (files.Length == 0) {
                Log.Debug("No files in database directory {0} marked deleted. File deletion not needed.", databaseDirectory);
                return;
            }

            foreach (var file in files) {
                var deleted = new DeletedDatabaseFile(file);

                switch (deleted.KindOfDelete) {
                    case DeletedDatabaseFile.Kind.Deleted:
                    case DeletedDatabaseFile.Kind.DeletedFully:
                        DeleteDatabaseFile(deleted, command.DatabaseKey);
                        break;
                    case DeletedDatabaseFile.Kind.Removed:
                        Log.Debug("Ignoring deleted database file(s) for database {0}. It was marked removed only.", command.Name);
                        break;
                    case DeletedDatabaseFile.Kind.Unsupported:
                    default:
                        Log.LogNotice("Ignoring deleted database config \"{0}\". The extension is not recognized.", file);
                        break;
                }
            }
        }

        void DeleteDatabaseFile(DeletedDatabaseFile file, string restrainToKey) {
            // 1. Check if restrained to key. Ignore if not matching.
            // 2. Check if we should delete data files - do that first.
            // 3. Delete the file itself.

            if (!string.IsNullOrEmpty(restrainToKey)) {
                if (!file.Key.Equals(restrainToKey)) {
                    return;
                }
            }

            if (file.KindOfDelete == DeletedDatabaseFile.Kind.DeletedFully) {
                // Locate the image- and log files and deleted them if they
                // still exist. In any case of error, raise an exception.
                // TODO:
            }

            File.Delete(file.FilePath);
        }
    }
}