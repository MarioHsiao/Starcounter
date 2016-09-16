using NUnit.Framework;
using System;
using System.IO;
using TJson = Starcounter.Templates.TObject;
using Starcounter.Templates;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SXP = Starcounter.XSON.PartialClassGenerator;

namespace Starcounter.Internal.XSON.PartialClassGeneration.Tests {
    [TestFixture]
    public class PartialClassGenerationTests {
        [TestFixtureSetUp]
        public static void InitializeTest() {
        }

        [Test]
        public static void TestMinimalTemplate() {
            var tj = ReadTemplate("Input/minimal.json");

            var child = tj.Properties[0]; // Text$
            Assert.AreEqual("Text$", child.TemplateName);
            Assert.AreEqual("Text", child.PropertyName);
            Assert.IsTrue(child.Editable);
            Assert.IsInstanceOf<TString>(child);
            Assert.IsNotNull(child.Parent);

            child = tj.Properties[1]; // ServerCode
            Assert.AreEqual("ServerCode", child.TemplateName);
            Assert.AreEqual("ServerCode", child.PropertyName);
            Assert.IsFalse(child.Editable);
            Assert.IsInstanceOf<TLong>(child);
            Assert.IsNotNull(child.Parent);

            child = tj.Properties[4]; // CustomAction$
            Assert.AreEqual("CustomAction$", child.TemplateName);
            Assert.AreEqual("CustomAction", child.PropertyName);
            Assert.IsTrue(child.Editable);
            Assert.IsInstanceOf<TLong>(child);
            Assert.IsNotNull(child.Parent);

            child = tj.Properties[5]; // List1
            Assert.AreEqual("List1", child.TemplateName);
            Assert.AreEqual("List1", child.PropertyName);
            Assert.IsInstanceOf<TArray<Json>>(child);
            Assert.IsNotNull(child.Parent);

            child = ((TArray<Json>)child).ElementType;
            Assert.IsNotNull(child.Parent);
        }

        [Test]
        public static void GenerateMinimalClassWithoutCodebehind() {
            var tj = ReadTemplate("Input/minimal.json");
            var codegen = SXP.PartialClassGenerator.GenerateTypedJsonCode(tj, null, null);

            Helper.ConsoleWriteLine(codegen.GenerateCode());
        }

        [Test]
        public static void GenerateReusedJson() {
            var tj = ReadTemplate("Input/ReuseJson.json");
            var codegen = SXP.PartialClassGenerator.GenerateTypedJsonCode(tj, null, null);

            Helper.ConsoleWriteLine(codegen.GenerateCode());
        }

        [Test]
        public static void GenerateReusedJsonWithAdditionalProperties() {
            var tj = ReadTemplate("Input/ReuseJson2.json");
            var codegen = SXP.PartialClassGenerator.GenerateTypedJsonCode(tj, null, null);

            Helper.ConsoleWriteLine(codegen.GenerateCode());
        }

        [Test]
        public static void GenerateMinimalClassWithCodebehind() {
            var tj = ReadTemplate("Input/minimal.json");
            var cb = File.ReadAllText("Input/minimal.json.cs");
            var codegen = SXP.PartialClassGenerator.GenerateTypedJsonCode(tj, cb, null);
            //            var dom = codegen.GenerateAST();
            Helper.ConsoleWriteLine(codegen.GenerateCode());
        }

        internal static TObject ReadTemplate(string path) {
            var str = File.ReadAllText(path);
            var tj = (TObject)TObject.CreateFromJson(str);
            tj.CodegenInfo.ClassName = Path.GetFileNameWithoutExtension(path);
            return tj;
        }


        [Test]
        public static void TestClassNames() {
            var code = SXP.PartialClassGenerator.GenerateTypedJsonCode(
                "Input/ThreadPage.json",
                "Input/ThreadPage.json.cs").GenerateCode();
            Helper.ConsoleWriteLine(code);
        }


        [Test]
        public static void TestNamespaceWithoutCodeBehind() {
            var codegen = SXP.PartialClassGenerator.GenerateTypedJsonCode(
                "Input/Namespaces.json", null);
//            var dump = codegen.DumpAstTree();
            var code = codegen.GenerateCode();
//            Helper.ConsoleWriteLine(dump);
            Helper.ConsoleWriteLine(code);
        }

		//[Test]
		//public static void TestGenerics() {
		//	var codegen = PartialClassGenerator.GenerateTypedJsonCode(
		//		"PartialClassGeneration/Generic.json",
		//		"PartialClassGeneration/Generic.json.cs");
		//	var dump = codegen.DumpAstTree();
		//	var code = codegen.GenerateCode();
		//	Helper.ConsoleWriteLine(dump);
		//	Helper.ConsoleWriteLine(code);
		//}

