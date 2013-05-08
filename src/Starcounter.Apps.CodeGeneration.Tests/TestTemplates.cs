﻿// ***********************************************************************
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
using Starcounter.XSON.Metadata;

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
            TJson templ = TemplateFromJs.ReadJsonTemplateFromFile("MySampleApp.json");
            Assert.NotNull(templ);
        }


        /// <summary>
        /// Generates the cs.
        /// </summary>
        [Test]
        public static void GenerateCs() {
            TJson actual = TemplateFromJs.ReadJsonTemplateFromFile("MySampleApp.json");
            Assert.IsInstanceOf(typeof(TJson), actual);
            CodeGenerationModule codegenmodule = new CodeGenerationModule();
            var codegen = codegenmodule.CreateGenerator(typeof(TJson), "C#", actual, CodeBehindMetadata.Empty);
            Console.WriteLine(codegen.GenerateCode());
        }

        /// <summary>
        /// </summary>
        [Test]
        public static void GenerateCsFromSimpleJs() {
            TJson actual = TemplateFromJs.ReadJsonTemplateFromFile("simple.json");
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
            TJson actual = TemplateFromJs.ReadJsonTemplateFromFile("supersimple.json");
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
            CodeBehindMetadata metadata = (CodeBehindMetadata)JsonFactory.Compiler.AnalyzeCodeBehind(className, className + ".json.cs");

            TJson actual = (TJson)TemplateFromJs.CreateFromJs(File.ReadAllText(className + ".json"), false);
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
            CodeBehindMetadata metadata = (CodeBehindMetadata)JsonFactory.Compiler.AnalyzeCodeBehind(className, className + ".json.cs");

            TJson actual = TemplateFromJs.ReadJsonTemplateFromFile(className + ".json");
            Assert.IsInstanceOf(typeof(TJson), actual);

            actual.Namespace = metadata.RootNamespace;
            Assert.IsNotNullOrEmpty(actual.Namespace);

            CodeGenerationModule codegenmodule = new CodeGenerationModule();
            ITemplateCodeGenerator codegen = codegenmodule.CreateGenerator(typeof(TJson), "C#", actual, metadata);
            Console.WriteLine(codegen.GenerateCode());
        }
    }
}




   