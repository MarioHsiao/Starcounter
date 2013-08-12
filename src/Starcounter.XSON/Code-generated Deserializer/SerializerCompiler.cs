

using Mono.CSharp;
using Starcounter.Advanced.XSON;
using Starcounter.Internal.Application.CodeGeneration;
using Starcounter.Templates;
using System;


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

        public TypedJsonSerializer CreateTypedJsonSerializer(TObj jsonTemplate) {
            AstNamespace node;
            string fullTypeName;

            if (jsonTemplate == null)
                throw new ArgumentNullException();

            node = AstTreeGenerator.BuildAstTree(jsonTemplate);
            fullTypeName = node.Namespace + "." + ((AstJsonSerializerClass)node.Children[0]).ClassName;

            string code = node.GenerateCsSourceCode();
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
            settings.AssemblyReferences.Add("Starcounter.Internal.dll");
            settings.AssemblyReferences.Add("Starcounter.XSON.dll");

            var context = new CompilerContext(settings, new ConsoleReportPrinter());
            var eval = new Evaluator(context);
            eval.Compile(code, out cm);

            return (TypedJsonSerializer)eval.Evaluate("new " + typeName + "();");
        }

    }
}

/*
/////////

        /// <summary>
        /// Initializes XSON code generation module.
        /// </summary>
        internal static void InitializeXSON() {

            if (xsonInitialized_)
                return;

            lock (lockObject_) {

                if (xsonInitialized_)
                    return;

                Obj.Factory = new TypedJsonFactory();

                xsonInitialized_ = true;
            }
        }


/////

    public sealed class Initializer {

        /// <summary>
        /// Locks initialization.
        /// </summary>
        static Object lockObject_ = new Object();

        /// <summary>
        /// Indicates if XSON is initialized.
        /// </summary>
        static Boolean xsonInitialized_;

    }
*/