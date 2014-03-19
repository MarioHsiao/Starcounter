using PostSharp.Sdk.Extensibility;
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

        public Dictionary<string, ModuleLoadStrategy> OutdatedAssemblies {
            get { return null; }
        }

        private FileManager(string sourceDir, string targetDir, WeaverCache cache) {
            SourceDirectory = sourceDir;
            TargetDirectory = targetDir;
            Cache = cache;
        }

        /// <summary>
        /// Opens a <see cref="FileManager"/> with the given directories, and the
        /// specified cache. When opened, the returned manager can be used to query
        /// the set of assemblies considered outdated and also to synchronized the
        /// source and the target directores.
        /// </summary>
        /// <param name="sourceDir">The source directory.</param>
        /// <param name="targetDir">The target directory.</param>
        /// <param name="cache">The cache</param>
        /// <returns>An open file manager instance.</returns>
        public static FileManager Open(string sourceDir, string targetDir, WeaverCache cache) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Synchronize the two directories, removing all files
        /// considered obsolete from the target directory and copying
        /// all files missing from the source.
        /// </summary>
        public void Synchronize() {
            throw new NotImplementedException();
        }
    }
}
