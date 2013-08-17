
using Starcounter.Internal.Application.CodeGeneration;
using Starcounter.Templates;
using Starcounter.Templates.Interfaces;
using Starcounter.XSON.Metadata;
using System;
using System.IO;
namespace Starcounter.Internal.XSON {
    public class PartialClassGenerator {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="jsonFilePath"></param>
        /// <param name="codeBehindFilePath"></param>
        /// <returns></returns>
        public static ITemplateCodeGenerator GenerateTypedJsonCode(string jsonFilePath, string codeBehindFilePath, bool debug = false ) {
            TObj t;
            CodeBehindMetadata metadata;
            ITemplateCodeGenerator codegen;
            ITemplateCodeGeneratorModule codegenmodule;

            string jsonContent = File.ReadAllText(jsonFilePath);

            //            var className = Paths.StripFileNameWithoutExtention(jsonFilename);
            var className = Path.GetFileNameWithoutExtension(jsonFilePath);
            metadata = (CodeBehindMetadata)MonoCSharpCompiler.AnalyzeCodeBehind(className, codeBehindFilePath);

            //            t = CreateJsonTemplate(className, jsonContent);
            t = TObj.CreateFromJson(jsonContent);
            t.ClassName = className;

            if (String.IsNullOrEmpty(t.Namespace))
                t.Namespace = metadata.RootNamespace;

            codegenmodule = new CodeGenerationModule();
            codegen = codegenmodule.CreateGenerator(typeof(TJson), "C#", t, metadata);

            return codegen;
//            if (debug)
//                return codegen.DumpAstTree();
//            return codegen.GenerateCode();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="className"></param>
        /// <param name="codeBehindFilePath"></param>
        /// <returns></returns>
        public static CodeBehindMetadata CreateCodeBehindMetadata(string className, string codeBehindFilePath) {
            return MonoCSharpCompiler.AnalyzeCodeBehind(className, codeBehindFilePath);
        }
    }
}