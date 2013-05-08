using Starcounter.Templates;
using Starcounter.Templates.Interfaces;

// TODO:
// The Compiler instance should not be instantiated here and the references 
// to Starcounter.CompilerService.Mono and Starcounter.CompilerService.Roslyn
// needs to be removed.

namespace Starcounter {
    public static class JsonFactory {
        private static ICompilerService compiler;

        public static ICompilerService Compiler {
            get { return compiler; }
        }

        static JsonFactory() {
//            Compiler = new Starcounter.CompilerService.Mono.MonoCSharpCompiler();
            compiler = new Starcounter.CompilerService.Roslyn.RoslynCSharpCompiler();
        }

        public static TObj CreateTemplate(string json) {
            return null;
        }

        public static Json CreateTypedJsonInstance(string json) {
            return null;
        }
    }
}
