using NUnit.Framework;
using Starcounter;
using Starcounter.Internal.Application.CodeGeneration;
using Starcounter.Templates;
using System;
using System.IO;


namespace Starcounter.Internal.XSON.PartialClassGeneration.Tests {

    [TestFixture]
    static class DetectVersionToUse {

        internal static TJson ReadTemplate(string path) {
            var str = File.ReadAllText(path);
            var tj = TJson.CreateFromJson(str);
            tj.ClassName = Path.GetFileNameWithoutExtension(path);
            return tj;
        }

        [Test]
        public static void GenerateCsDOM1() {
            _GenerateCsDOM("v1", "NRoot"); // Generation 1
        }

        [Test]
        public static void GenerateCsDOM2() {
            _GenerateCsDOM("v2", "NRoot"); // Generation 2
        }

        [Test]
        public static void GenerateCsDOM3() {
            _GenerateCsDOM("v3", "AstRoot"); // Generation 1
        }

        [Test]
        public static void GenerateCsDOM4() {
            _GenerateCsDOM("v4","AstRoot"); // Generation 1
        }

        private static void _GenerateCsDOM(string version,string root) {
            var tj = ReadTemplate("Nesting\\ParentChild.json");
            var cb = File.ReadAllText("Nesting\\ParentChild.json.cs."+version);
            var codegen = PartialClassGenerator.GenerateTypedJsonCode(tj, cb, null);
            var dom = codegen.GenerateAST();

            //            var str = TreeHelper.GenerateTreeString(dom, (IReadOnlyTree node) => node.ToString() ); 
            var str = TreeHelper.GenerateTreeString(dom);
            Console.WriteLine(str);

            Assert.AreEqual(root, dom.GetType().Name);
        }

    }
}
