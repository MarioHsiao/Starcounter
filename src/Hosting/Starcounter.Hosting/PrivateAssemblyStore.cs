
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Hosting {
    /// <summary>
    /// Represents the collection of private assemblies the code host is
    /// aware of, based on loaded applications.
    /// </summary>
    internal sealed class PrivateAssemblyStore {
        readonly List<string> applicationDirectories = new List<string>();
        readonly Dictionary<string, PrivateBinaryFile> fileToIdentity = new Dictionary<string, PrivateBinaryFile>(StringComparer.InvariantCultureIgnoreCase);

        public void RegisterApplicationDirectory(ApplicationDirectory dir) {
            applicationDirectories.Add(dir.Path);
            foreach (var binary in dir.Binaries) {
                fileToIdentity.Add(binary.FilePath, binary);
            }
        }

        /// <summary>
        /// Evaluates the given <paramref name="applicationDirectory"/> to see
        /// if it is a directory previously registered with the current store.
        /// </summary>
        /// <param name="applicationDirectory">The directory to look for.</param>
        /// <returns><c>true</c> if the given directory is a known application
        /// directory; <c>false</c> otherwise.</returns>
        public bool IsApplicationDirectory(string applicationDirectory) {
            return applicationDirectories.FirstOrDefault((candidate) => {
                return EqualDirectories(candidate, applicationDirectory);
            }) != null;
        }

        public AssemblyName GetAssembly(string filePath) {
            var record = fileToIdentity[filePath];
            if (!record.IsAssembly) throw new BadImageFormatException();
            return record.Name;
        }

        public PrivateBinaryFile[] GetAssemblies(string assemblyName) {
            var assemblies = fileToIdentity.Values.Where((candidate) => {
                return candidate.IsAssembly && candidate.Name.Name == assemblyName;
            });
            return assemblies.OrderBy(file => file.Resolved).ToArray();
        }

        public static bool EqualDirectories(string dir1, string dir2) {
            return string.Compare(
                Path.GetFullPath(dir1).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                Path.GetFullPath(dir2).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                StringComparison.CurrentCultureIgnoreCase) == 0;
        }
    }
}
