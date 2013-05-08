using Starcounter.Templates;
using Starcounter.Templates.Interfaces;

namespace Starcounter.Internal.Application.CodeGeneration {
    public static class JsonFactory {
        public static ICompilerService Compiler;

        static JsonFactory() {
            // TODO:
            // SHOULD NOT be initialized here. For debug purposes only.
            //Compiler = new Starcounter.CompilerService.Mono.MonoCSharpCompiler();
        }

        public static TObj CreateTemplate(string json) {
            return null;
        }
    }
}
