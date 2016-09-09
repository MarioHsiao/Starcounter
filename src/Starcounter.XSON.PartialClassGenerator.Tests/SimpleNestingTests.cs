using System.IO;
using NUnit.Framework;
using Starcounter.Templates;
using Starcounter.XSON.PartialClassGenerator;
using SXP = Starcounter.XSON.PartialClassGenerator;

namespace Starcounter.Internal.XSON.PartialClassGeneration.Tests {
    [TestFixture]
    static class SimpleNestingTests {
        internal static TObject ReadTemplate(string path) {
            var str = File.ReadAllText(path);
            var tj = (TObject)TObject.CreateFromJson(str);
            tj.ClassName = Path.GetFileNameWithoutExtension(path);
            return tj;
        }

        /// <summary>
        /// When a JSON object is declared as a property in another JSON object, the generated partial class
        /// is declared as an inner class unless the developer has not declared a JSON-mapping attribute
        /// using the [myfile.json.myproj] attribute. This test asserts that this is indeed the case.
        /// See also FlattenedClassForNestedJson().
        /// </summary>
        [Test]
        public static void UntouchedNesting() {
            var tj = ReadTemplate("Input\\ParentChild.json");
            var cb = File.ReadAllText("Input\\ParentChild.json.cs.v5");
            var codegen = SXP.PartialClassGenerator.GenerateTypedJsonCode(tj, cb, null);
            var dom = codegen.GenerateAST();

            //var dump = TreeHelper.GenerateTreeString(dom, (IReadOnlyTree node) => {
            //    var str = node.GetType().Name;
            //    str += " : " + node.ToString();
            //    return str;
            //});

            //Helper.ConsoleWriteLine(dump);
            Helper.ConsoleWriteLine(codegen.GenerateCode());

           // Assert.AreEqual(typeof(AstRoot), dom.GetType() );
           // Assert.AreEqual(typeof(AstJsonClass), dom.Children[0].GetType());
           // Assert.AreEqual("Parent", ((AstJsonClass)dom.Children[0]).ClassStemIdentifier);


          //  var rootClass = (AstBase)dom.Children[0];
          //  var nestedClass = (AstBase)dom.Children[0].Children[3];
          //  Assert.AreEqual("Child2Json",((AstClass)nestedClass).ClassStemIdentifier);
        }

        [Test]
        public static void FlattenedClassForNestedJson() {
            var tj = ReadTemplate("Input\\ParentChild.json");
            var cb = File.ReadAllText("Input\\ParentChild.json.cs.v3");
            var codegen = SXP.PartialClassGenerator.GenerateTypedJsonCode(tj, cb, null);
            var dom = codegen.GenerateAST();

            var dump = TreeHelper.GenerateTreeString(dom, (IReadOnlyTree node) => {
                return node.ToString();
            });

            Helper.ConsoleWriteLine(dump);

            Assert.AreEqual(typeof(AstRoot), dom.GetType());
            var rootClass = (AstBase)dom.Children[0];
            //var otherClass = (AstBase)dom.Children[0].Children[3];
            //var noLongerNestedClass = (AstClass)dom.Children[1];
            Helper.ConsoleWriteLine(codegen.GenerateCode());

            //Assert.AreEqual("Hello", ((AstClass)dom.Children[0]).ClassStemIdentifier); // Name gotten from code-behind in ParentChild.json.v3.cs

            //Assert.AreEqual("Mail", noLongerNestedClass.ClassStemIdentifier); // Name gotten from code-behind in ParentChild.json.v3.cs
            //Helper.ConsoleWriteLine(dump);
        }
    }
}
