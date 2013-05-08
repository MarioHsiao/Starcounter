using System;
using Mono.CSharp;
using Starcounter.Templates.Interfaces;

namespace Starcounter.CompilerService.Mono {
    public class MonoCSharpCompiler : ICompilerService {
        public object Compile(string code) {
            object o = null;
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

            bool b = eval.Run(code);
//            o = eval.Evaluate("new " + fullClassName + "();");

            return o;    
        }

        public object Compile(string code, params string[] assemblyRefs) {
            return null;
        }

        public object AnalyzeCodeBehind(string className, string codeBehindFile) {
            throw new NotImplementedException();
        }
    }
}
