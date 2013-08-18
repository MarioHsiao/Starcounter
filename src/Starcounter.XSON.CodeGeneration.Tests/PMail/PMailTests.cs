using NUnit.Framework;
using Starcounter;
using Starcounter.Internal.Application.CodeGeneration;
using Starcounter.Templates;
using System;
using System.IO;


namespace Starcounter.Internal.XSON.PartialClassGeneration.Tests {

    [TestFixture]
    static class PMailTests {

        internal static TJson ReadTemplate(string path) {
            var str = File.ReadAllText(path);
            var tj = TJson.CreateFromJson(str);
            tj.ClassName = Path.GetFileNameWithoutExtension(path);
            return tj;
        }

        [Test]
        public static void GenerateCsDOM() {
            var tj = ReadTemplate("PMail\\ContactApp.json");
            var cb = File.ReadAllText("PMail\\ContactApp.json.cs");
            var codegen = PartialClassGenerator.GenerateTypedJsonCode(tj, cb, null);
            var dom = codegen.GenerateAST();

            //            var str = TreeHelper.GenerateTreeString(dom, (IReadOnlyTree node) => node.ToString() ); 
            var str = TreeHelper.GenerateTreeString(dom, (IReadOnlyTree node) => {
                var ret = node.GetType().Name;
                ret += " : " + ((NBase)node).Name;
                //var s = node.ToString();
                //if (s.Length > 40 ) {
                //    s = s.Substring(0,37)+"...";
                //}
                //ret += "::" + s;
                return ret;
            }); 
            Console.WriteLine(str);
        }

    }
}
