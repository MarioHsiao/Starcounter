
using NUnit.Framework;
using System;
using System.IO;
using TJson = Starcounter.Templates.Schema<Starcounter.Json<object>>;


namespace Starcounter.Internal.XSON.PartialClassGeneration.Tests {

    [TestFixture]
    public class TestInheritance {
        [TestFixtureSetUp]
        public static void InitializeTest() {
        }


        [Test]
        public static void GenerateMinimalClassWithoutCodebehind() {
            var tj = ReadTemplate("Inheritance/minimal.json");
            var codegen = PartialClassGenerator.GenerateTypedJsonCode(tj, null, null);
            Console.WriteLine(codegen.GenerateCode());
        }

        [Test]
        public static void GenerateMinimalClassWithCodebehind() {
            var tj = ReadTemplate("Inheritance/minimal.json");
            var cb = File.ReadAllText("Inheritance/minimal.json.cs");
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
                "Inheritance/ThreadPage.json",
                "Inheritance/ThreadPage.json.cs").GenerateCode();
            Console.WriteLine( code );
        }


        [Test]
        public static void TestGenerics() {
            var codegen = PartialClassGenerator.GenerateTypedJsonCode(
                "Inheritance/Generic.json",
                "Inheritance/Generic.json.cs");
            var dump = codegen.DumpAstTree();
            var code = codegen.GenerateCode();
            Console.WriteLine(dump);
            Console.WriteLine(code);
        }
    }
}
