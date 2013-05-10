using System;
using Mono.CSharp;
using Starcounter.Templates.Interfaces;

namespace Starcounter.CompilerService.Mono {
    public class MonoCSharpCompiler : ICompilerService {
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

        public Type CompileType(string code, string typeName) {
            CompilerSettings settings = new CompilerSettings();
            settings.AssemblyReferences.Add("Starcounter.dll");
            settings.AssemblyReferences.Add("Starcounter.Apps.JsonPatch.dll");
            settings.AssemblyReferences.Add("Starcounter.Internal.dll");
            settings.AssemblyReferences.Add("Starcounter.BitsAndBytes.Native.dll");
            settings.AssemblyReferences.Add("Starcounter.XSON.dll");
            settings.Unsafe = true;
            settings.GenerateDebugInfo = true;

            CompilerContext ctx = new CompilerContext(settings, new ConsoleReportPrinter());
            Evaluator eval = new Evaluator(ctx);
            
            CompiledMethod cm;
            eval.Compile(code, out cm);

            return (Type)eval.Evaluate("typeof(" + typeName + ");");
        }

        public object AnalyzeCodeBehind(string className, string codeBehindFile) {
            throw new NotImplementedException();
        }
    }
}
