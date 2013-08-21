
using Starcounter.Internal.Application.CodeGeneration;
using Starcounter.Internal.MsBuild.Codegen;
using Starcounter.Templates;
using Starcounter.Templates.Interfaces;
using Starcounter.XSON.Compiler.Mono;
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

            var t = TObj.CreateFromMarkup<Json, TJson>("json", jsonContent, jsonFilePath);
            t.ClassName = Path.GetFileNameWithoutExtension(jsonFilePath);

            return GenerateTypedJsonCode( t, codeBehind, codeBehindFilePath);
        }

        public static ITemplateCodeGenerator GenerateTypedJsonCode( TJson template, string codebehind, string codeBehindFilePathNote ) {

            CodeBehindMetadata metadata;
            ITemplateCodeGenerator codegen;
            ITemplateCodeGeneratorModule codegenmodule;

           

            //            var className = Paths.StripFileNameWithoutExtention(jsonFilename);
            metadata = CodeBehindAnalyzer.Analyze(template.ClassName, codebehind, codeBehindFilePathNote); 
                //(CodeBehindMetadata)MonoCSharpCompiler.AnalyzeCodeBehind(template.ClassName, codebehind, codeBehindFilePathNote );
                //metadata = CodeBehindAnalyser.

            //            t = CreateJsonTemplate(className, jsonContent);
//            className = t.ClassName;

            if (String.IsNullOrEmpty(template.Namespace))
                template.Namespace = metadata.RootClassInfo.Namespace;

            if (metadata.RootClassInfo.RawJsonMapAttribute != null ||
                  (!metadata.RootClassInfo.IsDeclaredInCodeBehind && metadata.JsonPropertyMapList.Count > 1 ) )
               codegenmodule = new Gen2CodeGenerationModule(); // Before gen2, we did not support json attributes on the root class
            else
               codegenmodule = new Gen1CodeGenerationModule();

            codegen = codegenmodule.CreateGenerator(typeof(TJson), "C#", template, metadata);

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
//            return MonoCSharpCompiler.AnalyzeCodeBehind(className, code, codeBehindFilePath);
            return CodeBehindAnalyzer.Analyze(className, code, codeBehindFilePath);
        }
    }
}