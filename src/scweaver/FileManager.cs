using PostSharp.Sdk.Extensibility;
using Starcounter.Internal.Weaver;
using System;
using System.Collections.Generic;
using System.IO;
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
        List<string> sourceFiles;
        Dictionary<string, ModuleLoadStrategy> outdatedAssemblies;

        public readonly string SourceDirectory;
        public readonly string TargetDirectory;
        public readonly WeaverCache Cache;
        public readonly FileExclusionPolicy exclusionPolicy;

        public Dictionary<string, ModuleLoadStrategy> OutdatedAssemblies {
            get { return outdatedAssemblies; }
        }

        private FileManager(string sourceDir, string targetDir, WeaverCache cache) {
            SourceDirectory = sourceDir;
            TargetDirectory = targetDir;
            Cache = cache;
            exclusionPolicy = new FileExclusionPolicy(sourceDir);
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
            return new FileManager(sourceDir, targetDir, cache).Open();
        }

        /// <summary>
        /// Determines if the current file manager contains an assembly
        /// with the specified file name.
        /// </summary>
        /// <param name="file">The assembly file name to query.</param>
        /// <returns><c>true</c> if an assembly with the given name is
        /// in the set of files that needs to be weaved; <c>false</c>
        /// otherwise.</returns>
        public bool Contains(string file) {
            return outdatedAssemblies.ContainsKey(file);
        }

        /// <summary>
        /// Synchronize the two directories, removing all files
        /// considered obsolete from the target directory and copying
        /// all files missing from the source.
        /// </summary>
        public void Synchronize() {
            throw new NotImplementedException();
        }

        FileManager Open() {
            sourceFiles = new List<string>();
            outdatedAssemblies = new Dictionary<string, ModuleLoadStrategy>();

            sourceFiles.AddRange(Directory.GetFiles(SourceDirectory, "*.dll"));
            sourceFiles.AddRange(Directory.GetFiles(SourceDirectory, "*.exe"));

            foreach (var file in sourceFiles) {
                // If it's not excluded, and if it's not in the cache,
                // embrace it.

                if (exclusionPolicy.IsExcluded(file)) {
                    Exclude(file);
                    continue;
                }

                var cached = Cache.Get(Path.GetFileNameWithoutExtension(file));
                if (cached.Assembly != null) {
                    Reuse(file, cached);
                    continue;
                }

                WriteDebug("Unable to extract assembly \"{0}\" from the cache: {1}",
                    Path.GetFileName(file),
                    WeaverUtilities.GetExtractionFailureReason(cached)
                    );
                Include(file);
            }

            return this;
        }

        void Exclude(string file) {
            WriteInfo("Assembly {0} not processed, it's excluded by policy.", Path.GetFileName(file));

            // All excluded files in the input directory will need
            // to be mirrored in the target directory. Do that at
            // the end.
            // TODO:
        }

        void Reuse(string file, WeaverCache.CachedAssembly cachedAssembly) {
            WriteDebug("Assembly {0} not processed, it's reused from the cache.", Path.GetFileName(file));

            // It should be copied to the output directory if we are not
            // just weaving to the cache.
            // cachedAssembly.Files is what is to be copied.
            // TODO:
        }

        void Include(string file) {
            // Included files are those that need to be processed by
            // the weaver.
            var fullPath = Path.IsPathRooted(file) ? file : Path.GetFullPath(Path.Combine(SourceDirectory, Path.GetFileName(file)));
            outdatedAssemblies.Add(fullPath, new CodeWeaverModuleLoadStrategy(fullPath));
        }

        void WriteInfo(string message, params object[] parameters) {
            Program.WriteInformation(message, parameters);
        }

        void WriteDebug(string message, params object[] parameters) {
            Program.WriteDebug(message, parameters);
        }
    }
}
