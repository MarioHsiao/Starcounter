
using NUnit.Framework;
using System;
using System.IO;
using TJson = Starcounter.Templates.Schema;


namespace Starcounter.Internal.XSON.PartialClassGeneration.Tests {

    [TestFixture]
    public class TestInheritance {
        [TestFixtureSetUp]
        public static void InitializeTest() {
        }


        

        internal static TJson ReadTemplate(string path) {
            var str = File.ReadAllText(path);
            var tj = TJson.CreateFromJson(str);
            tj.ClassName = Path.GetFileNameWithoutExtension(path);
            return tj;
        }


//        [Test]
//        public static void TestClassNames() {
//            var code = PartialClassGenerator.GenerateTypedJsonCode(
//                "Inheritance/ThreadPage.json",
//                "Inheritance/ThreadPage.json.cs").GenerateCode();
//            Console.WriteLine( code );
//        }

    }
}
