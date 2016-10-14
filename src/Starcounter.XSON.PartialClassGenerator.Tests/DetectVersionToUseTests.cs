using System;
using System.IO;
using NUnit.Framework;
using Starcounter.Templates;
using SXP = Starcounter.XSON.PartialClassGenerator;

namespace Starcounter.Internal.XSON.PartialClassGeneration.Tests {
    [TestFixture]
    static class DetectVersionToUse {
        internal static TObject ReadTemplate(string path) {
            var str = File.ReadAllText(path);
            var tj = (TObject)TObject.CreateFromJson(str);
            tj.CodegenInfo.ClassName = Path.GetFileNameWithoutExtension(path);
            return tj;
        }

        [Test]
        public static void PartialWithoutMapPlusNestedWithMap() {
            _GenerateCsDOM("v1", "AstRoot"); // Generation 1
        }

        [Test]
        public static void SinglePartialWithoutMap() {
            _GenerateCsDOM("v2", "AstRoot"); // Generation 2
        }

        [Test]
        public static void MappedRootAndChild() {
            _GenerateCsDOM("v3", "AstRoot"); // Generation 1
        }

        [Test]
        public static void SingleMapForChild() {
            _GenerateCsDOM("v4", "AstRoot"); // Generation 1
        }

        [Test]
        public static void SingleMapForRoot() {
            _GenerateCsDOM("v5", "AstRoot"); // Generation 1
        }

        private static void _GenerateCsDOM(string version,string root) {
            var tj = ReadTemplate("Input\\ParentChild.json");
            var cb = File.ReadAllText("Input\\ParentChild.json.cs."+version);
            var codegen = SXP.PartialClassGenerator.GenerateTypedJsonCode(tj, cb, null);
            var dom = codegen.GenerateAST();

            //            var str = TreeHelper.GenerateTreeString(dom, (IReadOnlyTree node) => node.ToString() ); 
            var str = TreeHelper.GenerateTreeString(dom);
            Helper.ConsoleWriteLine(str);

            Assert.AreEqual(root, dom.GetType().Name);
        }
    }
}
