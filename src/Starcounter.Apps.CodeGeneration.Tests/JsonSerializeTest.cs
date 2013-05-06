using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Starcounter.Internal.Application.CodeGeneration;
using Starcounter.Internal.JsonTemplate;
using Starcounter.Templates;

namespace Starcounter.Apps.CodeGeneration.Tests {
    /// <summary>
    /// 
    /// </summary>
    public class JsonSerializeTest {
        [Test]
        public void GenerateSerializationParseTreeOverview() {
            TObj mainTemplate;
            List<TemplateMetadata> templates = new List<TemplateMetadata>();

            mainTemplate = TemplateFromJs.CreateFromJs(File.ReadAllText("TestMessage.json"), false);
            foreach (var template in mainTemplate.Children) {
                templates.Add(new TemplateMetadata(template));
            }

            ParseNode node = ParseTreeGenerator.BuildParseTree(templates);
            Console.WriteLine(node.ToString());
        }
    }
}
