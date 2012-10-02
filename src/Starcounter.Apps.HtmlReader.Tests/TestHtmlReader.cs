

using System;
using NUnit.Framework;
using Starcounter.Templates;
namespace Starcounter.Internal.Application.JsonReader.Tests {
    public class JsToTemplateTests {


        [Test]
        public static void CreateFromHtmlFile() {
            AppTemplate template = TemplateFromHtml.CreateFromHtmlFile("testtemplate.html");
            Assert.NotNull(template);
            Assert.IsInstanceOf<StringProperty>(template.Properties[0]);
            Assert.IsInstanceOf<ListingProperty>(template.Properties[1]);
        }


        [Test]
        public static void CreateFromHtmlFile_Misplaced() {
            try {
                AppTemplate template = TemplateFromHtml.CreateFromHtmlFile("template\\misplaced.html");
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
                Console.WriteLine("Exception was successfully provoked");
                return;
            }
            Assert.Fail();
        }
    }
}
