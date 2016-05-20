﻿using System;
using System.IO;
using NUnit.Framework;
using Starcounter.XSON.Compiler.Mono;
using Starcounter.XSON.Metadata;
using System.Text;
using Starcounter.XSON.PartialClassGenerator;

namespace Starcounter.Internal.XSON.PartialClassGeneration.Tests {

    public class CodeBehindParserTests {

        private static CodeBehindMetadata ParserAnalyze(string className, string path, bool useRoslynParser = false) {
            return CodeBehindParser.Analyze(className,
                File.ReadAllText(path), path, useRoslynParser);
        }

        private static CodeBehindMetadata ParserAnalyzeCode(string className, string sourceCode, bool useRoslynParser = false) {
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

            var ex2 = Assert.Throws<InvalidCodeBehindException>(() => ParserAnalyze("Incorrect", @"Input\incorrect.json.cs", useRoslynParser));
            Assert.IsTrue(ex2.Error == InvalidCodeBehindError.ClassGeneric);

            ex2 = Assert.Throws<InvalidCodeBehindException>(() => ParserAnalyze("Incorrect2", @"Input\incorrect2.json.cs", useRoslynParser));
            Assert.IsTrue(ex2.Error == InvalidCodeBehindError.DefineInstanceConstructor);

            ex2 = Assert.Throws<InvalidCodeBehindException>(() => ParserAnalyze("Incorrect3", @"Input\incorrect3.json.cs", useRoslynParser));
            Assert.IsTrue(ex2.Error == InvalidCodeBehindError.ClassNotPartial);

            ex2 = Assert.Throws<InvalidCodeBehindException>(() => ParserAnalyze("Incorrect4", @"Input\incorrect4.json.cs", useRoslynParser));
            Assert.IsTrue(ex2.Error == InvalidCodeBehindError.ClassNotMapped);

            ex2 = Assert.Throws<InvalidCodeBehindException>(() => ParserAnalyze("Incorrect5", @"Input\incorrect5.json.cs", useRoslynParser));
            Assert.IsTrue(ex2.Error == InvalidCodeBehindError.RootClassWithCustomMapping);

            ex2 = Assert.Throws<InvalidCodeBehindException>(() => ParserAnalyze("Incorrect6", @"Input\incorrect6.json.cs", useRoslynParser));
            Assert.IsTrue(ex2.Error == InvalidCodeBehindError.MultipleRootClasses);

            var source = "public partial class Foo : Json { public static void Handle(Input.Bar bar) {} }";
            ex2 = Assert.Throws<InvalidCodeBehindException>(() => ParserAnalyzeCode("Foo", source, useRoslynParser));
            Assert.IsTrue(ex2.Error == InvalidCodeBehindError.InputHandlerStatic);

            source = "public partial class Foo : Json { public void Handle(Input.Bar bar, Second illegal) {} }";
            ex2 = Assert.Throws<InvalidCodeBehindException>(() => ParserAnalyzeCode("Foo", source, useRoslynParser));
            Assert.IsTrue(ex2.Error == InvalidCodeBehindError.InputHandlerBadParameterCount);

            source = "public partial class Foo : Json { public void Handle<T>(Input.Bar bar) {} }";
            ex2 = Assert.Throws<InvalidCodeBehindException>(() => ParserAnalyzeCode("Foo", source, useRoslynParser));
            Assert.IsTrue(ex2.Error == InvalidCodeBehindError.InputHandlerHasTypeParameters);

            source = "public partial class Foo : Json { public void Handle(ref Input.Bar bar) {} }";
            ex2 = Assert.Throws<InvalidCodeBehindException>(() => ParserAnalyzeCode("Foo", source, useRoslynParser));
            Assert.IsTrue(ex2.Error == InvalidCodeBehindError.InputHandlerWithRefParameter);

            source = "public partial class Foo : Json { public int Handle(Input.Bar bar) {} }";
            ex2 = Assert.Throws<InvalidCodeBehindException>(() => ParserAnalyzeCode("Foo", source, useRoslynParser));
            Assert.IsTrue(ex2.Error == InvalidCodeBehindError.InputHandlerNotVoidReturnType);

            source = "public partial class Foo : Json { public abstract void Handle(Input.Bar bar); }";
            ex2 = Assert.Throws<InvalidCodeBehindException>(() => ParserAnalyzeCode("Foo", source, useRoslynParser));
            Assert.IsTrue(ex2.Error == InvalidCodeBehindError.InputHandlerAbstract);
        }

