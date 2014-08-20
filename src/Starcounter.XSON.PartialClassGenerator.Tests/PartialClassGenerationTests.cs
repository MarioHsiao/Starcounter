using NUnit.Framework;
using System;
using System.IO;
using TJson = Starcounter.Templates.TObject;
using Starcounter.Templates;
using System.Collections.Generic;

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
            Assert.IsInstanceOf<TTrigger>(child);
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
            var codegen = PartialClassGenerator.GenerateTypedJsonCode(tj, null, null);

            Console.WriteLine(codegen.GenerateCode());
        }

        [Test]
        public static void GenerateMinimalClassWithCodebehind() {
            var tj = ReadTemplate("Input/minimal.json");
            var cb = File.ReadAllText("Input/minimal.json.cs");
            var codegen = PartialClassGenerator.GenerateTypedJsonCode(tj, cb, null);
            //            var dom = codegen.GenerateAST();
            Console.WriteLine(codegen.GenerateCode());
        }

        internal static TJson ReadTemplate(string path) {
            var str = File.ReadAllText(path);
            var tj = TJson.CreateFromJson(str);
            tj.ClassName = Path.GetFileNameWithoutExtension(path);
            return tj;
        }


        [Test]
        public static void TestClassNames() {
            var code = PartialClassGenerator.GenerateTypedJsonCode(
                "Input/ThreadPage.json",
                "Input/ThreadPage.json.cs").GenerateCode();
            Console.WriteLine(code);
        }


        [Test]
        public static void TestNamespaceWithoutCodeBehind() {
            var codegen = PartialClassGenerator.GenerateTypedJsonCode(
                "Input/Namespaces.json", null);
//            var dump = codegen.DumpAstTree();
            var code = codegen.GenerateCode();
//            Console.WriteLine(dump);
            Console.WriteLine(code);
        }

		//[Test]
		//public static void TestGenerics() {
		//	var codegen = PartialClassGenerator.GenerateTypedJsonCode(
		//		"PartialClassGeneration/Generic.json",
		//		"PartialClassGeneration/Generic.json.cs");
		//	var dump = codegen.DumpAstTree();
		//	var code = codegen.GenerateCode();
		//	Console.WriteLine(dump);
		//	Console.WriteLine(code);
		//}

        [Test]
        public static void TestGlobalClassSpecifier() {
            string expected = "Starcounter.Json.JsonByExample.Metadata<TObject,Json>";
            Type t = new Json.JsonByExample.Metadata<Starcounter.Templates.TObject, Json>(null, null).GetType();
            string actual = HelperFunctions.GetGlobalClassSpecifier(t, true);
            Assert.AreEqual(expected, actual);

            expected = "Starcounter.Json.JsonByExample.Schema";
            t = new Json.JsonByExample.Schema().GetType();
            actual = HelperFunctions.GetGlobalClassSpecifier(t, true);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void TestGeneratedCodeWithArrayItemReuse() {
            string json = @"{ myarr:[ { $: {Reuse:""MyNamespace.Person""} } ] }";

            var className = "Test";
            var tobj = TObject.CreateFromMarkup<Json, TObject>("json", json, className);
            tobj.ClassName = className;

            var generator = PartialClassGenerator.GenerateTypedJsonCode(tobj, null, null);
            Console.WriteLine(generator.GenerateCode());
        }

        [Test]
        public static void TestJsonTreeWithReuse() {
            string json = @"{Header:"""",Depth:0,Count:0,Nodes:[{$:{Reuse:""NodeJson""}}]}";

            TObject tobj = Helper.Create(json, "NodeJson");
            Assert.AreEqual("NodeJson", ((TObjArr)tobj.Properties[3]).elementTypeName);

            var root = (Json)tobj.CreateInstance();
            root.Data = Helper.GetTreeData();
            
            Console.WriteLine(root.ToJson());
        }
    }
}
