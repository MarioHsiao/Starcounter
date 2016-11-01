﻿
using Sc.Server.Weaver.Schema;
using Starcounter.Weaver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Starcounter.Internal.Weaver.Cache
{
    /// <summary>
    /// Represents, and implements the functionality behind, the cache used
    /// by the Starcounter code weaver system.
    /// </summary>
    /// <remarks>This class is not thread-safe.</remarks>
    public sealed class WeaverCache
    {
        readonly IWeaverHost host;

        /// <summary>
        /// Schema referencing all assemblies successfully extracted.
        /// </summary>
        private DatabaseSchema schema;

        /// <summary>
        /// Dictionary of hashes for assemblies evaluated during the
        /// resolving the validity of a cached assembly. The key is the
        /// name of the assembly, the value it's hash.
        /// </summary>
        private Dictionary<string, string> hashedAssemblies;

        /// <summary>
        /// Gets the directory where cached files are stored.
        /// </summary>
        /// <value>The cache directory.</value>
        public string CacheDirectory { get; private set; }

        /// <summary>
        /// Gets a list of path strings pointing to directories the
        /// cache should utilize when it needs to find the binary of
        /// a referenced dependency assembly.
        /// </summary>
        /// <value>The assembly search directories.</value>
        public List<string> AssemblySearchDirectories { get; private set; }

        /// <summary>
        /// Indicates the cache is disabled. Every attempt to extract an
        /// assembly from the cache will be ignored.
        /// </summary>
        /// <value><c>true</c> if disabled; otherwise, <c>false</c>.</value>
        public bool Disabled { get; set; }

        /// <summary>
        /// Gets the schema extracted from the cache.
        /// </summary>
        /// <value>The schema.</value>
        public DatabaseSchema Schema {
            get {
                return this.schema;
            }
        }

        /// <summary>
        /// Initializes a <see cref="WeaverCache" /> instance.
        /// </summary>
        /// <param name="weaverHost">The weaver host</param>
        /// <param name="cacheDirectory">The directory possibly holding cached files.</param>
        /// <exception cref="System.IO.DirectoryNotFoundException"></exception>
        /// <remarks>After a schema instance has been created, it includes the
        /// built-in Starcounter assembly only. To populate the cache,
        /// use the <see cref="Extract" /> method.</remarks>
        public WeaverCache(IWeaverHost weaverHost, string cacheDirectory) {
            if (!Directory.Exists(cacheDirectory))
                throw new DirectoryNotFoundException(string.Format("Directory {0} does not exist."));

            host = weaverHost;
            CacheDirectory = cacheDirectory;

            Initialize();
        }

        /// <summary>
        /// Gets an assembly from the cache by name.
        /// </summary>
        /// <param name="assemblyName">Name of the assembly to get.
        /// The name expected is the simple name of the assembly, not
        /// including the extension.</param>
        /// <returns>An object describing the way the assembly was
        /// known to the cache, i.e. if it was not found, if it was
        /// out of date, etc. If it was up to date, the Assembly
        /// property of the returned object is not null and includes
        /// the deserialized schema of the cached assembly.
        /// <para>
        /// If there was a match, the contained <see cref="Schema"/>
        /// will also reflect the retreived (deserialized) assembly
        /// metadata as part of its assembly set.
        /// </para></returns>
        public CachedAssembly Get(string assemblyName) {
            CachedAssembly result;
            DatabaseAssembly candidate;
            string schemaFile;
            string assemblyFile;
            string dependencyHash;

            if (string.IsNullOrEmpty(assemblyName))
                throw new ArgumentNullException("assemblyName");

            result = new CachedAssembly(this, assemblyName);

            // If the cache is disabled, we just return the
            // result, and the assembly is considered not found.

            if (this.Disabled) return result;

            // Try finding a schema file.

            schemaFile = Path.Combine(this.CacheDirectory, string.Concat(assemblyName, ".schema"));
            if (!File.Exists(schemaFile)) {
                result.NotFound = true;
                return result;
            }

            // Try deserializing the schema from the file on disk, to
            // materialize the assembly metadata object.

            try {
                candidate = DatabaseAssembly.Deserialize(schemaFile);
            } catch (Exception e) {
                result.DeserializationException = e;
                return result;
            }

            // Evaluate dependencies

            foreach (KeyValuePair<string, string> dependency in candidate.Dependencies) {
                dependencyHash = GetAssemblyHash(dependency.Key);
                if (dependencyHash == null) {
                    result.MissingDependency = dependency.Key;
                    return result;
                } else if (!dependencyHash.Equals(dependency.Value)) {
                    result.BrokenDependency = dependency.Key;
                    return result;
                }
            }

            // All dependencies were up to date.
            // Check if the cached file indicates it was transformed. If it was,
            // we need to find and evaluate the transformed result too.

            assemblyFile = null;
            if (candidate.IsTransformed) {
                // Check if we can find the cached assembly file.
                // If not, we can not use the cached result.

                assemblyFile = Path.Combine(this.CacheDirectory, string.Concat(assemblyName, ".dll"));
                if (!File.Exists(assemblyFile)) {
                    assemblyFile = Path.Combine(this.CacheDirectory, string.Concat(assemblyName, ".exe"));
                    if (!File.Exists(assemblyFile)) {
                        result.TransformationNotFound = true;
                        return result;
                    }
                }

                // Likewise, if the timestamps of the schema file and the
                // assembly transformation does not match, we can not use
                // the cached result either. Check it.

                if (File.GetLastWriteTime(schemaFile) != File.GetLastWriteTime(assemblyFile)) {
                    result.TransformationOutdated = true;
                    return result;
                }
            }

            // Let the result contain the list of files considered cached
            // for the given assembly.

            result.Files = new CachedAssemblyFiles(schemaFile, candidate.IsTransformed ? assemblyFile : null);
            
            // Add it to the schema and assign the candidate binary to the
            // result before we finally return.

            candidate.SetIsLoadedFromCache();
            result.Assembly = candidate;
            schema.Assemblies.Add(candidate);

            return result;
        }

        internal void BootDiagnose() {
            host.WriteDebug("Weaver cache:");

            var props = new Dictionary<string, string>();
            props["Cache directory"] = this.CacheDirectory;
            props["Disabled"] = this.Disabled.ToString();

            foreach (var pair in props) {
                host.WriteDebug("  {0}: {1}", pair.Key, pair.Value);
            }

            host.WriteDebug("  {0} search directories:", this.AssemblySearchDirectories.Count);
            foreach (var dir in this.AssemblySearchDirectories) {
                host.WriteDebug("  " + dir);
            }
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        private void Initialize() {
            this.schema = new DatabaseSchema();
            this.schema.AddStarcounterAssembly();
            this.AssemblySearchDirectories = new List<string>();
        }

        /// <summary>
        /// Gets the hash value of the assembly with the given name. If the
        /// assembly could not be found, null is returned.
        /// </summary>
        /// <param name="assemblyName">The name of the assembly whose hash we need.</param>
        /// <returns>Hash value of the given assembly, or null if the an
        /// assembly with the given name could not be found.</returns>
        private string GetAssemblyHash(string assemblyName) {
            Assembly starcounterAssembly;
            string assemblyPath;
            string hash;
            bool found;

            hash = null;
            assemblyPath = null;

            if (hashedAssemblies == null) {
                hashedAssemblies = new Dictionary<string, string>(32, StringComparer.InvariantCultureIgnoreCase);
                starcounterAssembly = typeof(Db).Assembly;
                hash = HashHelper.ComputeHash(starcounterAssembly.Location);
                hashedAssemblies[starcounterAssembly.ManifestModule.Name] = hash;
            }

            found = hashedAssemblies.TryGetValue(assemblyName, out hash);
            if (found) return hash;

            // The assembly was not hashed. Try finding it to hash it.
            // Look in all the directories specified and in the set of
            // loaded assemblies too.

            foreach (var referenceDirectory in this.AssemblySearchDirectories) {
                assemblyPath = Path.Combine(referenceDirectory, assemblyName);
                if (File.Exists(assemblyPath)) {
                    found = true;
                    break;
                }
            }

            if (!found) {
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                    if (assembly.ManifestModule.Name == assemblyName) {
                        assemblyPath = assembly.Location;
                        found = true;
                        break;
                    }
                }
            }

            if (found) {
                hash = HashHelper.ComputeHash(assemblyPath);
                hashedAssemblies.Add(assemblyName, hash);
            }

            return hash;
        }
    }
}