using NUnit.Framework;
using Starcounter;
using Starcounter.Internal.Application.CodeGeneration;
using Starcounter.Internal.MsBuild.Codegen;
using Starcounter.Templates;
using System;
using System.IO;


namespace Starcounter.Internal.XSON.PartialClassGeneration.Tests {

    [TestFixture]
    static class TestSimpleNesting {

        internal static TJson ReadTemplate(string path) {
            var str = File.ReadAllText(path);
            var tj = TJson.CreateFromJson(str);
            tj.ClassName = Path.GetFileNameWithoutExtension(path);
            return tj;
        }

        [Test]
        public static void UntouchedNesting() {
            var tj = ReadTemplate("Nesting\\ParentChild.json");
            var cb = File.ReadAllText("Nesting\\ParentChild.json.cs.v5" );
            var codegen = PartialClassGenerator.GenerateTypedJsonCode(tj, cb, null);
            var dom = codegen.GenerateAST();

            var dump = TreeHelper.GenerateTreeString(dom, (IReadOnlyTree node) => {
                var str = node.GetType().Name;
                str += " : " + node.ToString();
                return str;
            });

            Assert.AreEqual(typeof(AstRoot), dom.GetType() );
            var rootClass = (AstBase)dom.Children[0];
            var nestedClass = (AstBase)dom.Children[0].Children[3];
            Assert.AreEqual("FocusedJson",nestedClass.Name);
            Console.WriteLine(dump);
        }


        [Test]
        public static void FlattenedClassForNestedJson() {
            var tj = ReadTemplate("Nesting\\ParentChild.json");
            var cb = File.ReadAllText("Nesting\\ParentChild.json.cs.v3");
            var codegen = PartialClassGenerator.GenerateTypedJsonCode(tj, cb, null);
            var dom = codegen.GenerateAST();

            var dump = TreeHelper.GenerateTreeString(dom, (IReadOnlyTree node) => {
                var str = node.GetType().Name;
                str += " : " + node.ToString();
                return str;
            });

            Assert.AreEqual(typeof(AstRoot), dom.GetType());
            var rootClass = (AstBase)dom.Children[0];
            var otherClass = (AstBase)dom.Children[0].Children[3];
            var noLongerNestedClass = (AstBase)dom.Children[1];
            Assert.AreEqual("ContactPage", noLongerNestedClass.Name); // Name gotten from code-behind in ParentChild.json.v3.cs
            Console.WriteLine(dump);
        }


    }
}
