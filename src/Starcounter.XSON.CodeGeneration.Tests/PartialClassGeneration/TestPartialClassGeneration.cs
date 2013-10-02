
using NUnit.Framework;
using System;
using System.IO;
using TJson = Starcounter.Templates.TObject;


namespace Starcounter.Internal.XSON.PartialClassGeneration.Tests {

    [TestFixture]
    public class TestPartialClassGeneration {
        [TestFixtureSetUp]
        public static void InitializeTest() {
        }


        [Test]
        public static void GenerateMinimalClassWithoutCodebehind() {
            var tj = ReadTemplate("PartialClassGeneration/minimal.json");
            var codegen = PartialClassGenerator.GenerateTypedJsonCode(tj, null, null);
            Console.WriteLine(codegen.GenerateCode());
        }

        [Test]
        public static void GenerateMinimalClassWithCodebehind() {
            var tj = ReadTemplate("PartialClassGeneration/minimal.json");
            var cb = File.ReadAllText("PartialClassGeneration/minimal.json.cs");
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
                "PartialClassGeneration/ThreadPage.json",
                "PartialClassGeneration/ThreadPage.json.cs").GenerateCode();
            Console.WriteLine(code);
        }


        [Test]
        public static void TestNamespaceWithoutCodeBehind() {
            var codegen = PartialClassGenerator.GenerateTypedJsonCode(
                "PartialClassGeneration/Namespaces.json", null);
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
    }
}
