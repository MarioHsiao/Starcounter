using Starcounter.Internal.MsBuild.Codegen;
using Starcounter.Templates;
using Starcounter.Templates.Interfaces;
using Starcounter.XSON.Compiler.Mono;
using Starcounter.XSON.Metadata;
using System;
using System.IO;

namespace Starcounter.Internal.XSON {
    public class PartialClassGenerator {
        // DONT FORGET:
        // Currently this value is not used in the buildtasks due to problems
        // with locked files when compiling Level1. 
        //
        // REMEMBER:
        // Update Starcounter.MsBuild.targets and Starcounter.MsBuild.Develop.targets 
        // with the new versionnumber.
        public const long CSHARP_CODEGEN_VERSION = 2;
        
        public static ITemplateCodeGenerator GenerateTypedJsonCode(string jsonFilePath, string codeBehindFilePath ) {
            string jsonContent = File.ReadAllText(jsonFilePath);
            string codeBehind;

            if (File.Exists(codeBehindFilePath)) {
                codeBehind = File.ReadAllText(codeBehindFilePath);
            }
            else {
                codeBehind = null;
            }

            var t = TObject.CreateFromMarkup<Json, TValue>("json", jsonContent, jsonFilePath);
            t.ClassName = Path.GetFileNameWithoutExtension(jsonFilePath);

            return GenerateTypedJsonCode(t, codeBehind, codeBehindFilePath);
        }

        public static ITemplateCodeGenerator GenerateTypedJsonCode(TValue template, string codebehind, string codeBehindFilePathNote) {
            CodeBehindMetadata metadata;
            ITemplateCodeGenerator codegen;
            ITemplateCodeGeneratorModule codegenmodule;

            metadata = CodeBehindParser.Analyze(template.ClassName, codebehind, codeBehindFilePathNote);
            var rootClassInfo = metadata.RootClassInfo;

            if (rootClassInfo != null) {
                if (String.IsNullOrEmpty(template.Namespace))
                    template.Namespace = metadata.RootClassInfo.Namespace;
            }

            codegenmodule = new Gen2CodeGenerationModule();
            codegen = codegenmodule.CreateGenerator(template.GetType(), "C#", template, metadata);

            return codegen;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="className"></param>
        /// <param name="codeBehindFilePath"></param>
        /// <returns></returns>
        public static CodeBehindMetadata CreateCodeBehindMetadata(string className, string code, string codeBehindFilePath) {
            return CodeBehindParser.Analyze(className, code, codeBehindFilePath);
        }
    }
}