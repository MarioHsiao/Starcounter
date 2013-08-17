
using Starcounter.Internal.Application.CodeGeneration;
using Starcounter.Templates;
using Starcounter.Templates.Interfaces;
using Starcounter.XSON.Metadata;
using System;
using System.IO;
namespace Starcounter.Internal.XSON {
    public class PartialClassGenerator {


        public static ITemplateCodeGenerator GenerateTypedJsonCode(string jsonFilePath, string codeBehindFilePath ) {
            string jsonContent = File.ReadAllText(jsonFilePath);
            string codeBehind;
            if (File.Exists(codeBehindFilePath)) {
                codeBehind = File.ReadAllText(codeBehindFilePath);
            }
            else {
                codeBehind = null;
            }
            var className = Path.GetFileNameWithoutExtension(jsonFilePath);
            return GenerateTypedJsonCode(className, jsonContent, codeBehind, jsonFilePath, codeBehindFilePath);
        }

        public static ITemplateCodeGenerator GenerateTypedJsonCode(string className, string jsonContent, string codebehind, string jsonFilePathNote, string codeBehindFilePathNote ) {
            TObj t;
            CodeBehindMetadata metadata;
            ITemplateCodeGenerator codegen;
            ITemplateCodeGeneratorModule codegenmodule;

           

            //            var className = Paths.StripFileNameWithoutExtention(jsonFilename);
            metadata = (CodeBehindMetadata)MonoCSharpCompiler.AnalyzeCodeBehind(className, codebehind, codeBehindFilePathNote );

            //            t = CreateJsonTemplate(className, jsonContent);
            t = TObj.CreateFromMarkup<Json,TJson>("json",jsonContent,jsonFilePathNote);
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
        public static CodeBehindMetadata CreateCodeBehindMetadata(string className, string code, string codeBehindFilePath) {
            return MonoCSharpCompiler.AnalyzeCodeBehind(className, code, codeBehindFilePath);
        }
    }
}