using System;
using System.IO;
using NUnit.Framework;
using Starcounter.XSON.Compiler.Mono;
using Starcounter.XSON.Metadata;

namespace Starcounter.Internal.XSON.PartialClassGeneration.Tests {

    public class CodeBehindParserTests {

        private static CodeBehindMetadata ParserAnalyze(string className, string path, bool useRoslynParser = false) {
            return CodeBehindParser.Analyze(className,
                File.ReadAllText(path), path, useRoslynParser);
        }

        [Test]
        public static void CodeBehindSimpleAnalyzeTest() {
			var monoMetadata = ParserAnalyze("Simple", @"Input\simple.json.cs");
            var roslynMetadata = ParserAnalyze("Simple", @"Input\simple.json.cs", true);

            foreach (var metadata in new[] { monoMetadata, roslynMetadata }) {
                Assert.AreEqual(null, metadata.RootClassInfo.BoundDataClass);
                Assert.AreEqual("Simple_json", metadata.RootClassInfo.RawDebugJsonMapAttribute);
                Assert.AreEqual("Json", metadata.RootClassInfo.BaseClassName);
                Assert.AreEqual("MySampleNamespace", metadata.RootClassInfo.Namespace);

                Assert.AreEqual(2, metadata.CodeBehindClasses.Count);
                var c2 = metadata.CodeBehindClasses.Find((candidate) => {
                    return candidate.ClassName == "InheritedChild";
                });
                Assert.AreEqual("OrderItem", c2.BoundDataClass);
                Assert.AreEqual("MyOtherNs.MySubNS.SubClass", c2.BaseClassName);
                Assert.AreEqual("Apapa_json.Items", c2.RawDebugJsonMapAttribute);

                Assert.AreEqual(3, metadata.UsingDirectives.Count);
                Assert.AreEqual("System", metadata.UsingDirectives[0]);
                Assert.AreEqual("Starcounter", metadata.UsingDirectives[1]);
                Assert.AreEqual("ScAdv=Starcounter.Advanced", metadata.UsingDirectives[2]);
            }

        }

		[Test]
		public static void CodeBehindComplexAnalyzeTest() {
            var monoMetadata = ParserAnalyze("Complex", @"Input\complex.json.cs");
            var roslynMetadata = ParserAnalyze("Complex", @"Input\complex.json.cs", true);

            foreach (var metadata in new[] { monoMetadata, roslynMetadata }) {
                Assert.AreEqual("Order", metadata.RootClassInfo.BoundDataClass);
                Assert.AreEqual("Complex_json", metadata.RootClassInfo.RawDebugJsonMapAttribute);
                Assert.AreEqual("MyBaseJsonClass", metadata.RootClassInfo.BaseClassName);
                Assert.AreEqual("MySampleNamespace", metadata.RootClassInfo.Namespace);

                Assert.AreEqual(6, metadata.CodeBehindClasses.Count);
                var c2 = metadata.CodeBehindClasses.Find((candidate) => {
                    return candidate.ClassName == "SubPage3Impl";
                });

                Assert.AreEqual("OrderItem", c2.BoundDataClass);
                Assert.AreEqual("Json", c2.BaseClassName);
                Assert.AreEqual("Complex_json.ActivePage.SubPage1.SubPage2.SubPage3", c2.RawDebugJsonMapAttribute);

                Assert.AreEqual(3, metadata.UsingDirectives.Count);
                Assert.AreEqual("System", metadata.UsingDirectives[0]);
                Assert.AreEqual("MySampleNamespace.Something", metadata.UsingDirectives[1]);
                Assert.AreEqual("SomeOtherNamespace", metadata.UsingDirectives[2]);
            }
		}

		[Test]
		public static void CodeBehindIncorrectAnalyzeTest() {
            Exception ex;

            bool useRoslynParser = false;

            ex = Assert.Throws<Exception>(() => ParserAnalyze("Incorrect", @"Input\incorrect.json.cs", useRoslynParser));
            Assert.IsTrue(ex.Message.Contains("Generic declaration"));

            ex = Assert.Throws<Exception>(() => ParserAnalyze("Incorrect2", @"Input\incorrect2.json.cs", useRoslynParser));
            Assert.IsTrue(ex.Message.Contains("constructors are not"));

            useRoslynParser = true;
            
            ex = Assert.Throws<Exception>(() => ParserAnalyze("Incorrect", @"Input\incorrect.json.cs", useRoslynParser));
            Assert.IsTrue(ex.Message.Contains("generic class"));

            ex = Assert.Throws<Exception>(() => ParserAnalyze("Incorrect2", @"Input\incorrect2.json.cs", useRoslynParser));
            Assert.IsTrue(ex.Message.Contains("defines at least one constructor"));

            ex = Assert.Throws<Exception>(() => ParserAnalyze("Incorrect3", @"Input\incorrect3.json.cs", useRoslynParser));
            Assert.IsTrue(ex.Message.Contains("not marked partial"));
            
            ex = Assert.Throws<Exception>(() => ParserAnalyze("Incorrect4", @"Input\incorrect4.json.cs", useRoslynParser));
            Assert.IsTrue(ex.Message.Contains("neither a named root nor contains any mapping attribute"));
            Assert.IsTrue(ex.Message.Contains("DoesNotMap"));

            ex = Assert.Throws<Exception>(() => ParserAnalyze("Incorrect5", @"Input\incorrect5.json.cs", useRoslynParser));
            Assert.IsTrue(ex.Message.Contains("is a named root but maps to"));
            Assert.IsTrue(ex.Message.Contains("SomethingElse_json"));

            ex = Assert.Throws<Exception>(() => ParserAnalyze("Incorrect6", @"Input\incorrect6.json.cs", useRoslynParser));
            Assert.IsTrue(ex.Message.Contains("is considered a root class"));
            Assert.IsTrue(ex.Message.Contains("is too"));
        }
    }
}
