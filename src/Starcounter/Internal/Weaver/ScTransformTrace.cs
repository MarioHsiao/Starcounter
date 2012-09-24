
using PostSharp.Extensibility;

namespace Starcounter.Internal.Weaver {
    /// <summary>
    /// Declare a PostSharp trace source for this assembly.
    /// </summary>
    internal static class ScTransformTrace {
        /// <summary>
        /// PostSharp trace source for this assembly.
        /// </summary>
        public static readonly PostSharpTrace Instance = new PostSharpTrace("ScTransform");
    }
}