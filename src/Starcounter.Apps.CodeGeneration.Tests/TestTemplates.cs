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
            TApp templ = TemplateFromJs.ReadFile("MySampleApp.json");
            Assert.NotNull(templ);
        }


        /// <summary>
        /// Generates the cs.
        /// </summary>
        [Test]
        public static void GenerateCs() {
            TApp actual = TemplateFromJs.ReadFile("MySampleApp.json");
            Assert.IsInstanceOf(typeof(TApp), actual);
            CodeGenerationModule codegenmodule = new CodeGenerationModule();
            var codegen = codegenmodule.CreateGenerator("C#", actual, CodeBehindMetadata.Empty);
            Console.WriteLine(codegen.GenerateCode());
        }

        /// <summary>
        /// Generates the cs from simple js.
        /// </summary>
        [Test]
        public static void GenerateCsFromSimpleJs() {
           TApp actual = TemplateFromJs.ReadFile("simple.json");
           actual.ClassName = "PlayerApp";

           var file = new System.IO.StreamReader("simple.facit.cs");
           var facit = file.ReadToEnd();
           file.Close();
           Assert.IsInstanceOf(typeof(TApp), actual);
           var codegenmodule = new CodeGenerationModule();
           ITemplateCodeGenerator codegen = codegenmodule.CreateGenerator("C#",actual, CodeBehindMetadata.Empty);
           var code = codegen.GenerateCode();
           Console.WriteLine(code);
           // Assert.AreEqual(facit, code);
        }

        [Test]
        public static void GenerateCsFromTestMessage() {
            String className = "TestMessage";
            CodeBehindMetadata metadata = CodeBehindAnalyzer.Analyze(className, className + ".json.cs");

            TApp actual = TemplateFromJs.ReadFile(className + ".json");
            Assert.IsInstanceOf(typeof(TApp), actual);

            actual.Namespace = metadata.RootNamespace;
            Assert.IsNotNullOrEmpty(actual.Namespace);

            CodeGenerationModule codegenmodule = new CodeGenerationModule();
            ITemplateCodeGenerator codegen = codegenmodule.CreateGenerator("C#", actual, metadata);
            Console.WriteLine(codegen.GenerateCode());
        }

        [Test]
        public static void GenerateCsWithCodeBehind()
        {
            String className = "MySampleApp";
            CodeBehindMetadata metadata = CodeBehindAnalyzer.Analyze(className, className + ".json.cs");
            
            TApp actual = TemplateFromJs.ReadFile(className + ".json");
            Assert.IsInstanceOf(typeof(TApp), actual);

            actual.Namespace = metadata.RootNamespace;
            Assert.IsNotNullOrEmpty(actual.Namespace);

            CodeGenerationModule codegenmodule = new CodeGenerationModule();
            ITemplateCodeGenerator codegen = codegenmodule.CreateGenerator("C#", actual, metadata);
            Console.WriteLine(codegen.GenerateCode());
        }
    }
}




   