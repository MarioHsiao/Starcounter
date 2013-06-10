using System;
using System.IO;
using Starcounter.Internal;
using Starcounter.Internal.Application.CodeGeneration;
using Starcounter.Internal.JsonTemplate;
using Starcounter.Templates;
using Starcounter.Templates.Interfaces;
using Starcounter.XSON.Compiler.Mono;
using Starcounter.XSON.Metadata;
using Starcounter.XSON.Serializers;
using System.Runtime.InteropServices;
using System.Reflection;

namespace Starcounter.XSON.CodeGeneration
{
    public sealed class Initializer {
        [DllImport("kernel32.dll", CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Unicode)]
        static extern IntPtr LoadLibrary(string filePath);

        /// <summary>
        /// Locks initialization.
        /// </summary>
        static Object initializerLock = new Object();

        /// <summary>
        /// Initializes XSON code generation module.
        /// </summary>
        public static void InitializeXSON() {

            if (null != Obj.Factory)
                return;

            lock (initializerLock) {

                if (null != Obj.Factory)
                    return;

                String monoDllPath = "Mono.CSharp.dll";

                // Get the location of current executing assembly.
                String tempMonoDllPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), monoDllPath);
                if (File.Exists(tempMonoDllPath)) {
                    monoDllPath = tempMonoDllPath;
                    goto LOAD_MONO_DLL;
                }
                
                // Since that didn't work trying StarcounterBin.
                String starcounterBin = System.Environment.GetEnvironmentVariable(StarcounterEnvironment.VariableNames.InstallationDirectory);
                monoDllPath = Path.Combine(starcounterBin, monoDllPath);

LOAD_MONO_DLL:

                if (!File.Exists(monoDllPath))
                    throw new FileNotFoundException("Mono DLL not found!");

                IntPtr moduleHandle = LoadLibrary(monoDllPath);

                if (IntPtr.Zero == moduleHandle)
                    throw new Exception("Can't load Mono DLL!");

                Obj.Factory = new TypedJsonFactory();
            }
        }
    }

    public class TypedJsonFactory : ITypedJsonFactory {
//        private ICompilerService compiler;
        private MonoCSharpCompiler compiler;

        public TypedJsonFactory() {
            this.compiler = new MonoCSharpCompiler();
//            this.compiler = new RoslynCSharpCompiler();
        }

        public TObj CreateJsonTemplate(string json) {
            return TemplateFromJs.CreateFromJs(json, false);
        }

        public TObj CreateJsonTemplateFromFile(string filePath) {
            string json = File.ReadAllText(filePath);
            return CreateJsonTemplate(json);
        }

        public TypedJsonSerializer CreateJsonSerializer(TObj jsonTemplate) {
            AstNamespace node;
            string fullTypeName;

            if (jsonTemplate == null)
                throw new ArgumentNullException();

            node = AstTreeGenerator.BuildAstTree(jsonTemplate);
            fullTypeName = node.Namespace + "." + ((AstJsonSerializerClass)node.Children[0]).ClassName;
    
            string code = node.GenerateCsSourceCode();
            return compiler.GenerateJsonSerializer(code, fullTypeName);
        }

        public string GenerateTypedJsonCode(string jsonFilePath) {
            return GenerateTypedJsonCode(jsonFilePath, null);
        }

        public string GenerateTypedJsonCode(string jsonFilePath, string codeBehindFilePath) {
            TObj t;
            CodeBehindMetadata metadata;
            ITemplateCodeGenerator codegen;
            ITemplateCodeGeneratorModule codegenmodule;

            String jsonContent = File.ReadAllText(jsonFilePath);

//            var className = Paths.StripFileNameWithoutExtention(jsonFilename);
            var className = Path.GetFileNameWithoutExtension(jsonFilePath);
            metadata = (CodeBehindMetadata)compiler.AnalyzeCodeBehind(className, codeBehindFilePath);

            t = CreateJsonTemplate(jsonContent);
            if (t.ClassName == null) {
                t.ClassName = className;
            }

            if (String.IsNullOrEmpty(t.Namespace))
                t.Namespace = metadata.RootNamespace;

            codegenmodule = new CodeGenerationModule();
            codegen = codegenmodule.CreateGenerator(typeof(TJson), "C#", t, metadata);

            return codegen.GenerateCode();
        }

        public CodeBehindMetadata CreateCodeBehindMetadata(string className, string codeBehindFilePath) {
            return compiler.AnalyzeCodeBehind(className, codeBehindFilePath);
        }
    }
}
