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
using Starcounter.Internal.JsonTemplate;
using System.IO;
using System.Collections.Generic;

namespace Test {
    /// <summary>
    /// Class TestTemplates
    /// </summary>
    [TestFixture]
    public class TestTemplates {

        /// <summary>
        /// Creates the cs from js file.
        /// </summary>
        [Test]
        public static void CreateCsFromJsFile() {
            TPuppet templ = TemplateFromJs.ReadPuppetTemplateFromFile("MySampleApp.json");
            Assert.NotNull(templ);
        }


        /// <summary>
        /// Generates the cs.
        /// </summary>
        [Test]
        public static void GenerateCs() {
            TPuppet actual = TemplateFromJs.ReadPuppetTemplateFromFile("MySampleApp.json");
            Assert.IsInstanceOf(typeof(TPuppet), actual);
            CodeGenerationModule codegenmodule = new CodeGenerationModule();
            var codegen = codegenmodule.CreateGenerator(typeof(TPuppet),"C#", actual, CodeBehindMetadata.Empty);
            Console.WriteLine(codegen.GenerateCode());
        }

        /// <summary>
        /// </summary>
        [Test]
        public static void GenerateCsFromSimpleJs() {
            TPuppet actual = TemplateFromJs.ReadPuppetTemplateFromFile("simple.json");
            actual.ClassName = "PlayerApp";

            var file = new System.IO.StreamReader("simple.facit.cs");
            var facit = file.ReadToEnd();
            file.Close();
            Assert.IsInstanceOf(typeof(TPuppet), actual);
            var codegenmodule = new CodeGenerationModule();
            ITemplateCodeGenerator codegen = codegenmodule.CreateGenerator(typeof(TPuppet), "C#", actual, CodeBehindMetadata.Empty);
            Console.WriteLine(codegen.DumpAstTree());
            var code = codegen.GenerateCode();
            Console.WriteLine(code);
            // Assert.AreEqual(facit, code);
        }

        /// <summary>
        /// </summary>
        [Test]
        public static void GenerateCsFromSuperSimpleJs() {
            TPuppet actual = TemplateFromJs.ReadPuppetTemplateFromFile("supersimple.json");
            actual.ClassName = "PlayerApp";

            Assert.IsInstanceOf(typeof(TPuppet), actual);
            var codegenmodule = new CodeGenerationModule();
            ITemplateCodeGenerator codegen = codegenmodule.CreateGenerator(typeof(TPuppet), "C#", actual, CodeBehindMetadata.Empty);
            Console.WriteLine(codegen.DumpAstTree());
            var code = codegen.GenerateCode();
            Console.WriteLine(code);
        }

        [Test]
        public static void GenerateCsFromTestMessage() {
            String className = "TestMessage";
            CodeBehindMetadata metadata = CodeBehindAnalyzer.Analyze(className, className + ".json.cs");

            TJson actual = (TJson)TemplateFromJs.CreateFromJs(typeof(TJson), File.ReadAllText(className + ".json"), false);
            Assert.IsInstanceOf(typeof(TJson), actual);

            actual.Namespace = metadata.RootNamespace;
            actual.ClassName = className;
            
            Assert.IsNotNullOrEmpty(actual.Namespace);

            CodeGenerationModule codegenmodule = new CodeGenerationModule();
            ITemplateCodeGenerator codegen = codegenmodule.CreateGenerator(typeof(TJson),"C#", actual, metadata);
            Console.WriteLine(codegen.GenerateCode());
        }

        [Test]
        public static void GenerateCsWithCodeBehind()
        {
            String className = "MySampleApp";
            CodeBehindMetadata metadata = CodeBehindAnalyzer.Analyze(className, className + ".json.cs");
            
            TPuppet actual = TemplateFromJs.ReadPuppetTemplateFromFile(className + ".json");
            Assert.IsInstanceOf(typeof(TPuppet), actual);

            actual.Namespace = metadata.RootNamespace;
            Assert.IsNotNullOrEmpty(actual.Namespace);

            CodeGenerationModule codegenmodule = new CodeGenerationModule();
            ITemplateCodeGenerator codegen = codegenmodule.CreateGenerator(typeof(TPuppet),"C#", actual, metadata);
            Console.WriteLine(codegen.GenerateCode());
        }
    }
}




   