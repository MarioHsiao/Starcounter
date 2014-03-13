
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
        readonly Dictionary<string, PrivateBinaryFile> fileToIdentity = new Dictionary<string, PrivateBinaryFile>();
        class PrivateBinaryFile {
            public AssemblyName Name;
            public DateTime Resolved;

            public bool IsAssembly { 
                get { return Name != null; } 
            }
        }
        
        // What directories have we got in store?
        // What assembly name does each file include (and when
        // did we resolve that)?
        // full_file_path -> info (assembly name, datetime)
        
        public void RegisterApplicationDirectory(DirectoryInfo dir) {
            var binaries = new List<FileInfo>();
            binaries.AddRange(dir.GetFiles("*.dll"));
            binaries.AddRange(dir.GetFiles("*.exe"));

            foreach (var binary in binaries) {
                var record = new PrivateBinaryFile() { Resolved = DateTime.Now };
                try {
                    record.Name = AssemblyName.GetAssemblyName(binary.FullName);
                } catch (BadImageFormatException) {}

                fileToIdentity.Add(binary.FullName, record);
            }
        }

        public object Assemblies {
            get {
                return fileToIdentity.Values.Where((f) => { return f.IsAssembly; });
            }
        }

        public AssemblyName GetAssembly(string filePath) {
            var record = fileToIdentity[filePath];
            if (!record.IsAssembly) throw new BadImageFormatException();
            return record.Name;
        }
    }
}
