using System;
using System.IO;
using NUnit.Framework;
using TJson = Starcounter.Templates.TObject;

namespace Starcounter.Internal.XSON.PartialClassGeneration.Tests {
    [TestFixture]
    static class InputMappingTests {
        internal static TJson ReadTemplate(string path) {
            var str = File.ReadAllText(path);
            var tj = TJson.CreateFromJson(str);
            tj.ClassName = Path.GetFileNameWithoutExtension(path);
            return tj;
        }

        [Test]
        public static void TestInputGeneration() {
            var tj = ReadTemplate("Input\\Company.json");
            var cb = File.ReadAllText("Input\\Company.json.cs");
            var codegen = PartialClassGenerator.GenerateTypedJsonCode(tj, cb, null);
            var dom = codegen.GenerateAST();

            var dump = TreeHelper.GenerateTreeString(dom, (IReadOnlyTree node) => {
                var str = node.GetType().Name;
                str += " : " + node.ToString();
                return str;
            });

//            Assert.AreEqual(typeof(AstRoot), dom.GetType() );
//            var rootClass = (AstBase)dom.Children[0];
//            var nestedClass = (AstBase)dom.Children[0].Children[3];
//            Assert.AreEqual("FocusedJson",nestedClass.Name);

            //            Helper.ConsoleWriteLine(dump);
            Helper.ConsoleWriteLine(codegen.GenerateCode());
        }

        [Test]
        public static void TestInputGeneration2() {
            var tj = ReadTemplate("Input\\MailApp.json");
            var cb = File.ReadAllText("Input\\MailApp.json.cs");
            var codegen = PartialClassGenerator.GenerateTypedJsonCode(tj, cb, null);
            var dom = codegen.GenerateAST();

            var dump = TreeHelper.GenerateTreeString(dom, (IReadOnlyTree node) => {
                var str = node.GetType().Name;
                str += " : " + node.ToString();
                return str;
            });
            Helper.ConsoleWriteLine(codegen.GenerateCode());
        }
    }
}
