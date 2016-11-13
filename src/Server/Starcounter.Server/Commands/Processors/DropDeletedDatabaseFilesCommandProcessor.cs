
using Starcounter.Advanced.Configuration;
using Starcounter.Internal;
using Starcounter.Server.Commands.InternalCommands;
using Starcounter.Server.PublicModel.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Starcounter.Server.Commands {

    [CommandProcessor(typeof(DropDeletedDatabaseFilesCommand), IsInternal = true)]
    internal sealed class DropDeletedDatabaseFilesCommandProcessor : CommandProcessor {
        static TimeSpan TimeBetweenRetries = TimeSpan.FromSeconds(3);
        static int MaxRetries = 20;

        /// <summary>
        /// Initializes a new <see cref="DropDeletedDatabaseFilesCommandProcessor"/>.
        /// </summary>
        /// <param name="server">The server in which the processor executes.</param>
        /// <param name="command">The <see cref="DropDeletedDatabaseFilesCommand"/> the
        /// processor should exeucte.</param>
        public DropDeletedDatabaseFilesCommandProcessor(ServerEngine server, ServerCommand command)
            : base(server, command, true) {
        }

        /// <summary>
        /// Runs the functionality of this processor once, in the scope of
        /// the caller.
        /// </summary>
        /// <param name="engine">The server engine in which to run the
        /// processor.</param>
        /// <param name="file">The file whose related files that are to be
        /// dropped from the file system.</param>
        internal static void RunOnce(ServerEngine engine, DeletedDatabaseFile file) {
            bool delete = file.KindOfDelete == DeletedDatabaseFile.Kind.Deleted || file.KindOfDelete == DeletedDatabaseFile.Kind.DeletedFully;
            if (delete) {
                DeleteDatabaseFile(engine, file);
            }
        }

        protected override void Execute() {
            var command = (DropDeletedDatabaseFilesCommand)this.Command;
            Log.Debug("Running \"{0}\", attempt {1}", command.Description, command.RetryCount);

            try {
                ExecuteSafe(command);
            } catch (Exception exception) {
                var safe = ErrorCode.ToException(
                    Error.SCERRDELETEDBFILESPOSTPONED,
                    exception,
                    string.Format("Database: '{0}'.", command.DatabaseUri)
                    );
                Log.LogException(safe);
                RescheduleIfApplicable(command);
            }
        }

        void ExecuteSafe(DropDeletedDatabaseFilesCommand command) {
            AwaitTimeToRetry(command);

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
                        DeleteDatabaseFile(this.Engine, deleted, command.DatabaseKey);
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

        static bool DeleteDatabaseFile(ServerEngine engine, DeletedDatabaseFile file, string restrainToKey = null) {
            // Check if restrained to key. Ignore if not matching.
            if (!string.IsNullOrEmpty(restrainToKey)) {
                if (!file.Key.Equals(restrainToKey)) {
                    return false;
                }
            }

            // Check if we should delete data files - do that first.
            if (file.KindOfDelete == DeletedDatabaseFile.Kind.DeletedFully) {
                var storage = engine.StorageService;
                DatabaseConfiguration config;

                using (FileStream stream = new FileStream(file.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    var fakePath = file.FilePath + DatabaseConfiguration.FileExtension;
                    config = DatabaseConfiguration.Load(stream, fakePath);
                }

                var imageFiles = DatabaseStorageService.GetImageFiles(config.Runtime.ImageDirectory, file.DatabaseName);
                var logFiles = DatabaseStorageService.GetTransactionLogFiles(config.Runtime.ImageDirectory, file.DatabaseName);

                foreach (var imageFile in imageFiles) {
                    File.Delete(imageFile);
                }
                if (storage.IsNamedKeyDirectory(config.Runtime.ImageDirectory, file.DatabaseName)) {
                    SafeDeleteDirectoryIfEmpty(config.Runtime.ImageDirectory);
                }

                foreach (var logFile in logFiles) {
                    File.Delete(logFile);
                }
                if (storage.IsNamedKeyDirectory(config.Runtime.TransactionLogDirectory, file.DatabaseName)) {
                    SafeDeleteDirectoryIfEmpty(config.Runtime.TransactionLogDirectory);
                }

                // Delete the temporary directory if unique for this database
                // and it can be done without hazzle. We currently leave no
                // guarantee for it's deletion.

                if (storage.IsNamedKeyDirectory(config.Runtime.TempDirectory, file.DatabaseName)) {
                    SafeDeleteDirectoryIfEmpty(config.Runtime.TempDirectory);
                }

                // Deleting apps autostart config.
                String pathToAppsAutostartJson =
                    Path.Combine(Path.GetDirectoryName(config.ConfigurationFilePath), StarcounterConstants.AutostartAppsJson);

                if (File.Exists(pathToAppsAutostartJson))
                    File.Delete(pathToAppsAutostartJson);
            }

            // Delete Applications "Apps" configuration file
            string appsConfiguration = Path.Combine(Path.GetDirectoryName(file.FilePath), "applications.json");
            if (File.Exists(appsConfiguration)) {
                File.Delete(appsConfiguration);
            }
            // Delete Applications "Apps" folder
            string appsFolder = Path.Combine(Path.GetDirectoryName(file.FilePath), "apps");
            if (Directory.Exists(appsFolder)) {
                Directory.Delete(appsFolder, true);
            }

            // Delete Applications "Software" configuration file
            string softwareConfiguration = Path.Combine(Path.GetDirectoryName(file.FilePath), "software.json");
            if (File.Exists(softwareConfiguration)) {
                File.Delete(softwareConfiguration);
            }


            // Delete the file itself.
            // Then Delete the directory it resides in, if empty. Don't
            // fail if this does not succeed (deleting the directory is
            // never crucial).
            File.Delete(file.FilePath);
            SafeDeleteDirectoryIfEmpty(Path.GetDirectoryName(file.FilePath));

            return true;
        }

        static bool SafeDeleteDirectoryIfEmpty(string directory) {
            bool deleted = false;
            try {
                var files = Directory.GetFiles(directory);
                if (files.Length == 0) {
                    Directory.Delete(directory);
                    deleted = true;
                }
            } catch {}
            return deleted;
        }

        void RescheduleIfApplicable(DropDeletedDatabaseFilesCommand command) {
            if (command.RetryCount < MaxRetries) {
                var retry = new DropDeletedDatabaseFilesCommand(this.Engine, command.Name, command.DatabaseKey);
                retry.EnableWaiting = command.EnableWaiting;
                retry.LastAttempt = DateTime.Now;
                retry.RetryCount = command.RetryCount + 1;
                this.Engine.Dispatcher.Enqueue(retry, null, this);
            }
        }

        void AwaitTimeToRetry(DropDeletedDatabaseFilesCommand command) {
            if (command.LastAttempt == null) {
                return;
            }

            var last = command.LastAttempt.Value;
            var now = DateTime.Now;
            var timeSinceLast = now.Subtract(last);
            
            if (timeSinceLast < TimeBetweenRetries) {
                var wait = TimeBetweenRetries.Subtract(timeSinceLast);
                Thread.Sleep(wait);
            }
        }
    }
}