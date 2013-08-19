using NUnit.Framework;
using Starcounter;
using Starcounter.Internal.Application.CodeGeneration;
using Starcounter.Internal.MsBuild.Codegen;
using Starcounter.Templates;
using System;
using System.IO;


namespace Starcounter.Internal.XSON.PartialClassGeneration.Tests {

    [TestFixture]
    static class TestInputMapping {

        internal static TJson ReadTemplate(string path) {
            var str = File.ReadAllText(path);
            var tj = TJson.CreateFromJson(str);
            tj.ClassName = Path.GetFileNameWithoutExtension(path);
            return tj;
        }

        [Test]
        public static void TestInputGeneration() {
            var tj = ReadTemplate("InputGeneration\\Company.json");
            var cb = File.ReadAllText("InputGeneration\\Company.json.cs");
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

            //            Console.WriteLine(dump);
            Console.WriteLine(codegen.GenerateCode());
        }

    }
}
