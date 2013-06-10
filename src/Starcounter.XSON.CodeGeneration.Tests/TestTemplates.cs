// ***********************************************************************
// <copyright file="TestTemplates.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using System;
using NUnit.Framework;
using Starcounter.Templates;
using Starcounter.Internal.Application.CodeGeneration;
using Starcounter.Templates.Interfaces;
using System.IO;
using Starcounter.XSON.Metadata;
using Starcounter.Internal;
using Starcounter.XSON.CodeGeneration;

namespace Starcounter.XSON.CodeGeneration.Tests {
    /// <summary>
    /// Class TestTemplates
    /// </summary>
    [TestFixture]
    public class TestTemplates {
        [TestFixtureSetUp]
        public static void InitializeTest() {
            CodeGeneration.Initializer.InitializeXSON();
        }

        /// <summary>
        /// Creates the cs from js file.
        /// </summary>
        [Test]
        public static void CreateCsFromJsFile() {
            TJson templ = (TJson)Obj.Factory.CreateJsonTemplateFromFile("MySampleApp.json");
            Assert.NotNull(templ);
        }

        /// <summary>
        /// Generates the cs.
        /// </summary>
        [Test]
        public static void GenerateCs() {
            TJson actual = (TJson)Obj.Factory.CreateJsonTemplateFromFile("MySampleApp.json");
            Assert.IsInstanceOf(typeof(TJson), actual);
            CodeGenerationModule codegenmodule = new CodeGenerationModule();
            var codegen = codegenmodule.CreateGenerator(typeof(TJson), "C#", actual, CodeBehindMetadata.Empty);
            Console.WriteLine(codegen.GenerateCode());
        }

        /// <summary>
        /// </summary>
        [Test]
        public static void GenerateCsFromSimpleJs() {
            TJson actual = (TJson)Obj.Factory.CreateJsonTemplateFromFile("simple.json");
            actual.ClassName = "PlayerApp";

            var file = new System.IO.StreamReader("simple.facit.cs");
            var facit = file.ReadToEnd();
            file.Close();
            Assert.IsInstanceOf(typeof(TJson), actual);
            var codegenmodule = new CodeGenerationModule();
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
            TJson actual = (TJson)Obj.Factory.CreateJsonTemplateFromFile("supersimple.json");
            actual.ClassName = "PlayerApp";

            Assert.IsInstanceOf(typeof(TJson), actual);
            var codegenmodule = new CodeGenerationModule();
            ITemplateCodeGenerator codegen = codegenmodule.CreateGenerator(typeof(TJson), "C#", actual, CodeBehindMetadata.Empty);
            Console.WriteLine(codegen.DumpAstTree());
            var code = codegen.GenerateCode();
            Console.WriteLine(code);
        }

        [Test]
        public static void GenerateCsFromTestMessage() {
            String className = "TestMessage";

            CodeBehindMetadata metadata = (CodeBehindMetadata)Obj.Factory.CreateCodeBehindMetadata(className, className + ".json.cs");

            TJson actual = (TJson)Obj.Factory.CreateJsonTemplateFromFile(className + ".json");
            Assert.IsInstanceOf(typeof(TJson), actual);

            actual.Namespace = metadata.RootNamespace;
            actual.ClassName = className;

            Assert.IsNotNullOrEmpty(actual.Namespace);

            CodeGenerationModule codegenmodule = new CodeGenerationModule();
            ITemplateCodeGenerator codegen = codegenmodule.CreateGenerator(typeof(TJson), "C#", actual, metadata);

            Console.WriteLine(codegen.GenerateCode());
        }

        [Test]
        public static void GenerateCsWithCodeBehind()
        {
            String className = "MySampleApp";
            CodeBehindMetadata metadata = (CodeBehindMetadata)Obj.Factory.CreateCodeBehindMetadata(className, className + ".json.cs");

            TJson actual = (TJson)Obj.Factory.CreateJsonTemplateFromFile(className + ".json");
            Assert.IsInstanceOf(typeof(TJson), actual);

            actual.Namespace = metadata.RootNamespace;
            Assert.IsNotNullOrEmpty(actual.Namespace);

            CodeGenerationModule codegenmodule = new CodeGenerationModule();
            ITemplateCodeGenerator codegen = codegenmodule.CreateGenerator(typeof(TJson), "C#", actual, metadata);
            Console.WriteLine(codegen.GenerateCode());
        }
    }
}




   