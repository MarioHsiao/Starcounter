using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using NUnit.Framework;
using Starcounter.Internal.Application.CodeGeneration;
using Starcounter.Templates;
using Starcounter.Templates.Interfaces;

namespace Starcounter.Apps.CodeGeneration.Tests {
    /// <summary>
    /// 
    /// </summary>
    public static class JsonSerializeTest {
        [TestFixtureSetUp]
        public static void InitializeTest() {
            Obj.Factory = new JsonFactoryImpl();
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
            Console.WriteLine(AstTreeGenerator.BuildAstTree(objTemplate).GenerateCsSourceCode());
        }

        [Test]
        public static void DebugPregeneratedCode() {
            TObj objTemplate;

            objTemplate = (TObj)Obj.Factory.CreateJsonTemplate(File.ReadAllText("supersimple.json"));
            dynamic simple = (Json)objTemplate.CreateInstance(null);
            simple.PlayerId = 666;
            var item = simple.Accounts.Add();
            item.AccountId = 123;

            byte[] buffer = new byte[4096];

            unsafe {
                fixed (byte* p = buffer) {
                    int i = simple.ToJson((IntPtr)p, buffer.Length);

                    simple = (Json)objTemplate.CreateInstance(null);
                    i = simple.Populate((IntPtr)p, i);                       
                }
            }
        }
    }
}
