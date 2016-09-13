// ***********************************************************************
// <copyright file="TestTemplates.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.IO;
using NUnit.Framework;
using Starcounter.Templates;
using Starcounter.XSON.Interfaces;
using Starcounter.XSON.Metadata;
using Starcounter.XSON.PartialClassGenerator;
using SXP = Starcounter.XSON.PartialClassGenerator;

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
        private static TObject CreateJsonTemplateFromFile(string filePath) {
            string json = File.ReadAllText("Input\\" + filePath);
            string className = Path.GetFileNameWithoutExtension(filePath);
            var tobj = (TObject)Template.CreateFromMarkup("json", json, className);
            tobj.ClassName = className;
            return tobj;
        }

        /// <summary>
        /// Creates the cs from js file.
        /// </summary>
        [Test]
        public static void CreateCsFromJsFile() {
            TObject templ = CreateJsonTemplateFromFile("MySampleApp.json");
            Assert.NotNull(templ);
        }

		/// <summary>
		/// Generates the cs.
		/// </summary>
		[Test]
		public static void GenerateCsGen2() {
            TObject actual = CreateJsonTemplateFromFile("MySampleApp.json");
			Assert.IsInstanceOf(typeof(TObject), actual);
			Gen2CodeGenerationModule codegenmodule = new Gen2CodeGenerationModule();
			var codegen = codegenmodule.CreateGenerator(typeof(TObject), "C#", actual, CodeBehindMetadata.Empty);
			Helper.ConsoleWriteLine(codegen.GenerateCode());
		}

        /// <summary>
        /// </summary>
        [Test]
        public static void GenerateCsFromSimpleJs() {
            TObject actual = CreateJsonTemplateFromFile("simple.json");
            actual.ClassName = "PlayerApp";

//            var file = new System.IO.StreamReader("simple.facit.cs");
//            var facit = file.ReadToEnd();
//            file.Close();
            Assert.IsInstanceOf(typeof(TObject), actual);
            var codegenmodule = new Gen2CodeGenerationModule();
            ITemplateCodeGenerator codegen = codegenmodule.CreateGenerator(typeof(TObject), "C#", actual, CodeBehindMetadata.Empty);
            Helper.ConsoleWriteLine(codegen.DumpAstTree());
            var code = codegen.GenerateCode();
            Helper.ConsoleWriteLine(code);
            // Assert.AreEqual(facit, code);
        }

        /// <summary>
        /// </summary>
        [Test]
        public static void GenerateCsFromPrimitiveJs() {
            TValue actual = (TValue)Template.CreateFromJson(@"{""Items"":[19]}");
            actual.ClassName = "PlayerApp";

            Assert.IsInstanceOf(typeof(TObject), actual);

            var codegenmodule = new Gen2CodeGenerationModule();
            ITemplateCodeGenerator codegen = codegenmodule.CreateGenerator(typeof(TObject), "C#", actual, CodeBehindMetadata.Empty);
//            Helper.ConsoleWriteLine(codegen.DumpAstTree());
            var code = codegen.GenerateCode();
            Helper.ConsoleWriteLine(code);
        }



        /// <summary>
        /// </summary>
        [Test]
        public static void GenerateCsFromSuperSimpleJs() {
            TObject actual = CreateJsonTemplateFromFile("supersimple.json");
            actual.ClassName = "PlayerApp";

            Assert.IsInstanceOf(typeof(TObject), actual);
            var codegenmodule = new Gen2CodeGenerationModule();
            ITemplateCodeGenerator codegen = codegenmodule.CreateGenerator(typeof(TObject), "C#", actual, CodeBehindMetadata.Empty);
            Helper.ConsoleWriteLine(codegen.DumpAstTree());
            var code = codegen.GenerateCode();
            Helper.ConsoleWriteLine(code);
        }

        [Test]
        public static void GenerateCsWithCodeBehind()
        {
            String className = "MySampleApp";
            string codeBehindFilePath = "Input\\" + className + ".json.cs";
            string codeBehind = File.ReadAllText(codeBehindFilePath);

            CodeBehindMetadata metadata = SXP.PartialClassGenerator.CreateCodeBehindMetadata(className, codeBehind, codeBehindFilePath);

            TObject actual = CreateJsonTemplateFromFile(className + ".json");
            Assert.IsInstanceOf(typeof(TObject), actual);

            actual.CodegenInfo.Namespace = metadata.RootClassInfo.Namespace;
            Assert.IsNotNullOrEmpty(actual.CodegenInfo.Namespace);

            Gen2CodeGenerationModule codegenmodule = new Gen2CodeGenerationModule();
            ITemplateCodeGenerator codegen = codegenmodule.CreateGenerator(typeof(TObject), "C#", actual, metadata);
            Helper.ConsoleWriteLine(codegen.GenerateCode());
        }

		[Test]
		public static void GenerateInheritedPartialClass() {
			var codegen = SXP.PartialClassGenerator.GenerateTypedJsonCode(
				"Input/ChildMsg.json",
				"Input/ChildMsg.json.cs");
			var astTree = codegen.DumpAstTree();
			var code = codegen.GenerateCode();

			//Helper.ConsoleWriteLine(astTree);
			Helper.ConsoleWriteLine(code);
		}

        [Test]
        public static void EmptyArrayCodeGenerationTest(){
            var codegen = SXP.PartialClassGenerator.GenerateTypedJsonCode(
                "Input/emptyarray.json",
                null);

            Helper.ConsoleWriteLine(codegen.GenerateCode());
        }

        [Test]
        public static void GeneratePrimitiveCode1() {
            var codegen = SXP.PartialClassGenerator.GenerateTypedJsonCode(
                "Input/Primitive.json",
                "Input/Primitive.json.cs");

            var ast = codegen.DumpAstTree();
            var code = codegen.GenerateCode();

//            Helper.ConsoleWriteLine(ast);
            Helper.ConsoleWriteLine(code);
        }

        [Test]
        public static void GeneratePrimitiveCode2() {
            var codegen = SXP.PartialClassGenerator.GenerateTypedJsonCode(
                "Input/Primitive2.json",
                null);

            var ast = codegen.DumpAstTree();
            var code = codegen.GenerateCode();

//            Helper.ConsoleWriteLine(ast);
            Helper.ConsoleWriteLine(code);
        }

        [Test]
        public static void GeneratePrimitiveCode3() {
            var codegen = SXP.PartialClassGenerator.GenerateTypedJsonCode(
                "Input/Primitive3.json",
                null);

            var ast = codegen.DumpAstTree();
            var code = codegen.GenerateCode();

            Helper.ConsoleWriteLine(ast);

            Helper.ConsoleWriteLine("\n\n");
            Helper.ConsoleWriteLine(code);
        }

        [Test]
        public static void GeneratePrimitiveArrayCode() {
            var codegen = SXP.PartialClassGenerator.GenerateTypedJsonCode(
                "Input/Primitive.json",
                null);

            var ast = codegen.DumpAstTree();
            var code = codegen.GenerateCode();

            Helper.ConsoleWriteLine(ast);
            Helper.ConsoleWriteLine(code);
        }

        [Test]
        public static void GeneratePrimitiveUntypedArrayCode() {
            TObjArr tarr = (TObjArr)Template.CreateFromJson("[]");

            var codegen = SXP.PartialClassGenerator.GenerateTypedJsonCode(
                tarr,
                null,
                null);

            var ast = codegen.DumpAstTree();
            var code = codegen.GenerateCode();

            Helper.ConsoleWriteLine(ast);
            Helper.ConsoleWriteLine(code);
        }
    }
}




   