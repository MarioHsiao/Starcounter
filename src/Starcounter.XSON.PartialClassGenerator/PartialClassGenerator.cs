using System.IO;
using Starcounter.Templates;
using Starcounter.XSON.Interfaces;
using Starcounter.XSON.Metadata;

namespace Starcounter.XSON.PartialClassGenerator {
    public class PartialClassGenerator {
        // DONT FORGET:
        // Currently this value is not used in the buildtasks due to problems
        // with locked files when compiling Level1. 
        //
        // REMEMBER:
        // Update Starcounter.MsBuild.targets and Starcounter.MsBuild.Develop.targets 
        // with the new versionnumber.
        public const long CSHARP_CODEGEN_VERSION = 3;
        
        public static ITemplateCodeGenerator GenerateTypedJsonCode(string jsonFilePath, string codeBehindFilePath ) {
            string jsonContent = File.ReadAllText(jsonFilePath);
            string codeBehind;

            if (File.Exists(codeBehindFilePath)) {
                codeBehind = File.ReadAllText(codeBehindFilePath);
            }
            else {
                codeBehind = null;
            }

            var t = (TValue)Template.CreateFromMarkup("json", jsonContent, jsonFilePath);
            t.CodegenInfo.ClassName = Path.GetFileNameWithoutExtension(jsonFilePath);

            return GenerateTypedJsonCode(t, codeBehind, codeBehindFilePath);
        }

        public static ITemplateCodeGenerator GenerateTypedJsonCode(TValue template, string codebehind, string codeBehindFilePathNote) {
            CodeBehindMetadata metadata;
            ITemplateCodeGenerator codegen;
            ITemplateCodeGeneratorModule codegenmodule;

            var parser = new RoslynCodeBehindParser(template.CodegenInfo.ClassName, codebehind, codeBehindFilePathNote);
            metadata = parser.ParseToMetadata();

            var rootClassInfo = metadata.RootClassInfo;

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
            var parser = new RoslynCodeBehindParser(className, code, codeBehindFilePath);
            return parser.ParseToMetadata();
        }
    }
}