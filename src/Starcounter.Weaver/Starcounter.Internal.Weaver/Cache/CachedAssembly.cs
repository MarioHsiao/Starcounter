using Sc.Server.Weaver.Schema;
using System;
using System.Collections.Generic;

namespace Starcounter.Internal.Weaver.Cache
{
    /// <summary>
    /// Represents the result of an assembly extracted from the weaver
    /// cache.
    /// </summary>
    public sealed class CachedAssembly
    {
        /// <summary>
        /// Gets the cache the assembly was requsted from.
        /// </summary>
        public readonly WeaverCache Cache;

        /// <summary>
        /// Gets the name of the assembly requested.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// Gets the assembly if the retreival was considered a success.
        /// If it was not, this field is null and other properties will
        /// reveal the reason why the assembly was not retreived.
        /// </summary>
        /// <value>The assembly.</value>
        public DatabaseAssembly Assembly { get; internal set; }

        /// <summary>
        /// Gets the set of files comprising a cached assembly. It's
        /// up to the client to operate on these (for example, extracting
        /// them to some "live" directory).
        /// </summary>
        public CachedAssemblyFiles Files { get; internal set; }
        
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
        internal CachedAssembly(WeaverCache cache, string name)
        {
            this.Cache = cache;
            this.Name = name;
            this.Assembly = null;
            this.NotFound = false;
            this.DeserializationException = null;
            this.BrokenDependency = null;
            this.MissingDependency = null;
        }
    }
}
