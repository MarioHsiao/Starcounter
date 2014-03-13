
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
        readonly Dictionary<string, PrivateBinaryFile> fileToIdentity = new Dictionary<string, PrivateBinaryFile>();
        class PrivateBinaryFile {
            public AssemblyName Name;
            public DateTime Resolved;

            public bool IsAssembly { 
                get { return Name != null; } 
            }
        }
        
        public void RegisterApplicationDirectory(DirectoryInfo dir) {
            var binaries = new List<FileInfo>();
            binaries.AddRange(dir.GetFiles("*.dll"));
            binaries.AddRange(dir.GetFiles("*.exe"));

            applicationDirectories.Add(dir.FullName);
            foreach (var binary in binaries) {
                var record = new PrivateBinaryFile() { Resolved = DateTime.Now };
                try {
                    record.Name = AssemblyName.GetAssemblyName(binary.FullName);
                } catch (BadImageFormatException) {}

                fileToIdentity.Add(binary.FullName, record);
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
                return string.Compare(
                Path.GetFullPath(candidate).TrimEnd('\\'),
                Path.GetFullPath(applicationDirectory).TrimEnd('\\'),
                StringComparison.CurrentCultureIgnoreCase) == 0;
            }) != null;
        }

        public AssemblyName GetAssembly(string filePath) {
            var record = fileToIdentity[filePath];
            if (!record.IsAssembly) throw new BadImageFormatException();
            return record.Name;
        }
    }
}
