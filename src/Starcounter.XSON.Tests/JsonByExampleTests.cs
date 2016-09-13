using System.Text;
using NUnit.Framework;
using Starcounter.Templates;
using Starcounter.XSON.Interfaces;
using Starcounter.XSON.JsonByExample;
using Starcounter.XSON.Templates.Factory;

namespace Starcounter.Internal.XSON.Tests {
    [TestFixture]
    public static class JsonByExampleTests {
        private static JsonByExampleMarkupReader jbeReader = new JsonByExampleMarkupReader();
        private static ITemplateFactory factory = new TemplateFactory();

        [Test]
        public static void TestBasicJsonByExample() {
            string json;
            Template template;
            
            json = "true"; // single bool value.
            template = jbeReader.CreateTemplate(json, "Test", factory);
            Assert.IsInstanceOf<TBool>(template);
            Assert.AreEqual(true, ((TBool)template).DefaultValue);
            
            json = "195"; // single integer value
            template = jbeReader.CreateTemplate(json, "Test", factory);
            Assert.IsInstanceOf<TLong>(template);
            Assert.AreEqual(195L, ((TLong)template).DefaultValue);
            
            json = "-195"; // single negative integer value
            template = jbeReader.CreateTemplate(json, "Test", factory);
            Assert.IsInstanceOf<TLong>(template);
            Assert.AreEqual(-195L, ((TLong)template).DefaultValue);
            
            json = @"""mystring"""; // single string value
            template = jbeReader.CreateTemplate(json, "Test", factory);
            Assert.IsInstanceOf<TString>(template);
            Assert.AreEqual("mystring", ((TString)template).DefaultValue);
            
            json = "195.56"; // single decimal value
            template = jbeReader.CreateTemplate(json, "Test", factory);
            Assert.IsInstanceOf<TDecimal>(template);
            Assert.AreEqual(195.56m, ((TDecimal)template).DefaultValue);
            
            // TODO:
            // How do we support double?
            json = @"1E4"; // single double value
            template = jbeReader.CreateTemplate(json, "Test", factory);
            //            Assert.IsInstanceOf<TDouble>(template);
            //            Assert.AreEqual(10000d, ((TDouble)template).DefaultValue);

            json = @"{ ""property1"": 123 }"; // object with one integer property.
            template = jbeReader.CreateTemplate(json, "Test", factory);
            Assert.IsInstanceOf<TObject>(template);

            var tobj = (TObject)template;
            Assert.AreEqual(1, tobj.Properties.Count);

            Assert.IsInstanceOf<TLong>(tobj.Properties[0]);
            Assert.AreEqual("property1", tobj.Properties[0].PropertyName);
            Assert.AreEqual(123, ((TLong)tobj.Properties[0]).DefaultValue);
        }

        [Test]
        public static void TestIgnoreAndSkipComments() {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine(@"  ""value"": 124, // Some long value! ");
            sb.AppendLine(@"  // ""value2"": 12.45,");
            sb.AppendLine(@"  ""value3"": /*23,*/ 24,");
            sb.AppendLine("}");

            Assert.DoesNotThrow(() => {
                Template template = jbeReader.CreateTemplate(sb.ToString(), "Test", factory);
            });
        }

        [Test]
        public static void TestJsonByExampleObject() {
            string json;
            Template template;

            json = @"{ ""Value1$"": ""value1"", ""Value2"": 123, ""Value3$"": false }";
            template = jbeReader.CreateTemplate(json, "Test", factory);
            Assert.IsInstanceOf<TObject>(template);

            var tobj = (TObject)template;
            Assert.AreEqual(3, tobj.Properties.Count);

            var property = ((TValue)tobj.Properties[0]);
            Assert.IsInstanceOf<TString>(property);
            Assert.AreEqual("Value1", property.PropertyName);
            Assert.AreEqual("Value1$", property.TemplateName);
            Assert.AreEqual(true, property.Editable);
            Assert.AreEqual("value1", ((TString)property).DefaultValue);

            property = ((TValue)tobj.Properties[1]);
            Assert.IsInstanceOf<TLong>(property);
            Assert.AreEqual("Value2", property.PropertyName);
            Assert.AreEqual("Value2", property.TemplateName);
            Assert.AreEqual(false, property.Editable);
            Assert.AreEqual(123L, ((TLong)property).DefaultValue);

            property = ((TValue)tobj.Properties[2]);
            Assert.IsInstanceOf<TBool>(property);
            Assert.AreEqual("Value3", property.PropertyName);
            Assert.AreEqual("Value3$", property.TemplateName);
            Assert.AreEqual(true, property.Editable);
            Assert.AreEqual(false, ((TBool)property).DefaultValue);
        }

