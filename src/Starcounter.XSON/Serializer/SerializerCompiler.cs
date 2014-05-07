using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Mono.CSharp;
using Starcounter.Advanced.XSON;
using Starcounter.Templates;
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

        internal TypedJsonSerializer CreateStandardJsonSerializer(TObject jsonTemplate) {
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

		internal TypedJsonSerializer CreateFTJSerializer(TObject jsonTemplate) {
			FTJDomGenerator domGenerator;
			FTJCSharpGenerator codeGenerator;
			string code;
			string fullTypeName;

			if (jsonTemplate == null)
				throw new ArgumentNullException();

			domGenerator = new FTJDomGenerator(jsonTemplate);
			codeGenerator = new FTJCSharpGenerator(domGenerator);
			code = codeGenerator.GenerateCode();
			fullTypeName = domGenerator.DomTree.SerializerClass.FullClassName;

			return GenerateJsonSerializer(code, fullTypeName);
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
            settings.LoadDefaultReferences = false;

            settings.AssemblyReferences.Add(Assembly.GetAssembly(typeof(Uri)).FullName); // System
            settings.AssemblyReferences.Add(Assembly.GetAssembly(typeof(CallSite)).FullName); // System.Core
            settings.AssemblyReferences.Add(Assembly.GetAssembly(typeof(Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)).FullName);

            settings.AssemblyReferences.Add("Starcounter.Internal.dll");
            settings.AssemblyReferences.Add("Starcounter.XSON.dll");
			settings.AssemblyReferences.Add("FasterThanJson.dll");

            var context = new CompilerContext(settings, new ConsoleReportPrinter());
            var eval = new Evaluator(context);
            eval.Compile(code, out cm);

            return (TypedJsonSerializer)eval.Evaluate("new " + typeName + "();");
        }

    }
}
