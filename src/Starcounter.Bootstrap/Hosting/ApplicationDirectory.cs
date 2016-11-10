using System.Collections.Generic;
using System.IO;

namespace Starcounter.Hosting {
    /// <summary>
    /// Represent the application directory of an application that has been
    /// requested to launch into a host.
    /// </summary>
    public sealed class ApplicationDirectory {
        /// <summary>
        /// Full path to the directory.
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
        /// <param name="directory">The directory to initialize from.</param>
        public ApplicationDirectory(DirectoryInfo directory) {
            Path = directory.FullName;

            var binaries = new List<FileInfo>();
            binaries.AddRange(directory.GetFiles("*.dll"));
            binaries.AddRange(directory.GetFiles("*.exe"));

            Binaries = new PrivateBinaryFile[binaries.Count];
            int i = 0;
            foreach (var binary in binaries) {
                Binaries[i] = new PrivateBinaryFile(binary.FullName);
                i++;
            }
        }
    }
}
