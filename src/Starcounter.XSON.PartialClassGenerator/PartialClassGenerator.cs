
using Starcounter.Internal.MsBuild.Codegen;
using Starcounter.Templates;
using Starcounter.Templates.Interfaces;
using Starcounter.XSON.Compiler.Mono;
using Starcounter.XSON.Metadata;
using System;
using System.IO;
using TJson = Starcounter.Templates.TObject;


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

            //            var t = TJson.CreateFromMarkup<Json, TJson>("json", jsonContent, jsonFilePath);
            var t = TJson.CreateFromMarkup<Json, Json.JsonByExample.Schema>("json", jsonContent, jsonFilePath);
            t.ClassName = Path.GetFileNameWithoutExtension(jsonFilePath);

            return GenerateTypedJsonCode( t, codeBehind, codeBehindFilePath);
        }

        public static ITemplateCodeGenerator GenerateTypedJsonCode(TJson template, string codebehind, string codeBehindFilePathNote) {

            CodeBehindMetadata metadata;
            ITemplateCodeGenerator codegen;
            ITemplateCodeGeneratorModule codegenmodule;

           

            //            var className = Paths.StripFileNameWithoutExtention(jsonFilename);
            metadata = CodeBehindParser.Analyze(template.ClassName, codebehind, codeBehindFilePathNote); 
                //(CodeBehindMetadata)MonoCSharpCompiler.AnalyzeCodeBehind(template.ClassName, codebehind, codeBehindFilePathNote );
                //metadata = CodeBehindAnalyser.

            //            t = CreateJsonTemplate(className, jsonContent);
//            className = t.ClassName;

            var rootClassInfo = metadata.RootClassInfo;

            if (rootClassInfo != null) {
                if (String.IsNullOrEmpty(template.Namespace))
                    template.Namespace = metadata.RootClassInfo.Namespace;
            }
            codegenmodule = new Gen2CodeGenerationModule();

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
            return CodeBehindParser.Analyze(className, code, codeBehindFilePath);
        }
    }
}