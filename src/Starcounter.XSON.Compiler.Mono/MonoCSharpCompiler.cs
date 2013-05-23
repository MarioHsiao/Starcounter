using Mono.CSharp;
using Starcounter.Internal;
using Starcounter.XSON.Metadata;
using Starcounter.XSON.Serializers;

namespace Starcounter.XSON.Compiler.Mono {
    /// <summary>
    /// 
    /// </summary>
    public class MonoCSharpCompiler {
        private CompilerSettings settings;
        private CompilerContext context;

        /// <summary>
        /// 
        /// </summary>
        public MonoCSharpCompiler() {
            settings = new CompilerSettings() {
                Unsafe = true,
                GenerateDebugInfo = false,
                Optimize = true
            };
            context = new CompilerContext(settings, new ConsoleReportPrinter());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="code"></param>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public TypedJsonSerializer GenerateJsonSerializer(string code, string typeName) {
            settings.AssemblyReferences.Clear();
            settings.AssemblyReferences.Add("Starcounter.Internal.dll");
            settings.AssemblyReferences.Add("Starcounter.XSON.dll");
            settings.AssemblyReferences.Add("Starcounter.XSON.CodeGeneration.dll");
            
            Evaluator eval = new Evaluator(context);
            
            CompiledMethod cm;
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
