
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
        /// Gets or sets a value dictating if outdated assemblies should be
        /// weaved/transformed. Defaults to true. If this is set to false,
        /// only analysis will be performed.
        /// </summary>
        public bool AnalyzeOnly { get; set; }

        /// <summary>
        /// Gets or sets the weaver runtime directory path. This path will be
        /// consulted when the weaver needs to locate neccessary runtime files,
        /// such as the PostSharp-related project- and plugin files.
        /// </summary>
        /// <remarks>
        /// Weaver setup will try to assign this to the Starcounter binary
        /// directory and, as a fallback, use the current directory if that
        /// fails. Tools can override this funcationality by explicitly setting
        /// it after the weaver component is created (and before executed).
        /// </remarks>
        public string WeaverRuntimeDirectory { get; set; }

        /// <summary>
        /// Gets or sets a value indicating if the weaver cache should be
        /// disabled. If the cache is disabled, cached assemblies will not
        /// be considered and all input will always be analyzed and/or
        /// transformed on every run.
        /// </summary>
        public bool DisableWeaverCache { get; set; }

        /// <summary>
        /// More of an internal, experimental thing, allowing to instruct
        /// weaver to emit a redirection layer over CRUD calls.
        /// </summary>
        public bool UseStateRedirect { get; set; }

        /// <summary>
        /// Gets or sets a value indicating if the current weaver should
        /// exclude any possible edition libraries found when running.
        /// </summary>
        public bool DisableEditionLibraries { get; set; }

        /// <summary>
        /// Can be used by hosts that want to enable maximum tracing
        /// and diagnostic output.
        /// </summary>
        public bool EnableTracing { get; set; }

        /// <summary>
        /// Indicates if the weaver should emit a detailed boot diagnostic message
        /// before actual weaaving kicks in, and a similar message when finalizing.
        /// </summary>
        public bool EmitBootAndFinalizationDiagnostics { get; set; }

        /// <summary>
        /// Instruct the weaver to always include a location when writing errors to
        /// the host.
        /// </summary>
        public bool IncludeLocationInErrorMessages { get; set; }
    }
}
