using Mono.CSharp;
using Starcounter.Advanced.XSON;
using Starcounter.Templates;
using System;
using Starcounter.XSON.Serializer.Ast;
using Starcounter.XSON.Serializer;

namespace Starcounter.Internal.XSON.DeserializerCompiler {    
    internal class SerializerCompiler {
        private static SerializerCompiler _The;
        private static object Lock = new Object();

        internal static SerializerCompiler The {
            get {
                if (_The != null) {
                    return _The;
                }
                lock (Lock) {
                    _The = new SerializerCompiler();
                }
                return _The;
            }
        }

        public TypedJsonSerializer CreateStandardJsonSerializer(TObject jsonTemplate) {
			StdDomGenerator domGenerator;
			StdCSharpGenerator codeGenerator;
			string code;
			string fullTypeName;

            if (jsonTemplate == null)
                throw new ArgumentNullException();

			domGenerator = new StdDomGenerator(jsonTemplate);
			codeGenerator = new StdCSharpGenerator(domGenerator);
			code = codeGenerator.GenerateCode();
			fullTypeName = domGenerator.DomTree.SerializerClass.FullClassName;

            return GenerateJsonSerializer(code, fullTypeName);
        }

		public TypedJsonSerializer CreateFTJSerializer(Template jsonTemplate) {
			throw new NotImplementedException("TODO!");
			//AstNamespace node;
			//string fullTypeName;

			//if (jsonTemplate == null)
			//	throw new ArgumentNullException();

			//node = AstTreeGenerator.BuildAstTree(jsonTemplate);
			//fullTypeName = node.Namespace + "." + ((AstJsonSerializerClass)node.Children[0]).ClassName;

			//string code = node.GenerateCsSourceCode();
			//return GenerateJsonSerializer(code, fullTypeName);
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="code"></param>
        /// <param name="typeName"></param>
        /// <returns></returns>
        internal static TypedJsonSerializer GenerateJsonSerializer(string code, string typeName) {
            CompiledMethod cm;

            var settings = new CompilerSettings();
            settings.Unsafe = true;
            settings.GenerateDebugInfo = false;
            settings.Optimize = true;
            settings.AssemblyReferences.Add("Starcounter.Internal.dll");
            settings.AssemblyReferences.Add("Starcounter.XSON.dll");

            var context = new CompilerContext(settings, new ConsoleReportPrinter());
            var eval = new Evaluator(context);
            eval.Compile(code, out cm);

            return (TypedJsonSerializer)eval.Evaluate("new " + typeName + "();");
        }

    }
}
