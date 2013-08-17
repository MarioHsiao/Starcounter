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
            NRoot dom = (NRoot)codegen.GenerateAST();

            Console.WriteLine(codegen.DumpAstTree());
        }

    }
}
