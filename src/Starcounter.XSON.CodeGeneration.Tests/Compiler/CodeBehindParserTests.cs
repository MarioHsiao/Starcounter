using System;
using Starcounter.XSON.Metadata;
using NUnit.Framework;
using Starcounter.Internal.XSON;
using System.IO;
using Starcounter.XSON.Compiler.Mono;

namespace Starcounter.Internal.XSON.PartialClassGeneration.Tests {
    public class CodeBehindParserTests {


        private static CodeBehindMetadata MonoAnalyze(string className, string path) {
            return CodeBehindParser.Analyze(className,
                File.ReadAllText(path),path );
        }

//        [Test]
//        public static void AnalyzeSimpleCase() {
//            CodeBehindMetadata monoMetadata;
//            monoMetadata = MonoAnalyze("Simple", @"Compiler\simple.json.cs");
//            Assert.AreEqual("Simple", monoMetadata.JsonPropertyMapList[0].ClassName);
//            Assert.AreEqual("T,T2", monoMetadata.JsonPropertyMapList[0].GenericArg);
//            Assert.AreEqual("Json", monoMetadata.JsonPropertyMapList[0].BaseClassName);
//            Assert.AreEqual("T", monoMetadata.JsonPropertyMapList[0].BaseClassGenericArg);
//            Assert.AreEqual("MySampleNamespace", monoMetadata.JsonPropertyMapList[0].Namespace);
//        }

        [Test]
        public static void CodeBehindAnalyzeTest() {
            CodeBehindMetadata monoMetadata;
            
			monoMetadata = MonoAnalyze("Simple", @"Compiler\simple.json.cs");
			Assert.AreEqual(null, monoMetadata.RootClassInfo.BoundDataClass);
			Assert.AreEqual(null, monoMetadata.RootClassInfo.RawDebugJsonMapAttribute);
			Assert.AreEqual("Json", monoMetadata.RootClassInfo.BaseClassName);
			Assert.AreEqual("MySampleNamespace", monoMetadata.RootClassInfo.Namespace);
			
			Assert.AreEqual(2, monoMetadata.JsonPropertyMapList.Count);
			Assert.AreEqual("OrderItem", monoMetadata.JsonPropertyMapList[1].BoundDataClass);
			Assert.AreEqual("MyOtherNs.MySubNS.SubClass", monoMetadata.JsonPropertyMapList[1].BaseClassName);
			Assert.AreEqual("Apapa.json.Items", monoMetadata.JsonPropertyMapList[1].RawDebugJsonMapAttribute);

			Assert.Throws<Exception>(() => MonoAnalyze("Incorrect", @"Compiler\incorrect.json.cs"));

//            AssertMetadataAreEqual(roslynMetadata, monoMetadata);

			//monoMetadata = MonoAnalyze("Complex", @"Compiler\complex.json.cs");
			//roslynMetadata = roslyn.AnalyzeCodeBehind("Complex", @"Compiler\complex.json.cs");
			//AssertMetadataAreEqual(roslynMetadata, monoMetadata);

			//monoMetadata = MonoAnalyze("MySampleApp", @"MySampleApp.json.cs");
			//roslynMetadata = roslyn.AnalyzeCodeBehind("MySampleApp", @"MySampleApp.json.cs");
			//AssertMetadataAreEqual(roslynMetadata, monoMetadata);

			//monoMetadata = MonoAnalyze("Incorrect", @"Compiler\Incorrect.json.cs");
			//roslynMetadata = roslyn.AnalyzeCodeBehind("Incorrect", @"Compiler\Incorrect.json.cs");            
			//AssertMetadataAreEqual(roslynMetadata, monoMetadata);
        }
    }
}
