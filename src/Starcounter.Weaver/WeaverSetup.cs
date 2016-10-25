
using System;

namespace Starcounter.Weaver
{
    /// <summary>
    /// Defines the customizable weaver setup traits.
    /// </summary>
    [Serializable]
    public class WeaverSetup
    {
        /// <summary>
        /// The directory where the weaver looks for input.
        /// </summary>
        public string InputDirectory { get; set; }

        /// <summary>
        /// The cache directory used by the weaver.
        /// </summary>
        public string CacheDirectory { get; set; }

        /// <summary>
        /// The output directory, mirroring the binaries in the input directory
        /// with relevant binaries weaved.
        /// </summary>
        public string OutputDirectory { get; set; }

        /// <summary>
        /// Gets or sets the assembly file the weaver should act upon. The
        /// file is expected to be found in the <see cref="InputDirectory"/>.
        /// </summary>
        public string AssemblyFile { get; set; }

        /// <summary>
        /// Gets or sets a value that adapts the code weaver to perform
        /// weaving only to the cache directory and never touch the input
        /// binaries.
        /// </summary>
        public bool WeaveToCacheOnly { get; set; }

        /// <summary>
        /// Gets or sets a value indicating if the weaver cache should be
        /// disabled. If the cache is disabled, cached assemblies will not
        /// be considered and all input will always be analyzed and/or
        /// transformed on every run.
        /// </summary>
        public bool DisableWeaverCache { get; set; }
    }
}
