using System;
using Starcounter.XSON.Metadata;
using NUnit.Framework;
using Mono = Starcounter.XSON.Compiler.Mono;
using Roslyn = Starcounter.XSON.Compiler.Roslyn;

namespace Starcounter.XSON.CodeGeneration.Tests {
    public class TestCodeCompile {

        [Test]
        public static void CodeBehindAnalyzeTest() {
            CodeBehindMetadata monoMetadata;
            CodeBehindMetadata roslynMetadata; 

            Mono.MonoCSharpCompiler mono = new Mono.MonoCSharpCompiler();
            Roslyn.RoslynCSharpCompiler roslyn = new Roslyn.RoslynCSharpCompiler();

            monoMetadata = mono.AnalyzeCodeBehind("Simple", @"Compiler\simple.json.cs");
            roslynMetadata = roslyn.AnalyzeCodeBehind("Simple", @"Compiler\simple.json.cs");
            AssertMetadataAreEqual(roslynMetadata, monoMetadata);

            monoMetadata = mono.AnalyzeCodeBehind("Complex", @"Compiler\complex.json.cs");
            roslynMetadata = roslyn.AnalyzeCodeBehind("Complex", @"Compiler\complex.json.cs");
            AssertMetadataAreEqual(roslynMetadata, monoMetadata);

            roslynMetadata = roslyn.AnalyzeCodeBehind("MySampleApp", @"MySampleApp.json.cs");
            monoMetadata = mono.AnalyzeCodeBehind("MySampleApp", @"MySampleApp.json.cs");
            
            AssertMetadataAreEqual(roslynMetadata, monoMetadata);
        }

        private static void AssertMetadataAreEqual(CodeBehindMetadata roslyn, CodeBehindMetadata mono) {
            Assert.AreEqual(roslyn.AutoBindToDataObject, mono.AutoBindToDataObject);
            Assert.AreEqual(roslyn.GenericArgument, mono.GenericArgument);
            Assert.AreEqual(roslyn.RootNamespace, mono.RootNamespace);

            Assert.AreEqual(roslyn.InputBindingList.Count, mono.InputBindingList.Count);
            for (int i = 0; i < roslyn.InputBindingList.Count; i++) {
                var monoInput = mono.InputBindingList[i];
                var roslynInput = roslyn.InputBindingList[i];

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
                Assert.AreEqual(roslynMap.JsonMapName, monoMap.JsonMapName);
                Assert.AreEqual(roslynMap.Namespace, monoMap.Namespace);

                Assert.AreEqual(roslynMap.ParentClasses.Count, monoMap.ParentClasses.Count);
                for (int k = 0; k < roslynMap.ParentClasses.Count; k++) {
                    Assert.AreEqual(roslynMap.ParentClasses[k], monoMap.ParentClasses[k]);
                }
            }
        }
    }
}
