using System;
using System.IO;
using Starcounter.CompilerService.Mono;
using Starcounter.CompilerService.Roslyn;
using Starcounter.Internal.Application.CodeGeneration;
using Starcounter.Internal.JsonTemplate;
using Starcounter.Templates;
using Starcounter.Templates.Interfaces;

namespace Starcounter {
    public class JsonFactoryImpl : IJsonFactory {
        private ICompilerService compiler;

        public JsonFactoryImpl() {
            this.compiler = new MonoCSharpCompiler();
//            this.compiler = new RoslynCSharpCompiler();
        }

        public object CreateJsonTemplate(string json) {
            return TemplateFromJs.CreateFromJs(json, false);
        }

        public object CreateJsonTemplateFromFile(string filePath) {
            string json = File.ReadAllText(filePath);
            return CreateJsonTemplate(json);
        }

        public object CreateJsonSerializer(object jsonTemplate) {
            AstNamespace node;
            TObj tobj;
            string fullTypeName;

            if (jsonTemplate == null)
                throw new ArgumentNullException();

            tobj = jsonTemplate as TObj;
            if (tobj == null)
                throw new ArgumentException();

            node = AstTreeGenerator.BuildAstTree(tobj);
            fullTypeName = node.Namespace + "." + ((AstJsonSerializerClass)node.Children[0]).ClassName;
    
            string code = node.GenerateCsSourceCode();
            return compiler.GenerateJsonSerializer(code, fullTypeName);
        }

        public ICompilerService Compiler { get { return compiler; } }
    }
}
