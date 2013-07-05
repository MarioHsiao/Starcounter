using Starcounter.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Server {
    /// <summary>
    /// Encapsulates the logic and conventions used by the server library
    /// when deleting databases (via renaming of database configuration files
    /// and subsequent physical removal of said files and, optionally, the
    /// database data files the reference).
    /// </summary>
    internal sealed class DeletedDatabaseFile {
        /// <summary>
        /// The file pattern used by the server to find database files that
        /// have been marked deleted/removed.
        /// </summary>
        public const string DeletedFilesPattern = "*+deleted";

        /// <summary>
        /// The kind of deletes supported by the server library.
        /// </summary>
        public enum Kind {
            /// <summary>
            /// The database is deleted in the sence that it is no
            /// longer made visible in the public server model, but
            /// no data is ever deleted (including configuration).
            /// </summary>
            Removed,
            /// <summary>
            /// Extends <see cref="DeletedDatabaseFile.Kind.Removed"/>
            /// by having the configuration and temporary data deleted,
            /// but not database image- and log files.
            /// </summary>
            Deleted,
            /// <summary>
            /// Extends <see cref="DeletedDatabaseFile.Kind.Deleted"/>
            /// by having image- and log files deleted.
            /// </summary>
            DeletedFully,
            /// <summary>
            /// Denotes the kind of delete is not supported or
            /// recognized.
            /// </summary>
            Unsupported
        }

        public readonly string FilePath;

        public string DatabaseName {
            get;
            private set;
        }

        public string Key {
            get;
            private set;
        }

        public Kind KindOfDelete {
            get;
            private set;
        }

        public static DeletedDatabaseFile MarkDeleted(string databaseConfigFile, string key, DeletedDatabaseFile.Kind kind) {
            if (kind == Kind.Unsupported) {
                throw new ArgumentOutOfRangeException("kind");
            }

            var extension = string.Format(".{0}{1}", key, ToFileExtension(kind));
            var renamedPath = databaseConfigFile + extension;

            try {
                File.Move(databaseConfigFile, renamedPath);
            } catch (Exception e) {
                throw ErrorCode.ToException(Error.SCERRDELETEDBRENAMECONFIG, e,
                    string.Format("Config path: \"{0}\". Tried: \"{1}\", ", databaseConfigFile, renamedPath));
            }

            return new DeletedDatabaseFile(renamedPath);
        }

        public static string ToFileExtension(DeletedDatabaseFile.Kind kind) {
            var extension = string.Empty;
            switch (kind) {
                case Kind.Removed:
                    extension = "+deleted";
                    break;
                case Kind.Deleted:
                    extension = "++deleted";
                    break;
                case Kind.DeletedFully:
                    extension = "+++deleted";
                    break;
                default:
                    break;
            }
            return extension;
        }

        public static DeletedDatabaseFile.Kind GetKindFromExtension(string file) {
            if (file.EndsWith("+++deleted")) {
                return Kind.DeletedFully;
            } else if (file.EndsWith("++deleted")) {
                return Kind.Deleted;
            } else if (file.EndsWith("+deleted")) {
                return Kind.Removed;
            }

            return Kind.Unsupported;
        }

        public static string[] GetAllFromDirectory(string directory) {
            return Directory.GetFiles(directory, DeletedDatabaseFile.DeletedFilesPattern);
        }

        /// <summary>
        /// Initializes a new <see cref="DeletedDatabaseFile"/> based
        /// on the given path to a file previously marked as deleted.
        /// </summary>
        /// <param name="file">Full path to a file previously marked
        /// deleted.</param>
        public DeletedDatabaseFile(string file) {
            this.FilePath = file;
            
            var filename = Path.GetFileName(file);
            int indexOfMetadataDelimiter = filename.LastIndexOf('.');
            var original = filename.Substring(0, indexOfMetadataDelimiter);
            var metadata = filename.Substring(indexOfMetadataDelimiter + 1);

            this.DatabaseName = original.Replace(DatabaseConfiguration.FileExtension, "");
            this.Key = metadata.Substring(0, metadata.IndexOf('+'));
            this.KindOfDelete = GetKindFromExtension(filename);
        } 
    }
}
