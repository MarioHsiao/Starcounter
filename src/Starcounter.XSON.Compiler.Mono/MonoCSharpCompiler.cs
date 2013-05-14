using System;
using Mono.CSharp;
using Starcounter.Internal;
using Starcounter.Templates.Interfaces;
using Starcounter.XSON.Compiler.Roslyn;
using Starcounter.XSON.Metadata;

namespace Starcounter.XSON.Compiler.Mono {
    /// <summary>
    /// 
    /// </summary>
    public class MonoCSharpCompiler {
        private RoslynCSharpCompiler roslynCompiler;
        private CompilerSettings settings;
        private CompilerContext context;
//        private Evaluator evaluator;

        /// <summary>
        /// 
        /// </summary>
        public MonoCSharpCompiler() {
            roslynCompiler = new RoslynCSharpCompiler();
            settings = new CompilerSettings() {
                Unsafe = true,
                GenerateDebugInfo = false,
                Optimize = true
            };
            context = new CompilerContext(settings, new ConsoleReportPrinter());

        }

        //public object Compile(string code) {
        //    // TODO:
        //    // Adding debug assembly references for testing.
        //    return Compile(code,
        //                   "Starcounter.dll",
        //                   "Starcounter.Apps.JsonPatch.dll",
        //                   "Starcounter.Internal.dll",
        //                   "Starcounter.BitsAndBytes.Native.dll",
        //                   "Starcounter.XSON.dll");
        //}

        //public object Compile(string code, params string[] assemblyRefs) {
        //    object o = null;
        //    CompilerSettings settings = new CompilerSettings();

        //    foreach (var assRef in assemblyRefs) {
        //        settings.AssemblyReferences.Add(assRef);
        //    }
        //    settings.Unsafe = true;
        //    settings.GenerateDebugInfo = true;

        //    CompilerContext ctx = new CompilerContext(settings, new ConsoleReportPrinter());
        //    Evaluator eval = new Evaluator(ctx);
        //    CompiledMethod cm;
        //    string hmm = eval.Compile(code, out cm);

        //    //            o = eval.Evaluate("new " + fullClassName + "();");
        //    return o;
        //}

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
            return roslynCompiler.AnalyzeCodeBehind(className, codeBehindFile);
        }
    }
}
