using System;
using System.IO;
using NUnit.Framework;
using Starcounter.XSON.Compiler.Mono;
using Starcounter.XSON.Metadata;

namespace Starcounter.Internal.XSON.PartialClassGeneration.Tests {
    public class CodeBehindParserTests {
        private static CodeBehindMetadata MonoAnalyze(string className, string path) {
            return CodeBehindParser.Analyze(className,
                File.ReadAllText(path),path );
        }

        [Test]
        public static void CodeBehindSimpleAnalyzeTest() {
            CodeBehindMetadata monoMetadata;
            
			monoMetadata = MonoAnalyze("Simple", @"Input\simple.json.cs");
			Assert.AreEqual(null, monoMetadata.RootClassInfo.BoundDataClass);
			Assert.AreEqual(null, monoMetadata.RootClassInfo.RawDebugJsonMapAttribute);
			Assert.AreEqual("Json", monoMetadata.RootClassInfo.BaseClassName);
			Assert.AreEqual("MySampleNamespace", monoMetadata.RootClassInfo.Namespace);
			
			Assert.AreEqual(2, monoMetadata.JsonPropertyMapList.Count);
			Assert.AreEqual("OrderItem", monoMetadata.JsonPropertyMapList[1].BoundDataClass);
			Assert.AreEqual("MyOtherNs.MySubNS.SubClass", monoMetadata.JsonPropertyMapList[1].BaseClassName);
			Assert.AreEqual("Apapa.json.Items", monoMetadata.JsonPropertyMapList[1].RawDebugJsonMapAttribute);

			Assert.AreEqual(3, monoMetadata.UsingDirectives.Count);
			Assert.AreEqual("System", monoMetadata.UsingDirectives[0]);
			Assert.AreEqual("Starcounter", monoMetadata.UsingDirectives[1]);
			Assert.AreEqual("ScAdv=Starcounter.Advanced", monoMetadata.UsingDirectives[2]);
        }

		[Test]
		public static void CodeBehindComplexAnalyzeTest() {
			CodeBehindMetadata monoMetadata;

			monoMetadata = MonoAnalyze("Complex", @"Input\complex.json.cs");
			Assert.AreEqual("Order", monoMetadata.RootClassInfo.BoundDataClass);
			Assert.AreEqual("Complex_json", monoMetadata.RootClassInfo.RawDebugJsonMapAttribute);
			Assert.AreEqual("MyBaseJsonClass", monoMetadata.RootClassInfo.BaseClassName);
			Assert.AreEqual("MySampleNamespace", monoMetadata.RootClassInfo.Namespace);

			Assert.AreEqual(6, monoMetadata.JsonPropertyMapList.Count);
			Assert.AreEqual("OrderItem", monoMetadata.JsonPropertyMapList[1].BoundDataClass);
			Assert.AreEqual("Json", monoMetadata.JsonPropertyMapList[1].BaseClassName);
			Assert.AreEqual("json.ActivePage.SubPage1.SubPage2.SubPage3", monoMetadata.JsonPropertyMapList[1].RawDebugJsonMapAttribute);

			Assert.AreEqual(3, monoMetadata.UsingDirectives.Count);
			Assert.AreEqual("System", monoMetadata.UsingDirectives[0]);
			Assert.AreEqual("MySampleNamespace.Something", monoMetadata.UsingDirectives[1]);
			Assert.AreEqual("SomeOtherNamespace", monoMetadata.UsingDirectives[2]);
		}

		[Test]
		public static void CodeBehindIncorrectAnalyzeTest() {
            Exception ex;
            
            ex = Assert.Throws<Exception>(() => MonoAnalyze("Incorrect", @"Input\incorrect.json.cs"));
            Assert.IsTrue(ex.Message.Contains("Generic declaration"));

            ex = Assert.Throws<Exception>(() => MonoAnalyze("Incorrect2", @"Input\incorrect2.json.cs"));
            Assert.IsTrue(ex.Message.Contains("constructors are not"));
		}
    }
}
