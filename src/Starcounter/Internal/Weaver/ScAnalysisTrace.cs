
using PostSharp.Extensibility;

namespace Starcounter.Internal.Weaver {
    /// <summary>
    /// Defines a trace source for <see cref="ScAnalysisTask"/>.
    /// </summary>
    internal class ScAnalysisTrace {
        /// <summary>
        /// Trace source for <see cref="ScAnalysisTask"/>.
        /// </summary>
        public static readonly PostSharpTrace Instance = new PostSharpTrace("ScAnalysis");
    }
}