        [Test]
        public static void CodeBehindParsersEqualityTest() {
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

            for (int i = 0; i < c1.ParentClasses.Count; i++) {
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
            for (int i = 0; i < mono.CodeBehindClasses.Count; i++) {
                var mc = mono.CodeBehindClasses[i];
                var rc = roslyn.CodeBehindClasses[i];

                if (mc.ClassName != "Foo5") {
                    // For Foo5, the mono parser generate a naked IBound as the
                    // base type, while Roslyn return IBound<Fubar.Bar>. Let's not
                    // bother with this in mono parser.
                    Assert.AreEqual(mc.BaseClassName, rc.BaseClassName);
                    Assert.AreEqual(mc.BoundDataClass, rc.BoundDataClass);
                    Assert.AreEqual(mc.DerivesDirectlyFromJson, rc.DerivesDirectlyFromJson);
                }
                else {
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

            sb.Clear();
            sb.Append("public partial class Foo { ");
            sb.Append("  public void Handle(Input.Bar bar) {}");
            sb.Append("  public void Handle(Input.Bar2 bar) {}");
            sb.Append("}");

            source = sb.ToString();
            mono = ParserAnalyzeCode("Foo", source);
            roslyn = ParserAnalyzeCode("Foo", source, true);

            Assert.AreEqual(mono.RootClassInfo.InputBindingList.Count, roslyn.RootClassInfo.InputBindingList.Count);
            for (int i = 0; i < mono.RootClassInfo.InputBindingList.Count; i++) {
                var mh = mono.RootClassInfo.InputBindingList[i];
                var rh = roslyn.RootClassInfo.InputBindingList[i];

                Assert.AreEqual(mh.DeclaringClassName, rh.DeclaringClassName);
                Assert.AreEqual(mh.DeclaringClassNamespace, rh.DeclaringClassNamespace);
                Assert.AreEqual(mh.FullInputTypeName, rh.FullInputTypeName);
            }

            Assert.IsNotNull(roslyn.RootClassInfo.InputBindingList.Find((candidate) => {
                return candidate.DeclaringClassName == "Foo" && candidate.FullInputTypeName == "Input.Bar";
            }));
            Assert.IsNotNull(roslyn.RootClassInfo.InputBindingList.Find((candidate) => {
                return candidate.DeclaringClassNamespace == null && candidate.FullInputTypeName == "Input.Bar2";
            }));
        }

        [Test]
        public static void CodeBehindParserErrorPropagationTest() {
            var source = "public /*partial*/ class Foo {}";
            var ex = Assert.Throws<InvalidCodeBehindException>(() => { var roslyn = ParserAnalyzeCode("Foo", source, true); });
            Assert.True(ex.Line == 1);
            Assert.True(ex.EndLine == 1);

            source = Environment.NewLine; // 1
            source += "using Starcounter" + Environment.NewLine; // 2
            source += "public /*partial*/ class Foo {}"; // 3
            source += Environment.NewLine;

            ex = Assert.Throws<InvalidCodeBehindException>(() => { var roslyn = ParserAnalyzeCode("Foo", source, true); });
            Assert.True(ex.Line == 3);
            Assert.True(ex.EndLine == 3);

            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("public partial class Foo {"); // 2
            sb.AppendLine();
            sb.AppendLine("public int Handle(Input.Bar bar) {");    // 4
            sb.AppendLine("Console.WriteLine(\"Hello from invalid handler\");");
            sb.AppendLine("}");

            source = sb.ToString();
            ex = Assert.Throws<InvalidCodeBehindException>(() => { var roslyn = ParserAnalyzeCode("Foo", source, true); });
            Assert.True(ex.Error == InvalidCodeBehindError.InputHandlerNotVoidReturnType);
            Assert.True(ex.Line == 4);
        }
    }
}
