using Starcounter.Internal;
using System;
using System.IO;
using System.Reflection;

namespace Starcounter.Hosting {
    /// <summary>
    /// Represents a binary file the host is aware of, possibly an
    /// assembly, located in one of the application directories.
    /// </summary>
    internal sealed class PrivateBinaryFile {
        public readonly string FilePath;
        public readonly AssemblyName Name;
        public readonly DateTime Resolved;

        public PrivateBinaryFile(string path) {
            FilePath = path;
            Resolved = DateTime.Now;
            try {
                Name = AssemblyName.GetAssemblyName(path);
            } catch (BadImageFormatException) { }
        }

        public bool IsAssembly {
            get { return Name != null; }
        }

        public bool IsFromApplicaionDirectory(string dir) {
            return DirectoryExtensions.EqualDirectories(
                Path.GetDirectoryName(FilePath),
                dir);
        }
    }
}
