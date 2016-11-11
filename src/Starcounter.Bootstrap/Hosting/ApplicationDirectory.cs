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

            var binaries = new List<FileInfo>();
            var schemas = new List<FileInfo>();

            AddArtifactFilesFromDirectory(primaryDirectory, binaries, schemas);
            foreach (var secondary in secondaryDirectories)
            {
                AddArtifactFilesFromDirectory(secondary, binaries, schemas);
            }
            
            Binaries = new PrivateBinaryFile[binaries.Count];
            int i = 0;
            foreach (var binary in binaries) {
                Binaries[i] = new PrivateBinaryFile(binary.FullName);
                i++;
            }

            schemaFiles = schemas.ToArray();
        }

        void AddArtifactFilesFromDirectory(DirectoryInfo directory, List<FileInfo> binaries, List<FileInfo> schemaFiles)
        {
            binaries.AddRange(directory.GetFiles("*.dll"));
            binaries.AddRange(directory.GetFiles("*.exe"));
            
            schemaFiles.AddRange(directory.GetFiles("*.schema"));
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
