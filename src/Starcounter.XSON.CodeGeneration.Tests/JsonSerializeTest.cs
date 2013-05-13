using System;
using System.IO;
using NUnit.Framework;
using Starcounter.Internal;
using Starcounter.Internal.Application.CodeGeneration;
using Starcounter.Templates;
using Starcounter.XSON.CodeGeneration;

namespace Starcounter.Apps.CodeGeneration.Tests {
    /// <summary>
    /// 
    /// </summary>
    public static class JsonSerializeTest {
        [TestFixtureSetUp]
        public static void InitializeTest() {
            Obj.Factory = new TypedJsonFactory();
        }

        [Test]
        public static void GenerateSerializationParseTreeOverview() {
            TObj objTemplate;

            objTemplate = (TObj)Obj.Factory.CreateJsonTemplate(File.ReadAllText("supersimple.json"));

            ParseNode parseTree = ParseTreeGenerator.BuildParseTree(objTemplate);
            Console.WriteLine(parseTree.ToString());
        }

        [Test]
        public static void GenerateSerializationAstTreeOverview() {
            TObj objTemplate;

            objTemplate = (TObj)Obj.Factory.CreateJsonTemplate(File.ReadAllText("supersimple.json"));
            Console.WriteLine(AstTreeGenerator.BuildAstTree(objTemplate).ToString());
        }

        [Test]
        public static void GenerateSerializationCsCode() {
            TObj objTemplate;

            objTemplate = (TObj)Obj.Factory.CreateJsonTemplate(File.ReadAllText("supersimple.json"));
            objTemplate.ClassName = "PreGenerated";
            Console.WriteLine(AstTreeGenerator.BuildAstTree(objTemplate).GenerateCsSourceCode());
        }

        [Test]
        public static void DebugPregeneratedSerializationCode() {
            TObj objTemplate;

            objTemplate = (TObj)Obj.Factory.CreateJsonTemplate(File.ReadAllText("supersimple.json"));
            dynamic simple = (Json)objTemplate.CreateInstance(null);
            simple.PlayerId = 666;
            var item = simple.Accounts.Add();
            item.AccountId = 123;

            byte[] buffer = new byte[4096];
            TypedJsonSerializer serializer = new __starcountergenerated__.PreGeneratedSerializer();
            int usedBufferSize;
            int usedAfterPopulation;

            unsafe {
                fixed (byte* p = buffer) {
                    usedBufferSize = serializer.Serialize((IntPtr)p, buffer.Length, simple);

                    simple = objTemplate.CreateInstance(null);
                    usedAfterPopulation = serializer.PopulateFromJson((IntPtr)p, usedBufferSize, simple);

                    Assert.AreEqual(usedBufferSize, usedAfterPopulation);
                }
            }

        }
    }
}