        [Test]
        public static void TestGlobalClassSpecifier() {
            string expected = "Starcounter.Json.JsonByExample.Metadata<Json,TObject>";
            Type t = new Json.JsonByExample.Metadata<Json, TObject>(null, null).GetType();
            string actual = HelperFunctions.GetGlobalClassSpecifier(t, true);
            Assert.AreEqual(expected, actual);

            expected = "Starcounter.Templates.TObject";
            t = typeof(TObject);
            actual = HelperFunctions.GetGlobalClassSpecifier(t, true);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void TestGeneratedCodeWithArrayItemReuse() {
            string json = @"{ myarr:[ { $: {Reuse:""MyNamespace.Person""} } ] }";

            var className = "Test";
            var tobj = (TObject)Template.CreateFromMarkup("json", json, className);
            tobj.CodegenInfo.ClassName = className;

            var generator = SXP.PartialClassGenerator.GenerateTypedJsonCode(tobj, null, null);
            Helper.ConsoleWriteLine(generator.GenerateCode());
        }

        [Test]
        public static void TestAliasesInGeneratedCode() {
            string json = @"{ myarr:[ { $: {Reuse:""Test.myarr""} } ] }";

            var className = "Test";
            var tobj = (TObject)Template.CreateFromMarkup("json", json, className);
            tobj.CodegenInfo.ClassName = className;

            var generator = SXP.PartialClassGenerator.GenerateTypedJsonCode(tobj, null, null);
            string code = generator.GenerateCode();
            string pattern = @"using .*\..* = .*;";

            var matches = Regex.Matches(code, pattern);
            foreach (Match match in matches) {
                Helper.ConsoleWriteLine(match.Value);
            }

            Assert.IsTrue(matches.Count == 0);
        }


        [Test]
        public static void TestJsonTreeWithReuse() {
            string json = @"{Header:"""",Depth:0,Count:0,Nodes:[{$:{Reuse:""NodeJson""}}]}";

            TObject tobj = Helper.Create(json, "NodeJson");

            var root = (Json)tobj.CreateInstance();
            root.Data = Helper.GetTreeData();
            
            Helper.ConsoleWriteLine(root.ToJson());
        }

        [Test]
        public static void TestMetadataProperties() {
            TObject tobj = null;
            string json;

            // Editable property with valid metadata
            json = @"{""MyValue$"":1,""$MyValue"":{""Bind"":""A""}}";
            Assert.DoesNotThrow(() => { tobj = Helper.Create(json, "Test1"); });
            Assert.AreEqual(1, tobj.Properties.Count);
            Assert.AreEqual("A", ((TValue)tobj.Properties[0]).Bind);
            Assert.AreEqual(true, ((TValue)tobj.Properties[0]).Editable);
            
            // Readonly property with valid metadata
            json = @"{""MyValue"":1,""$MyValue"":{""Bind"":""A""}}";
            Assert.DoesNotThrow(() => { tobj = Helper.Create(json, "Test2"); });
            Assert.AreEqual(1, tobj.Properties.Count);
            Assert.AreEqual("A", ((TValue)tobj.Properties[0]).Bind);
            Assert.AreEqual(false, ((TValue)tobj.Properties[0]).Editable);

            // Editable property with invalid metadata (but still allowed, editable just ignored).
            json = @"{""MyValue$"":1,""$MyValue$"":{""Bind"":""A""}}";
            Assert.DoesNotThrow(() => { tobj = Helper.Create(json, "Test3"); });
            Assert.AreEqual(1, tobj.Properties.Count);
            Assert.AreEqual("A", ((TValue)tobj.Properties[0]).Bind);
            Assert.AreEqual(true, ((TValue)tobj.Properties[0]).Editable);
            
            // Readonly property with invalid metadata (but still allowed, editable just ignored).
            json = @"{""MyValue"":1,""$MyValue$"":{""Bind"":""A""}}";
            Assert.DoesNotThrow(() => { tobj = Helper.Create(json, "Test4"); });
            Assert.AreEqual(1, tobj.Properties.Count);
            Assert.AreEqual("A", ((TValue)tobj.Properties[0]).Bind);
            Assert.AreEqual(false, ((TValue)tobj.Properties[0]).Editable);
        }

        [Test]
        public static void TestInstanceTypeAssignment() {
            var json = "{"
                     + @" ""ElapsedTime"": 1.0, "
                     + @" ""Page"": { ""ChildOfPage"": 1.0 },"
                     + @" ""Page2"": { ""PartialTime"": 2.0 },"
                     + @" ""Page3"": { },"
                     + "}";

            var codebehind = "public partial class Foo : Json {"
                        + "  static Foo() {"
                        + "    DefaultTemplate.ElapsedTime.InstanceType = typeof(double);"
                        + "    DefaultTemplate.Page3.InstanceType = typeof(MyOtherJson);"
                        + "    DefaultTemplate.Page.ChildOfPage.InstanceType = typeof(System.Decimal);"
                        + "  }"
                        + "  [Foo_json.Page2]"
                        + "  partial class Page2Json : Json {"
                        + "    static Page2Json() {"
                        + "      DefaultTemplate.PartialTime.InstanceType = typeof(Double);"
                        + "    }"
                        + "  }"
                        + "}";
            
            TValue template = (TValue)Template.CreateFromJson(json);
            template.CodegenInfo.ClassName = "Foo";
            var codegen = SXP.PartialClassGenerator.GenerateTypedJsonCode(template, codebehind, "Foo");
            string code = codegen.GenerateCode();
        }
    }
}
