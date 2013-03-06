using System;
using System.Collections.Generic;
using NUnit.Framework;
using Starcounter.Internal.Application.CodeGeneration;
using Starcounter.Internal.JsonTemplate;
using Starcounter.Internal.Uri;
using Starcounter.Templates;
using Starcounter.Templates.Interfaces;
using CodeGen = Starcounter.Internal.Application.CodeGeneration.Serialization;

namespace Starcounter.Apps.CodeGeneration.Tests {
    /// <summary>
    /// 
    /// </summary>
    public class JsonSerializeTest {
        //[Test]
        //public static void DebugPregeneratedSerializationCode() {
        //    //String className = "TestMessage";
        //    //CodeBehindMetadata metadata = CodeBehindAnalyzer.Analyze(className, className + ".json.cs");

        //    //TApp actual = TemplateFromJs.ReadFile(className + ".json");
        //    //Assert.IsInstanceOf(typeof(TApp), actual);

        //    //actual.Namespace = metadata.RootNamespace;
        //    //Assert.IsNotNullOrEmpty(actual.Namespace);



        //    //CodeGenerationModule codegenmodule = new CodeGenerationModule();
        //    //ITemplateCodeGenerator codegen = codegenmodule.CreateGenerator("C#", actual, metadata);

        //    string generatedCode = System.IO.File.ReadAllText("TestMessage.g.cs");
        //    string codebehindCode = System.IO.File.ReadAllText("TestMessage.json.cs");
        //    GenereratedJsonCodeCompiler.CompileCode(generatedCode, codebehindCode, "MySampleNamespace.TestMessage");
        //}

        /// <summary>
        /// Generates the parse tree overview.
        /// </summary>
        [Test]
        public static void GenerateSerializationParseTreeOverview() {
            List<RequestProcessorMetaData> handlers = RegisterTemplatesForApp(CreateTApp());
            Console.WriteLine(ParseTreeGenerator.BuildParseTree(handlers).ToString(false));
        }

        /// <summary>
        /// Generates the parse tree details.
        /// </summary>
        [Test]
        public static void GenerateSerializationParseTreeDetails() {
            List<RequestProcessorMetaData> handlers = RegisterTemplatesForApp(CreateTApp());
            Console.WriteLine(ParseTreeGenerator.BuildParseTree(handlers).ToString(true));
        }

        /// <summary>
        /// 
        /// </summary>
        [Test]
        public static void GenerateSerializationAstTreeOverview() {
            CodeGen.AstNode astTree = CodeGen.AstTreeGenerator.BuildAstTree(CreateTApp());
            Console.WriteLine(astTree.ToString());
        }

        /// <summary>
        /// 
        /// </summary>
        [Test]
        public static void GenerateSerializationCode() {
            CodeGen.AstNode astTree = CodeGen.AstTreeGenerator.BuildAstTree(CreateTApp());
            Console.WriteLine(astTree.GenerateCsSourceCode());
        }

        private static TPuppet CreateTApp() {
            TPuppet template = TemplateFromJs.ReadPuppetTemplateFromFile("TestMessage.json");
            template.ClassName = "TestMessage";
            return template;
        }

        private static List<RequestProcessorMetaData> RegisterTemplatesForApp(TPuppet appTemplate) {
            List<RequestProcessorMetaData> handlers = new List<RequestProcessorMetaData>();
            foreach (Template child in appTemplate.Children) {
                RequestProcessorMetaData rp = new RequestProcessorMetaData();
                rp.UnpreparedVerbAndUri = child.Name;
                rp.Code = child;
                handlers.Add(rp);
            }
            return handlers;
        }

        ///// <summary>
        ///// Creates the sample app.
        ///// </summary>
        ///// <returns>AppAndTemplate.</returns>
        //private static AppAndTemplate CreateSampleApp() {
        //    dynamic template = TemplateFromJs.ReadFile("TestMessage.json");
        //    dynamic app = new App() { Template = template };

        //    app.FirstName = "Cliff";
        //    app.LastName = "Barnes";

        //    var itemApp = app.Items.Add();
        //    itemApp.Description = "Take a nap!";
        //    itemApp.IsDone = false;

        //    itemApp = app.Items.Add();
        //    itemApp.Description = "Fix Apps!";
        //    itemApp.IsDone = true;

        //    return new AppAndTemplate(app, template);
        //}
    }

    /// <summary>
    /// Struct AppAndTemplate
    /// </summary>
    internal struct AppAndTemplate {
        /// <summary>
        /// The app
        /// </summary>
        public readonly Puppet App;
        /// <summary>
        /// The template
        /// </summary>
        public readonly Template Template;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppAndTemplate" /> struct.
        /// </summary>
        /// <param name="app">The app.</param>
        /// <param name="template">The template.</param>
        public AppAndTemplate(Puppet app, Template template) {
            App = app;
            Template = template;
        }
    }
}