        [Test]
        public static void TestJsonByExampleObjectWithArray() {
            string json;
            Template template;

            json = @"{ ""Value1"": [ ""arr1"", ""arr2"" ] }";
            template = jbeReader.CreateTemplate(json, "Test", factory);
            Assert.IsInstanceOf<TObject>(template);

            var tobj = (TObject)template;
            Assert.AreEqual(1, tobj.Properties.Count);

            var property = ((TValue)tobj.Properties[0]);
            Assert.IsInstanceOf<TObjArr>(property);
            Assert.AreEqual("Value1", property.PropertyName);
            Assert.AreEqual("Value1", property.TemplateName);
        }


        [Test]
        public static void TestJsonByExampleLegacySupport() {
            string json;
            Template template;
            TObject tobj;
            
            // Supported metadata properties are (case doesn't matter):
            // namespace : string
            // datatype : string 
            // bind : string
            // reuse : string

            // Test covers that all are set correctly, and incorrect metadata is detected.

            // Namespace and Datatype
            json = @"{ ""$"": { ""namespace"": ""my.custom.ns"", ""datatype"": ""my.datatype"" } }";
            template = jbeReader.CreateTemplate(json, "Test", factory);
            Assert.IsInstanceOf<TObject>(template);
            Assert.AreEqual("my.custom.ns", template.CodegenInfo.Namespace);
            Assert.AreEqual("my.datatype", template.CodegenInfo.BoundToType);

            // Bind
            json = @"{ ""Value1"": ""value"", ""$Value1"": { ""bind"": ""my.path"" } }"; 
            template = jbeReader.CreateTemplate(json, "Test", factory);
            Assert.IsInstanceOf<TObject>(template);
            Assert.AreEqual("my.path", ((TValue)((TObject)template).Properties[0]).Bind);

            // Reuse
            json = @"{ ""Value1"": { }, ""$Value1"": { ""reuse"": ""my.existing.json"" } }";
            template = jbeReader.CreateTemplate(json, "Test", factory);
            Assert.IsInstanceOf<TObject>(template);
            tobj = (TObject)template;
            Assert.IsInstanceOf<TObject>(tobj.Properties[0]);
            Assert.AreEqual("my.existing.json", tobj.Properties[0].CodegenInfo.ReuseType);

            json = @"{ ""$"": ""incorrect"" }"; // old metadata object (that is not object here)
            Assert.Throws<TemplateFactoryException>(() => {
                template = jbeReader.CreateTemplate(json, "Test", factory);
            });

            json = @"{ ""Value1"": ""value"", ""$Value1"": ""incorrect"" }"; // old metadata object (that is not object here)
            Assert.Throws<TemplateFactoryException>(() => {
                template = jbeReader.CreateTemplate(json, "Test", factory);
            });

            json = @"{ ""$"": { ""namespace"": 123 } }"; // Wrong type
            Assert.Throws<TemplateFactoryException>(() => {
                template = jbeReader.CreateTemplate(json, "Test", factory);
            });

            json = @"{ ""$"": { ""datatype"": 123.34 } }"; // Wrong type
            Assert.Throws<TemplateFactoryException>(() => {
                template = jbeReader.CreateTemplate(json, "Test", factory);
            });

            json = @"{ ""$"": { ""bind"": true } }"; // Wrong type
            Assert.Throws<TemplateFactoryException>(() => {
                template = jbeReader.CreateTemplate(json, "Test", factory);
            });

            json = @"{ ""$"": { ""reuse"": [1] } }"; // Wrong type
            Assert.Throws<TemplateFactoryException>(() => {
                template = jbeReader.CreateTemplate(json, "Test", factory);
            });

            json = @"{ ""$"": { ""reuse"": { } } }"; // Wrong type
            Assert.Throws<TemplateFactoryException>(() => {
                template = jbeReader.CreateTemplate(json, "Test", factory);
            });

            json = @"{ ""$"": { ""ConvertTo"": 123 } }"; // Unsupported metadata
            Assert.Throws<TemplateFactoryException>(() => {
                template = jbeReader.CreateTemplate(json, "Test", factory);
            });
        }
    }
}
