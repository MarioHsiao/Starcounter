using System.Collections.Generic;
using System.IO;

namespace Starcounter.Internal.Weaver.Cache
{
    /// <summary>
    /// Represent a set of files in the weaver cache, defining
    /// all artifacts a cached assembly consist of.
    /// </summary>
    public sealed class CachedAssemblyFiles
    {
        readonly List<string> artifacts;
        readonly string name;

        /// <summary>
        /// Gets the full path of each artifact.
        /// </summary>
        public IEnumerable<string> ArtifactPaths {
            get {
                return artifacts;
            }
        }

        /// <summary>
        /// Gets the logical name of this file set.
        /// </summary>
        public string Name {
            get {
                return name;
            }
        }

        /// <summary>
        /// Gets a value indicating if the cached assembly is a
        /// lone schema file, and does not include any binary files.
        /// </summary>
        public bool IsSchemaOnly {
            get {
                return artifacts != null && artifacts.Count == 1;
            }
        }

        /// <summary>
        /// Gets a set of <see cref="CachedAssemblyFiles"/> representing the artifact set
        /// of every cached assembly in a given weaver cache directory.
        /// </summary>
        /// <param name="cacheDirectory">The weaver cache directory.</param>
        /// <returns>Set of artifacts.</returns>
        public static IEnumerable<CachedAssemblyFiles> GetAllFromCacheDirectory(string cacheDirectory)
        {
            Guard.DirectoryExists(cacheDirectory, nameof(cacheDirectory));

            var schemas = Directory.GetFiles(cacheDirectory, "*.schema");
            return CreateSetFromSchemaFiles(schemas);
        }

        /// <summary>
        /// Gets a set of <see cref="CachedAssemblyFiles"/> representing the artifact set
        /// of every cached assembly in a given weaver cache directory, filtered by corresponding
        /// input assemblies in a given source directory.
        /// </summary>
        /// <param name="cacheDirectory">The weaver cache directory.</param>
        /// <param name="sourceDirectory">Directory whose assemblies define a filter of cached
        /// artifacts to include in the result.</param>
        /// <returns>Set of artifacts.</returns>
        public static IEnumerable<CachedAssemblyFiles> GetAllFromCacheDirectoryMatchingSources(
            string cacheDirectory,
            string sourceDirectory)
        {
            Guard.DirectoryExists(cacheDirectory, nameof(cacheDirectory));
            Guard.DirectoryExists(sourceDirectory, nameof(sourceDirectory));

            var sourceAssemblies = new List<string>();
            sourceAssemblies.AddRange(Directory.GetFiles(sourceDirectory, "*.dll"));
            sourceAssemblies.AddRange(Directory.GetFiles(sourceDirectory, "*.exe"));

            var schemaFiles = new List<string>();

            foreach (var sourceFile in sourceAssemblies)
            {
                var sourceName = Path.GetFileNameWithoutExtension(sourceFile);
                var schemaFile = Path.Combine(cacheDirectory, sourceName + ".schema");
                if (File.Exists(schemaFile))
                {
                    schemaFiles.Add(schemaFile);
                }
            }

            return CreateSetFromSchemaFiles(schemaFiles);
        }

        static IEnumerable<CachedAssemblyFiles> CreateSetFromSchemaFiles(IEnumerable<string> schemaFiles)
        {
            var result = new List<CachedAssemblyFiles>();

            foreach (var schema in schemaFiles)
            {
                var assembly = Path.ChangeExtension(schema, ".dll");
                if (!File.Exists(assembly))
                {
                    assembly = Path.ChangeExtension(schema, ".exe");
                    if (!File.Exists(assembly))
                    {
                        // It's a schema-only assembly
                        assembly = null;
                    }
                }

                var f = new CachedAssemblyFiles(schema, assembly);
                result.Add(f);
            }

            return result;
        }

        /// <summary>
        /// Initialize a new <see cref="CachedAssemblyFiles"/> instance.
        /// </summary>
        /// <param name="schemaPath">Full path to the schema file.</param>
        /// <param name="transformedAssemblyPath">Optional full path to the transformed
        /// assembly, if such exist, or null otherwise.</param>
        public CachedAssemblyFiles(string schemaPath, string transformedAssemblyPath)
        {
            Guard.FileExists(schemaPath, nameof(schemaPath));

            name = Path.GetFileNameWithoutExtension(schemaPath);

            artifacts = new List<string>();
            artifacts.Add(schemaPath);

            if (!string.IsNullOrEmpty(transformedAssemblyPath))
            {
                Guard.FileExists(transformedAssemblyPath, nameof(transformedAssemblyPath));
                artifacts.Add(transformedAssemblyPath);
                var pdb = Path.ChangeExtension(transformedAssemblyPath, ".pdb");
                if (File.Exists(pdb))
                {
                    artifacts.Add(pdb);
                }
            }
        }

        public void CopyTo(string targetDirectory, bool overwriteExistingArtifacts = false)
        {
            Guard.DirectoryExists(targetDirectory, nameof(targetDirectory));

            foreach (var artifact in artifacts)
            {
                var fileName = Path.GetFileName(artifact);
                var targetName = Path.Combine(targetDirectory, fileName);

                File.Copy(artifact, targetName, overwriteExistingArtifacts);
            }
        }
    }
}