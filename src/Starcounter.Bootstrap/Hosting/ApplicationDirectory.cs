using System.Collections.Generic;
using System.IO;

namespace Starcounter.Hosting
{
    /// <summary>
    /// Represent the application directory of an application that has been
    /// requested to launch into a host.
    /// </summary>
    public class ApplicationDirectory {
        readonly FileInfo[] schemaFiles;

        /// <summary>
        /// Full path to the primary application directory.
        /// </summary>
        public readonly string Path;

        /// <summary>
        /// Set of binaries that have been resolved from the current
        /// directory.
        /// </summary>
        internal readonly PrivateBinaryFile[] Binaries;

        /// <summary>
        /// Initialize a new <see cref="ApplicationDirectory"/>.
        /// </summary>
        /// <param name="primaryDirectory">The directory to initialize from.</param>
        /// <param name="secondaryDirectories">Optional secondary directories.</param>
        public ApplicationDirectory(DirectoryInfo primaryDirectory, IEnumerable<DirectoryInfo> secondaryDirectories = null) {
            Path = primaryDirectory.FullName;

            var binaries = new Dictionary<string, FileInfo>();
            var schemas = new Dictionary<string, FileInfo>();

            AddArtifactFilesFromDirectory(primaryDirectory, binaries, schemas);
            foreach (var secondary in secondaryDirectories)
            {
                AddArtifactFilesFromDirectory(secondary, binaries, schemas);
            }
            
            Binaries = new PrivateBinaryFile[binaries.Count];
            int i = 0;
            foreach (var binary in binaries.Values) {
                Binaries[i] = new PrivateBinaryFile(binary.FullName);
                i++;
            }

            schemaFiles = new FileInfo[schemas.Count];
            schemas.Values.CopyTo(schemaFiles, 0);
        }

        void AddArtifactFilesFromDirectory(DirectoryInfo directory, Dictionary<string, FileInfo> binaries, Dictionary<string, FileInfo> schemaFiles)
        {
            AddAllFilesNotContainedByName(directory.GetFiles("*.dll"), binaries);
            AddAllFilesNotContainedByName(directory.GetFiles("*.exe"), binaries);
            AddAllFilesNotContainedByName(directory.GetFiles("*.schema"), schemaFiles);
        }

        void AddAllFilesNotContainedByName(FileInfo[] files, Dictionary<string, FileInfo> filesByName)
        {
            foreach (var file in files)
            {
                if (!filesByName.ContainsKey(file.Name))
                {
                    filesByName.Add(file.Name, file);
                }
                else
                {
                    // Trace or log this, maybe even a notice.
                    // TODO:
                }
            }
        }
        
        /// <summary>
        /// Return a list of schema files from the given virtual directory.
        /// </summary>
        /// <returns>List of schema files.</returns>
        public IEnumerable<FileInfo> GetApplicationSchemaFiles()
        {
            return schemaFiles;
        }

        /// <summary>
        /// Return a list of schema files found in a specific directory.
        /// </summary>
        /// <param name="directory">The target directory to look in.</param>
        /// <returns>List of schema files</returns>
        public static IEnumerable<FileInfo> GetApplicationSchemaFilesFromDirectory(string directory)
        {
            var dir = new DirectoryInfo(directory);
            return dir.GetFiles("*.schema");
        }
    }
}
