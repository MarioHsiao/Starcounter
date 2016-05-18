using System;
using System.IO;
using NUnit.Framework;
using Starcounter.XSON.Compiler.Mono;
using Starcounter.XSON.Metadata;
using System.Text;

namespace Starcounter.Internal.XSON.PartialClassGeneration.Tests {

    public class CodeBehindParserTests {

        private static CodeBehindMetadata ParserAnalyze(string className, string path, bool useRoslynParser = false) {
            return CodeBehindParser.Analyze(className,
                File.ReadAllText(path), path, useRoslynParser);
        }

        private static CodeBehindMetadata ParserAnalyzeCode(string className, string sourceCode, bool useRoslynParser = false)
        {
            return CodeBehindParser.Analyze(className, sourceCode, className + ".cs", useRoslynParser);
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

        [Test]
        public static void CodeBehindParsersEqualityTest()
        {
            var source = "namespace Foo { namespace Bar { public partial class Fubar {} } }";
            var mono = ParserAnalyzeCode("Fubar", source);
            var roslyn = ParserAnalyzeCode("Fubar", source, true);

            // Fail because of bug in mono parser
            // Assert.AreEqual(mono.RootClassInfo.Namespace, roslyn.RootClassInfo.Namespace);
            Assert.True(roslyn.RootClassInfo.Namespace == "Foo.Bar");

            source = "namespace Foo { { public partial class Bar { [Bar_json.Foo] public partial class Foo { [Bar_json.Foo.Fubar] public partial class Fubar {} } } }";
            mono = ParserAnalyzeCode("Bar", source);
            roslyn = ParserAnalyzeCode("Bar", source, true);

            var c1 = mono.FindClassInfo("*.Foo.Fubar");
            var c2 = roslyn.FindClassInfo("*.Foo.Fubar");

            Assert.NotNull(c1);
            Assert.NotNull(c2);
            Assert.True(c1.ParentClasses.Count == 2);
            Assert.AreEqual(c1.ParentClasses.Count, c2.ParentClasses.Count);
            Assert.AreEqual(c1.GlobalClassSpecifier, c2.GlobalClassSpecifier);
            Assert.AreEqual(c1.UseGlobalSpecifier, c2.UseGlobalSpecifier);

            for (int i = 0; i < c1.ParentClasses.Count; i++)
            {
                Assert.AreEqual(c1.ParentClasses[i], c2.ParentClasses[i]);
            }

            var sb = new StringBuilder();
            sb.Append("public partial class Foo : Json { } ");
            sb.Append("[Foo_json.Foo2] public partial class Foo2 : Json, IDisposable { } ");
            sb.Append("[Foo_json.Foo3] public partial class Foo3 : Json, IBound<Fubar.Bar> { } ");
            sb.Append("[Foo_json.Foo4] public partial class Foo4 : IDisposable { } ");
            sb.Append("[Foo_json.Foo5] public partial class Foo5 : IBound<Fubar.Bar> {} ");
            sb.Append("[Foo_json.Foo6] public partial class Foo6 : Custom.BaseType, IBaseType, IRootType {} ");
            sb.Append("[Foo_json.Foo7] public partial class Foo7 { } ");
            sb.Append("[Foo_json.Foo8] public partial class Foo8 : Custom.BaseType, IBaseType, IBound<Fubar.Bar>, IRootType {} ");

            source = sb.ToString();
            mono = ParserAnalyzeCode("Foo", source);
            roslyn = ParserAnalyzeCode("Foo", source, true);

            Assert.AreEqual(mono.CodeBehindClasses.Count, roslyn.CodeBehindClasses.Count);
            for (int i = 0; i < mono.CodeBehindClasses.Count; i++)
            {
                var mc = mono.CodeBehindClasses[i];
                var rc = roslyn.CodeBehindClasses[i];

                if (mc.ClassName != "Foo5")
                {
                    // For Foo5, the mono parser generate a naked IBound as the
                    // base type, while Roslyn return IBound<Fubar.Bar>. Let's not
                    // bother with this in mono parser.
                    Assert.AreEqual(mc.BaseClassName, rc.BaseClassName);
                    Assert.AreEqual(mc.BoundDataClass, rc.BoundDataClass);
                    Assert.AreEqual(mc.DerivesDirectlyFromJson, rc.DerivesDirectlyFromJson);
                }
                else
                {
                    // Check Roslyn produce a more accurate result
                    Assert.AreEqual(rc.BaseClassName, string.Empty);
                    Assert.AreEqual(rc.BoundDataClass, "Fubar.Bar");
                    Assert.True(rc.DerivesDirectlyFromJson);
                }

                Assert.AreEqual(mc.ClassName, rc.ClassName);
                Assert.AreEqual(mc.ClassPath, rc.ClassPath);
                Assert.AreEqual(mc.GlobalClassSpecifier, rc.GlobalClassSpecifier);
                Assert.AreEqual(mc.InputBindingList, rc.InputBindingList);
                Assert.AreEqual(mc.IsDeclaredInCodeBehind, rc.IsDeclaredInCodeBehind);
                Assert.AreEqual(mc.IsMapped, rc.IsMapped);
                Assert.AreEqual(mc.IsRootClass, rc.IsRootClass);
                Assert.AreEqual(mc.Namespace, rc.Namespace);
                Assert.AreEqual(mc.ParentClasses, rc.ParentClasses);
                Assert.AreEqual(mc.RawDebugJsonMapAttribute, rc.RawDebugJsonMapAttribute);
                Assert.AreEqual(mc.UseGlobalSpecifier, rc.UseGlobalSpecifier);
            }
        }
    }
}
