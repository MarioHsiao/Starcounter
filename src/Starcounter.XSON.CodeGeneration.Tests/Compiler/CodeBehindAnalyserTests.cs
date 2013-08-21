using System;
using Starcounter.XSON.Metadata;
using NUnit.Framework;
using Starcounter.Internal.XSON;
using System.IO;
using Starcounter.XSON.Compiler.Mono;

namespace Starcounter.Internal.XSON.PartialClassGeneration.Tests {
    public class CodeBehindAnalyserTests {


        private static CodeBehindMetadata MonoAnalyze(string className, string path) {
            return CodeBehindAnalyzer.Analyze(className,
                File.ReadAllText(path),path );
        }

        [Test]
        public static void CodeBehindAnalyzeTest() {
            CodeBehindMetadata monoMetadata;
//            CodeBehindMetadata roslynMetadata; 

//            var roslyn = new Starcounter.XSON.Compiler.Roslyn.RoslynCSharpCompiler();

            monoMetadata = MonoAnalyze("Simple", @"Compiler\simple.json.cs");
//            roslynMetadata = roslyn.AnalyzeCodeBehind("Simple", @"Compiler\simple.json.cs");
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

        private static void AssertMetadataAreEqual(CodeBehindMetadata roslyn, CodeBehindMetadata mono) {
            Assert.AreEqual(roslyn.RootClassInfo.AutoBindToDataObject, mono.RootClassInfo.AutoBindToDataObject);
            Assert.AreEqual(roslyn.RootClassInfo.GenericArgument, mono.RootClassInfo.GenericArgument);
            Assert.AreEqual(roslyn.RootClassInfo.Namespace, mono.RootClassInfo.Namespace);

            Assert.AreEqual(roslyn.RootClassInfo.InputBindingList.Count, mono.RootClassInfo.InputBindingList.Count);
            for (int i = 0; i < roslyn.RootClassInfo.InputBindingList.Count; i++) {
                var monoInput = mono.RootClassInfo.InputBindingList[i];
                var roslynInput = roslyn.RootClassInfo.InputBindingList[i];

                Assert.AreEqual(roslynInput.DeclaringClassName, monoInput.DeclaringClassName);
                Assert.AreEqual(roslynInput.DeclaringClassNamespace, monoInput.DeclaringClassNamespace);
                Assert.AreEqual(roslynInput.FullInputTypeName, monoInput.FullInputTypeName);
            }

            Assert.AreEqual(roslyn.JsonPropertyMapList.Count, mono.JsonPropertyMapList.Count);
            for (int i = 0; i < roslyn.JsonPropertyMapList.Count; i++) {
                var monoMap = mono.JsonPropertyMapList[i];
                var roslynMap = roslyn.JsonPropertyMapList[i];

                Assert.AreEqual(roslynMap.AutoBindToDataObject, monoMap.AutoBindToDataObject);
                Assert.AreEqual(roslynMap.ClassName, monoMap.ClassName);
                Assert.AreEqual(roslynMap.GenericArgument, monoMap.GenericArgument);
                Assert.AreEqual(roslynMap.RawJsonMapAttribute, monoMap.RawJsonMapAttribute);
                Assert.AreEqual(roslynMap.Namespace, monoMap.Namespace);

                Assert.AreEqual(roslynMap.ParentClasses.Count, monoMap.ParentClasses.Count);
                for (int k = 0; k < roslynMap.ParentClasses.Count; k++) {
                    Assert.AreEqual(roslynMap.ParentClasses[k], monoMap.ParentClasses[k]);
                }
            }
        }
    }
}
