
using PostSharp.Sdk.Extensibility;
using Starcounter.Internal;
using Starcounter.Internal.Weaver;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Starcounter.Weaver {
    
    /// <summary>
    /// Governs the management of files to be processed by the weaver
    /// and that the source directory is always synchronized with the
    /// target directory after a successfull weaving session.
    /// </summary>
    public class FileManager {
        readonly IWeaverHost host;
        List<string> sourceFiles;
        Dictionary<string, ModuleLoadStrategy> outdatedAssemblies;
        List<string> filesToCopy;
        List<string> presentTargetFiles;

        /// <summary>
        /// Index of the first edition library file, if any. The
        /// index is based on the <see cref="sourceFiles"/> list.
        /// </summary>
        int editionLibriesIndex;

        public readonly string SourceDirectory;
        public readonly string TargetDirectory;
        public readonly WeaverCache Cache;
        public readonly FileExclusionPolicy exclusionPolicy;

        public Dictionary<string, ModuleLoadStrategy> OutdatedAssemblies {
            get { return outdatedAssemblies; }
        }

        public DatabaseTypeConfiguration TypeConfiguration {
            get;
            private set;
        }

        private FileManager(IWeaverHost weaverHost, string sourceDir, string targetDir, WeaverCache cache) {
            host = weaverHost;
            SourceDirectory = sourceDir;
            TargetDirectory = targetDir;
            Cache = cache;
            sourceFiles = new List<string>();
            outdatedAssemblies = new Dictionary<string, ModuleLoadStrategy>();
            filesToCopy = new List<string>();
            exclusionPolicy = new FileExclusionPolicy(weaverHost, sourceDir);
            presentTargetFiles = new List<string>();
            editionLibriesIndex = -1;
        }

        /// <summary>
        /// Opens a <see cref="FileManager"/> with the given directories, and the
        /// specified cache. When opened, and after <see cref="BuildState"/> has
        /// been invoked, the returned manager can be used to query the set of 
        /// assemblies considered outdated and also to synchronized the source and
        /// the target directores.
        /// </summary>
        /// <param name="host">The weaver host</param>
        /// <param name="sourceDir">The source directory.</param>
        /// <param name="targetDir">The target directory.</param>
        /// <param name="cache">The cache</param>
        /// <returns>An open file manager instance.</returns>
        public static FileManager Open(IWeaverHost host, string sourceDir, string targetDir, WeaverCache cache) {
            return new FileManager(host, sourceDir, targetDir, cache).Open();
        }

        /// <summary>
        /// Builds up the state and prepare the manager for use, based on what was
        /// was discovered when opened.
        /// </summary>
        public void BuildState() {
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

                WriteDebug("Not reusing cached assembly \"{0}\": {1}",
                    Path.GetFileName(file),
                    WeaverUtilities.GetExtractionFailureReason(cached)
                    );
                Include(file);
            }
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

        public bool IsEditionLibrary(string file) {
            var result = false;
            if (editionLibriesIndex != -1) {
                result = sourceFiles.IndexOf(file, editionLibriesIndex) != -1;
            }
            return result;
        }

        /// <summary>
        /// Synchronize the two directories, removing all files
        /// considered obsolete from the target directory and copying
        /// all files missing from the source.
        /// </summary>
        public void Synchronize() {
            MirrorFilesToCopy();
            RemoveFilesAbandoned();
        }

        public void BootDiagnose() {
            exclusionPolicy.BootDiagnose();

            host.WriteDebug("File manager:");

            var props = new Dictionary<string, string>();
            props["Source directory"] = this.SourceDirectory;
            props["Target directory"] = this.TargetDirectory;

            foreach (var pair in props) {
                host.WriteDebug("  {0}: {1}", pair.Key, pair.Value);
            }

            // Group files by directory

            host.WriteDebug("  {0} input files considered.", sourceFiles.Count);

            var filesByDirectory = new FilesByDirectory(sourceFiles).Files;
            foreach (var hive in filesByDirectory) {
                host.WriteDebug("  {0}:", hive.Key);
                foreach (var file in hive.Value) {
                    host.WriteDebug("    {0}:", file);
                }
            }
        }

        FileManager Open() {
            TypeConfiguration = DatabaseTypeConfiguration.Open(SourceDirectory);

            sourceFiles.AddRange(Directory.GetFiles(SourceDirectory, "*.dll"));
            sourceFiles.AddRange(Directory.GetFiles(SourceDirectory, "*.exe"));

            // Assure we always add edition libraries LAST - this is what we depend
            // on to avoid having to weave them when they are not referenced
            var editionLibraries = CodeWeaver.Current.EditionLibrariesDirectory;

            if (Directory.Exists(editionLibraries) && (!CodeWeaver.Current.DisableEditionLibraries)) {
                var libs = Directory.GetFiles(editionLibraries, "*.dll");
                AddEditionLibraries(libs, sourceFiles);
            }

            // Adding libraries from database classes libraries directory.
            var librariesWithDbClasses = CodeWeaver.Current.LibrariesWithDatabaseClassesDirectory;

            if (Directory.Exists(librariesWithDbClasses)) {
                var libs = Directory.GetFiles(librariesWithDbClasses, "*.dll");
                AddEditionLibraries(libs, sourceFiles);
            }
            
            presentTargetFiles.AddRange(Directory.GetFiles(TargetDirectory, "*.dll"));
            presentTargetFiles.AddRange(Directory.GetFiles(TargetDirectory, "*.exe"));
            presentTargetFiles.AddRange(Directory.GetFiles(TargetDirectory, "*.pdb"));
            presentTargetFiles.AddRange(Directory.GetFiles(TargetDirectory, "*.schema"));
            var targetConfigFile = new FileInfo(Path.Combine(TargetDirectory, DatabaseTypeConfiguration.TypeConfigFileName));
            if (targetConfigFile.Exists) {
                presentTargetFiles.Add(targetConfigFile.FullName);
            }

            var typeConfig = TypeConfiguration.FilePath;
            if (typeConfig != null) {
                filesToCopy.Add(typeConfig);
                if (!targetConfigFile.Exists) {
                    // If the type configuration file was added, we have to
                    // invalidate every assembly in the cache. (If it is removed,
                    // it is already tracked by the weaver assembly dependency
                    // tracking mechanism).
                    Cache.Disabled = true;
                }
            }

            return this;
        }

        void Exclude(string file) {
            WriteInfo("Not processing assembly {0}: it's excluded by policy.", Path.GetFileName(file));

            filesToCopy.Add(file);
            var pdb = Path.ChangeExtension(file, ".pdb");
            if (File.Exists(pdb)) {
                filesToCopy.Add(pdb);
            }
        }

        void Reuse(string file, WeaverCache.CachedAssembly cachedAssembly) {
            WriteDebug("Not processing assembly {0}: it's reused from the cache.", Path.GetFileName(file));
            if (!cachedAssembly.IsSchemaOnly) {
                filesToCopy.AddRange(cachedAssembly.Files);
            }
        }

        void Include(string file) {
            var fullPath = Path.IsPathRooted(file) ? file : Path.GetFullPath(Path.Combine(SourceDirectory, Path.GetFileName(file)));
            outdatedAssemblies.Add(fullPath, new CodeWeaverModuleLoadStrategy(fullPath));
        }

        void MirrorFilesToCopy() {
            foreach (var item in filesToCopy) {
                var target = Path.GetFullPath(Path.Combine(TargetDirectory, Path.GetFileName(item)));
                if (!File.Exists(target) || File.GetLastWriteTime(target) < File.GetLastWriteTime(item)) {
                    File.Copy(item, target, true);
                }
            }
        }

        void RemoveFilesAbandoned() {
            var expectedFiles = new List<string>();
            filesToCopy.ForEach((f) => { expectedFiles.Add(Path.GetFileName(f)); });
            foreach (var item in outdatedAssemblies.Keys) {
                var fileName = Path.GetFileName(item);
                expectedFiles.Add(fileName);
                expectedFiles.Add(Path.ChangeExtension(fileName, ".pdb"));
                expectedFiles.Add(Path.ChangeExtension(fileName, ".schema"));
            }

            presentTargetFiles.ForEach((targetFile) => {
                if (!expectedFiles.Contains(Path.GetFileName(targetFile))) {
                    WriteDebug("Removing abandoned file {0}", targetFile);
                    File.Delete(targetFile);
                }
            });
        }

        void AddEditionLibraries(string[] editionLibraries, List<string> sourceFiles) {
            if (editionLibraries.Length > 0) {
                // Detect any duplicate before adding
                // TODO:

                var duplicates = sourceFiles.FindAll((s) => {
                    var duplicate = editionLibraries.FirstOrDefault(
                        e => Path.GetFileName(e).Equals(Path.GetFileName(s), System.StringComparison.InvariantCultureIgnoreCase));
                    return duplicate != null;
                });

                if (duplicates.Count > 0) {
                    // We found at least one duplicate. Convert full path
                    // names to simple file name and add them to the error
                    // message.
                    duplicates = duplicates.ConvertAll<string>(f => Path.GetFileName(f));
                    var msg = string.Format("{0} duplicate(s): {1}", duplicates.Count, string.Join(";", duplicates));
                    throw ErrorCode.ToException(Error.SCERRAPPDEPLOYEDEDITIONLIBRARY, msg);
                }

                editionLibriesIndex = sourceFiles.Count;
                sourceFiles.AddRange(editionLibraries);
            }
        }

        void WriteInfo(string message, params object[] parameters) {
            host.WriteInformation(message, parameters);
        }
        
        void WriteDebug(string message, params object[] parameters) {
            host.WriteDebug(message, parameters);
        }
    }
}
