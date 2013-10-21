// ***********************************************************************
// <copyright file="WeaverCache.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Sc.Server.Weaver.Schema;
using Starcounter;

namespace Starcounter.Internal.Weaver {
    /// <summary>
    /// Represents, and implements the functionality behind, the cache used
    /// by the Starcounter code weaver system.
    /// </summary>
    /// <remarks>This class is not thread-safe.</remarks>
    public sealed class WeaverCache {
        /// <summary>
        /// Represents the result of an assembly extracted from the weaver
        /// cache.
        /// </summary>
        public sealed class CachedAssembly {
            /// <summary>
            /// Gets the cache the assembly was requsted from.
            /// </summary>
            public readonly WeaverCache Cache;

            /// <summary>
            /// Gets the name of the assembly requested.
            /// </summary>
            public readonly string Name;

            /// <summary>
            /// Gets the assembly if the extraction was considered a success.
            /// If it was not, this field is null and other properties will
            /// reveal the reason why the assembly was not extracted.
            /// </summary>
            /// <value>The assembly.</value>
            public DatabaseAssembly Assembly { get; internal set; }

            /// <summary>
            /// Gets a value indicating if the assembly was looked for but was
            /// not found in the cache. False means either it was not queried
            /// for (because of some previous failure, or the cache being
            /// disabled) or it was found.
            /// </summary>
            /// <value><c>true</c> if [not found]; otherwise, <c>false</c>.</value>
            public bool NotFound { get; internal set; }

            /// <summary>
            /// Gets a value indicating if a transformed representation of the
            /// cached assembly was looked for but was not found in the cache.
            /// False means either it was not queried for (because of some previous
            /// failure, or the cache being disabled) or it was found.
            /// </summary>
            /// <value><c>true</c> if [transformation not found]; otherwise, <c>false</c>.</value>
            public bool TransformationNotFound { get; internal set; }

            /// <summary>
            /// Gets a value indicating if a transformed representation of the
            /// cached assembly was outdated. False means either it was not
            /// queried for (because of some previous failure, or the cache being
            /// disabled) or it was up-to-date.
            /// </summary>
            /// <value><c>true</c> if [transformation outdated]; otherwise, <c>false</c>.</value>
            public bool TransformationOutdated { get; internal set; }

            /// <summary>
            /// Gets a reference to an exception happening when trying to
            /// deserialize the assembly from the cache.
            /// </summary>
            /// <value>The deserialization exception.</value>
            public Exception DeserializationException { get; internal set; }

            /// <summary>
            /// Gets the name of an assembly this cached assembly depend upon
            /// that was out of date, i.e. it was hashed but the value didn't
            /// match the one in the cache.
            /// </summary>
            /// <value>The broken dependency.</value>
            public string BrokenDependency { get; internal set; }

            /// <summary>
            /// Gets the name of an assembly this cached assembly depend upon
            /// that was not found.
            /// </summary>
            /// <value>The missing dependency.</value>
            public string MissingDependency { get; internal set; }

            /// <summary>
            /// Initializes an instance of <see cref="CachedAssembly" />.
            /// </summary>
            /// <param name="cache">The cache.</param>
            /// <param name="name">The name.</param>
            internal CachedAssembly(WeaverCache cache, string name) {
                this.Cache = cache;
                this.Name = name;
                this.Assembly = null;
                this.NotFound = false;
                this.DeserializationException = null;
                this.BrokenDependency = null;
                this.MissingDependency = null;
            }
        }

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
        /// <param name="cacheDirectory">The directory possibly holding cached files.</param>
        /// <exception cref="System.IO.DirectoryNotFoundException"></exception>
        /// <remarks>After a schema instance has been created, it includes the
        /// built-in Starcounter assembly only. To populate the cache,
        /// use the <see cref="Extract" /> method.</remarks>
        public WeaverCache(string cacheDirectory) {
            if (!Directory.Exists(cacheDirectory))
                throw new DirectoryNotFoundException(string.Format("Directory {0} does not exist."));

            this.CacheDirectory = cacheDirectory;
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
        /// the deserialized schema of the cached assembly.</returns>
        public CachedAssembly Get(string assemblyName) {
            return Extract(assemblyName, null);
        }

        /// <summary>
        /// Tries to extract an assembly by name.
        /// </summary>
        /// <param name="assemblyName">Name of the assembly to extract.
        /// The name expected is the simple name of the assembly, not
        /// including the extension.</param>
        /// <param name="targetDirectory">Target directory where the assembly is to be extracted to.
        /// If null, and the extraction is considered a success, the
        /// assembly is read into the cached schema but not copied.</param>
        /// <returns>An object describing the way the assembly was
        /// known to the cache, i.e. if it was not found, if it was
        /// out of date, etc. If it was up to date, the Assembly
        /// property of the returned object is not null and includes
        /// the deserialized schema of the cached assembly.</returns>
        /// <exception cref="System.ArgumentNullException">assemblyName</exception>
        public CachedAssembly Extract(string assemblyName, string targetDirectory) {
            CachedAssembly result;
            DatabaseAssembly candidate;
            string schemaFile;
            string assemblyFile;
            string debugSymbolsFile;
            string debugSymbolsPath;
            string targetFilePath;
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

                // The transformed result is there and it's up to date.
                // The cached file is considered usable and we should return
                // it with the result.
                // Just check first if we need to copy it (along with a
                // possibly program debug file) to a target directory first.

                if (!string.IsNullOrEmpty(targetDirectory)) {
                    targetFilePath = Path.Combine(targetDirectory, Path.GetFileName(assemblyFile));
                    File.Copy(assemblyFile, targetFilePath, true);

                    debugSymbolsFile = string.Concat(Path.GetFileNameWithoutExtension(assemblyFile), ".pdb");
                    debugSymbolsPath = Path.Combine(this.CacheDirectory, debugSymbolsFile);

                    if (File.Exists(debugSymbolsPath)) {
                        targetFilePath = Path.Combine(targetDirectory, debugSymbolsFile);
                        File.Copy(debugSymbolsPath, targetFilePath, true);
                    }
                }
            }

            // Add it to the schema and assign the candidate binary to the
            // result before we finally return.

            candidate.SetIsLoadedFromCache();
            result.Assembly = candidate;
            schema.Assemblies.Add(candidate);

            return result;
        }

        /// <summary>
        /// Gets a value indicating if an assembly named <paramref name="assemblyName" />
        /// is part of the cached schema.
        /// </summary>
        /// <param name="assemblyName">The name of the assembly to be checked.</param>
        /// <returns>True if an assembly with the given name was successfully
        /// extraced; false if not.</returns>
        public bool IsExtracted(string assemblyName) {
            return this.schema.Assemblies.FirstOrDefault(assembly => assembly.Name == assemblyName) != null;
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