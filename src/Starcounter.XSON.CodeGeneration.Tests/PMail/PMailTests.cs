using NUnit.Framework;
using Starcounter;
using Starcounter.Templates;
using System;
using System.IO;


namespace Starcounter.Internal.XSON.PartialClassGeneration.Tests {

    [TestFixture]
    static class PMailTests {

        internal static TJson ReadTemplate(string path) {
            var str = File.ReadAllText(path);
            return TJson.CreateFromJson(str);
        }

        [Test]
        public static void GenerateCsDOM() {
            var tj = ReadTemplate("PMail\\ContactApp.json");
            Assert.NotNull(tj);

//            Assert.AreEqual( "ContactApp", tj.ClassName );
//                        var className = Path.GetFileNameWithoutExtension(jsonFilePath);


         //   PartialClassGenerator.GenerateTypedJsonCode(
//            Starcounter.Internal.XSON.PartialClassGenerator.
        }

    }
}
