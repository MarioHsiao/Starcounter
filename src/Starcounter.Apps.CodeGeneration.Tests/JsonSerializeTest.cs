using System;
using System.Collections.Generic;
using NUnit.Framework;
using Starcounter.Internal.Application.JsonReader;
using Starcounter.Internal.Uri;
using Starcounter.Templates;
using CodeGen = Starcounter.Internal.Application.CodeGeneration.Serialization;

namespace Starcounter.Apps.CodeGeneration.Tests {
    /// <summary>
    /// 
    /// </summary>
    public class JsonSerializeTest {
        /// <summary>
        /// Generates the parse tree overview.
        /// </summary>
        [Test]
        public static void GenerateParseTreeOverview() {
            AppTemplate template = TemplateFromJs.ReadFile("MySampleApp.json");
            template.ClassName = "PlayerApp";
            List<RequestProcessorMetaData> handlers = RegisterTemplatesForApp(template);
            Console.WriteLine(ParseTreeGenerator.BuildParseTree(handlers).ToString(false));
        }

        /// <summary>
        /// Generates the parse tree details.
        /// </summary>
        [Test]
        public static void GenerateParseTreeDetails() {
            AppTemplate template = TemplateFromJs.ReadFile("simple.json");
            template.ClassName = "PlayerApp";
            List<RequestProcessorMetaData> handlers = RegisterTemplatesForApp(template);
            Console.WriteLine(ParseTreeGenerator.BuildParseTree(handlers).ToString(true));
        }

        /// <summary>
        /// 
        /// </summary>
        [Test]
        public static void GenerateAstTreeOverview() {
            AppTemplate template = TemplateFromJs.ReadFile("simple.json");
            template.ClassName = "PlayerApp";
            CodeGen.AstNode astTree = CodeGen.AstTreeGenerator.BuildAstTree(template);
            Console.WriteLine(astTree.ToString());
        }

        /// <summary>
        /// 
        /// </summary>
        [Test]
        public static void GenerateDeserializationCode() {
            AppTemplate template = TemplateFromJs.ReadFile("simple.json");
            template.ClassName = "PlayerApp";
            CodeGen.AstNode astTree = CodeGen.AstTreeGenerator.BuildAstTree(template);
            Console.WriteLine(astTree.GenerateCsSourceCode());
        }

        /// <summary>
        /// Creates the sample app.
        /// </summary>
        /// <returns>AppAndTemplate.</returns>
        private static AppAndTemplate CreateSampleApp() {
            dynamic template = TemplateFromJs.ReadFile("simple.json");
            dynamic app = new App() { Template = template };

            app.FirstName = "Cliff";
            app.LastName = "Barnes";

            var itemApp = app.Items.Add();
            itemApp.Description = "Take a nap!";
            itemApp.IsDone = false;

            itemApp = app.Items.Add();
            itemApp.Description = "Fix Apps!";
            itemApp.IsDone = true;

            return new AppAndTemplate(app, template);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="appTemplate"></param>
        private static List<RequestProcessorMetaData> RegisterTemplatesForApp(AppTemplate appTemplate) {
            List<RequestProcessorMetaData> handlers = new List<RequestProcessorMetaData>();
            foreach (Template child in appTemplate.Children) {
                if (child is ActionProperty)
                    continue;

                RequestProcessorMetaData rp = new RequestProcessorMetaData();
                rp.UnpreparedVerbAndUri = child.Name;
                rp.Code = child;
                handlers.Add(rp);
            }
            return handlers;
        }
    }

    /// <summary>
    /// Struct AppAndTemplate
    /// </summary>
    internal struct AppAndTemplate {
        /// <summary>
        /// The app
        /// </summary>
        public readonly App App;
        /// <summary>
        /// The template
        /// </summary>
        public readonly Template Template;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppAndTemplate" /> struct.
        /// </summary>
        /// <param name="app">The app.</param>
        /// <param name="template">The template.</param>
        public AppAndTemplate(App app, Template template) {
            App = app;
            Template = template;
        }
    }
}
