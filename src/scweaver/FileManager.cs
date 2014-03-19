using Starcounter.Internal.Weaver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Weaver {
    /// <summary>
    /// Governs the management of files to be processed by the weaver
    /// and that the source directory is always synchronized with the
    /// target directory after a successfull weaving session.
    /// </summary>
    internal class FileManager {
        public readonly string SourceDirectory;
        public readonly string TargetDirectory;
        public readonly WeaverCache Cache;

        public FileManager(string sourceDir, string targetDir, WeaverCache cache) {
            SourceDirectory = sourceDir;
            TargetDirectory = targetDir;
            Cache = cache;
        }
    }
}
