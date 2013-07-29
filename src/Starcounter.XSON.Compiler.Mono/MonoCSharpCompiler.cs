using Mono.CSharp;
using Starcounter.Internal;
using Starcounter.XSON.Metadata;
using Starcounter.XSON.Serializers;

namespace Starcounter.XSON.Compiler.Mono {
    /// <summary>
    /// 
    /// </summary>
    public class MonoCSharpCompiler {
        /// <summary>
        /// 
        /// </summary>
        public MonoCSharpCompiler() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="code"></param>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public TypedJsonSerializer GenerateJsonSerializer(string code, string typeName) {
            CompiledMethod cm;

            var settings = new CompilerSettings() {
                Unsafe = true,
                GenerateDebugInfo = false,
                Optimize = true
            };
            settings.AssemblyReferences.Clear();
            settings.AssemblyReferences.Add("Starcounter.Internal.dll");
            settings.AssemblyReferences.Add("Starcounter.XSON.dll");

            var context = new CompilerContext(settings, new ConsoleReportPrinter());
            var eval = new Evaluator(context);
            eval.Compile(code, out cm);

            return (TypedJsonSerializer)eval.Evaluate("new " + typeName + "();");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="className"></param>
        /// <param name="codeBehindFile"></param>
        /// <returns></returns>
        public CodeBehindMetadata AnalyzeCodeBehind(string className, string codeBehindFile) {
            return CodeBehindAnalyzer.Analyze(className, codeBehindFile);
        }
    }
}
