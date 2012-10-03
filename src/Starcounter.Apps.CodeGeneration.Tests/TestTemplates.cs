
using Starcounter;
using System;
using NUnit.Framework;
using Starcounter.Templates;
using Starcounter.Internal.Application.CodeGeneration;
using Starcounter.Templates.Interfaces;
using Starcounter.Internal.Application.JsonReader;
using System.IO;
using System.Collections.Generic;

namespace Test {
    [TestFixture]
    public class TestTemplates {

        [Test]
        public static void CreateCsFromJsFile() {
            AppTemplate templ = TemplateFromJs.ReadFile("MySampleApp.json");
            Assert.NotNull(templ);
        }


        [Test]
        public static void GenerateCs() {
            AppTemplate actual = TemplateFromJs.ReadFile("MySampleApp.json");
            Assert.IsInstanceOf(typeof(AppTemplate), actual);
            CodeGenerationModule codegenmodule = new CodeGenerationModule();
            var codegen = codegenmodule.CreateGenerator("C#", actual, CodeBehindMetadata.Empty);
            Console.WriteLine(codegen.GenerateCode());
        }

        [Test]
        public static void GenerateCsFromSimpleJs() {
           AppTemplate actual = TemplateFromJs.ReadFile("simple.json");
           actual.ClassName = "PlayerApp";

           var file = new System.IO.StreamReader("simple.facit.cs");
           var facit = file.ReadToEnd();
           file.Close();
           Assert.IsInstanceOf(typeof(AppTemplate), actual);
           CodeGenerationModule codegenmodule = new CodeGenerationModule();
           ITemplateCodeGenerator codegen = codegenmodule.CreateGenerator("C#",actual, CodeBehindMetadata.Empty);
           var code = codegen.GenerateCode();
           Console.WriteLine(code);
           Assert.AreEqual(facit, code);
        }

        [Test]
        public static void GenerateCsWithCodeBehind()
        {
            String className = "MySampleApp";
            CodeBehindMetadata metadata = RoslynParserHelper.GetCodeBehindMetadata(className,
                                                                                   className + ".json.cs");
            
            AppTemplate actual = TemplateFromJs.ReadFile(className + ".json");
            Assert.IsInstanceOf(typeof(AppTemplate), actual);

            actual.Namespace = metadata.RootNamespace;
            Assert.IsNotNullOrEmpty(actual.Namespace);

            CodeGenerationModule codegenmodule = new CodeGenerationModule();
            ITemplateCodeGenerator codegen = codegenmodule.CreateGenerator("C#", actual, metadata);
            Console.WriteLine(codegen.GenerateCode());
        }
    }
}




   