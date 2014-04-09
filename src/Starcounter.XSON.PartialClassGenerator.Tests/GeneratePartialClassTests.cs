// ***********************************************************************
// <copyright file="TestTemplates.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.IO;
using NUnit.Framework;
using Starcounter.Internal.Application.CodeGeneration;
using Starcounter.Internal.MsBuild.Codegen;
using Starcounter.Templates.Interfaces;
using Starcounter.XSON.Metadata;
using TJson = Starcounter.Templates.TObject;

namespace Starcounter.Internal.XSON.PartialClassGeneration.Tests {
    /// <summary>
    /// Class TestTemplates
    /// </summary>
    [TestFixture]
    public class GeneratePartialClassTests {
        [TestFixtureSetUp]
        public static void InitializeTest() {
        }

        /// <summary>
        /// Creates a template from a JSON-by-example file
        /// </summary>
        /// <param name="filePath">The file to load</param>
        /// <returns>The newly created template</returns>
        private static TJson CreateJsonTemplateFromFile(string filePath) {
            string json = File.ReadAllText("Input\\" + filePath);
            string className = Path.GetFileNameWithoutExtension(filePath);
            var tobj = TJson.CreateFromMarkup<Json, TJson>("json", json, className);
            tobj.ClassName = className;
            return tobj;
        }

        /// <summary>
        /// Creates the cs from js file.
        /// </summary>
        [Test]
        public static void CreateCsFromJsFile() {
            TJson templ = CreateJsonTemplateFromFile("MySampleApp.json");
            Assert.NotNull(templ);
        }

        /// <summary>
        /// Generates the cs.
        /// </summary>
        [Test]
        public static void GenerateCsGen1() {
            TJson actual = CreateJsonTemplateFromFile("MySampleApp.json");
            Assert.IsInstanceOf(typeof(TJson), actual);
            Gen1CodeGenerationModule codegenmodule = new Gen1CodeGenerationModule();
            var codegen = codegenmodule.CreateGenerator(typeof(TJson), "C#", actual, CodeBehindMetadata.Empty);
            Console.WriteLine(codegen.GenerateCode());
        }

		/// <summary>
		/// Generates the cs.
		/// </summary>
		[Test]
		public static void GenerateCsGen2() {
			TJson actual = CreateJsonTemplateFromFile("MySampleApp.json");
			Assert.IsInstanceOf(typeof(TJson), actual);
			Gen2CodeGenerationModule codegenmodule = new Gen2CodeGenerationModule();
			var codegen = codegenmodule.CreateGenerator(typeof(TJson), "C#", actual, CodeBehindMetadata.Empty);
			Console.WriteLine(codegen.GenerateCode());
		}

        /// <summary>
        /// </summary>
        [Test]
        public static void GenerateCsFromSimpleJs() {
            TJson actual = CreateJsonTemplateFromFile("simple.json");
            actual.ClassName = "PlayerApp";

//            var file = new System.IO.StreamReader("simple.facit.cs");
//            var facit = file.ReadToEnd();
//            file.Close();
            Assert.IsInstanceOf(typeof(TJson), actual);
            var codegenmodule = new Gen2CodeGenerationModule();
            ITemplateCodeGenerator codegen = codegenmodule.CreateGenerator(typeof(TJson), "C#", actual, CodeBehindMetadata.Empty);
            Console.WriteLine(codegen.DumpAstTree());
            var code = codegen.GenerateCode();
            Console.WriteLine(code);
            // Assert.AreEqual(facit, code);
        }

        /// <summary>
        /// </summary>
        [Test]
        public static void GenerateCsFromSuperSimpleJs() {
            TJson actual = CreateJsonTemplateFromFile("supersimple.json");
            actual.ClassName = "PlayerApp";

            Assert.IsInstanceOf(typeof(TJson), actual);
            var codegenmodule = new Gen2CodeGenerationModule();
            ITemplateCodeGenerator codegen = codegenmodule.CreateGenerator(typeof(TJson), "C#", actual, CodeBehindMetadata.Empty);
            Console.WriteLine(codegen.DumpAstTree());
            var code = codegen.GenerateCode();
            Console.WriteLine(code);
        }

        [Test]
        public static void GenerateCsFromTestMessage() {
            String className = "TestMessage";
            string codeBehindFilePath = "Input\\" + className + ".json.cs";
            string codeBehind = File.ReadAllText(codeBehindFilePath);
            CodeBehindMetadata metadata = PartialClassGenerator.CreateCodeBehindMetadata(className, codeBehind, codeBehindFilePath );

            TJson actual = CreateJsonTemplateFromFile(className + ".json");
            Assert.IsInstanceOf(typeof(TJson), actual);

            actual.Namespace = metadata.RootClassInfo.Namespace;
            actual.ClassName = className;

            Assert.IsNotNullOrEmpty(actual.Namespace);

            Gen1CodeGenerationModule codegenmodule = new Gen1CodeGenerationModule();
            ITemplateCodeGenerator codegen = codegenmodule.CreateGenerator(typeof(TJson), "C#", actual, metadata);

            Console.WriteLine(codegen.GenerateCode());
        }

        [Test]
        public static void GenerateCsWithCodeBehind()
        {
            String className = "MySampleApp";
            string codeBehindFilePath = "Input\\" + className + ".json.cs";
            string codeBehind = File.ReadAllText(codeBehindFilePath);

            CodeBehindMetadata metadata = PartialClassGenerator.CreateCodeBehindMetadata(className, codeBehind, codeBehindFilePath);

            TJson actual = CreateJsonTemplateFromFile(className + ".json");
            Assert.IsInstanceOf(typeof(TJson), actual);

            actual.Namespace = metadata.RootClassInfo.Namespace;
            Assert.IsNotNullOrEmpty(actual.Namespace);

            Gen2CodeGenerationModule codegenmodule = new Gen2CodeGenerationModule();
            ITemplateCodeGenerator codegen = codegenmodule.CreateGenerator(typeof(TJson), "C#", actual, metadata);
            Console.WriteLine(codegen.GenerateCode());
        }

		[Test]
		public static void GenerateInheritedPartialClass() {
			var codegen = PartialClassGenerator.GenerateTypedJsonCode(
				"Input/ChildMsg.json",
				"Input/ChildMsg.json.cs");
			var astTree = codegen.DumpAstTree();
			var code = codegen.GenerateCode();

			//Console.WriteLine(astTree);
			Console.WriteLine(code);
		}

        [Test]
        public static void EmptyArrayCodeGenerationTest(){
            var codegen = PartialClassGenerator.GenerateTypedJsonCode(
                "Input/emptyarray.json",
                null);

            Console.WriteLine(codegen.GenerateCode());
        }
    }
}




   