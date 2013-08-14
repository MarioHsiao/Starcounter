

using Starcounter.XSON.Compiler.Mono;
using Starcounter.XSON.Metadata;

namespace Starcounter.Internal.XSON {
    /// <summary>
    /// 
    /// </summary>
    public static class MonoCSharpCompiler {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="className"></param>
        /// <param name="codeBehindFile"></param>
        /// <returns></returns>
        public static CodeBehindMetadata AnalyzeCodeBehind(string className, string codeBehindFile) {
            return CodeBehindAnalyzer.Analyze(className, codeBehindFile);
        }
    }
}
