using System;
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
                Assert.AreEqual("Simple_json", metadata.RootClassInfo.JsonMapAttribute);
                Assert.AreEqual("Json", metadata.RootClassInfo.BaseClassName);
                Assert.AreEqual("MySampleNamespace", metadata.RootClassInfo.Namespace);

                Assert.AreEqual(2, metadata.CodeBehindClasses.Count);
                var c2 = metadata.CodeBehindClasses.Find((candidate) => {
                    return candidate.ClassName == "InheritedChild";
                });
                Assert.AreEqual("OrderItem", c2.BoundDataClass);
                Assert.AreEqual("MyOtherNs.MySubNS.SubClass", c2.BaseClassName);
                Assert.AreEqual("Apapa_json.Items", c2.JsonMapAttribute);

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
                Assert.AreEqual("Complex_json", metadata.RootClassInfo.JsonMapAttribute);
                Assert.AreEqual("MyBaseJsonClass", metadata.RootClassInfo.BaseClassName);
                Assert.AreEqual("MySampleNamespace", metadata.RootClassInfo.Namespace);

                Assert.AreEqual(6, metadata.CodeBehindClasses.Count);
                var c2 = metadata.CodeBehindClasses.Find((candidate) => {
                    return candidate.ClassName == "SubPage3Impl";
                });

                Assert.AreEqual("OrderItem", c2.BoundDataClass);
                Assert.AreEqual("Json", c2.BaseClassName);
                Assert.AreEqual("Complex_json.ActivePage.SubPage1.SubPage2.SubPage3", c2.JsonMapAttribute);

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
            Assert.IsTrue(ex2.Error == InvalidCodeBehindError.MultipleMappingAttributes);

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
                Assert.AreEqual(mc.JsonMapAttribute, rc.JsonMapAttribute);
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
            Assert.True(ex.Error == InvalidCodeBehindError.ClassNotPartial);
            Assert.True(ex.Line == 0);
            Assert.True(ex.EndLine == 0);

            source = Environment.NewLine; // 0
            source += "using Starcounter" + Environment.NewLine; // 1
            source += "public /*partial*/ class Foo {}"; // 2
            source += Environment.NewLine;

            ex = Assert.Throws<InvalidCodeBehindException>(() => { var roslyn = ParserAnalyzeCode("Foo", source, true); });
            Assert.True(ex.Error == InvalidCodeBehindError.ClassNotPartial);
            Assert.True(ex.Line == 2);
            Assert.True(ex.EndLine == 2);

            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("public partial class Foo {"); // 1
            sb.AppendLine();
            sb.AppendLine("public int Handle(Input.Bar bar) {");    // 3
            sb.AppendLine("Console.WriteLine(\"Hello from invalid handler\");");
            sb.AppendLine("}");

            source = sb.ToString();
            ex = Assert.Throws<InvalidCodeBehindException>(() => { var roslyn = ParserAnalyzeCode("Foo", source, true); });
            Assert.True(ex.Error == InvalidCodeBehindError.InputHandlerNotVoidReturnType);
            Assert.True(ex.Line == 3);
        }

        [Test]
        public static void CodeBehindBoundtoNullableTest() {
            // Using IBound<T> where T is a nullable type does not get parsed correctly
            // in the old Mono-parser, there it's simply gets truncated.

            var source = "public partial class Foo : Json, IBound<MyStruct?> {}";
            var roslyn = ParserAnalyzeCode("Foo", source, true);

            Assert.IsNotNull(roslyn.RootClassInfo);
            Assert.AreEqual("MyStruct?", roslyn.RootClassInfo.BoundDataClass);

            source = "public partial class Foo : Json, IBound<Nullable<MyStruct>> {}";
            roslyn = ParserAnalyzeCode("Foo", source, true);

            Assert.IsNotNull(roslyn.RootClassInfo);
            Assert.AreEqual("Nullable<MyStruct>", roslyn.RootClassInfo.BoundDataClass);
        }

        [Test]
        public static void CodeBehindCustomConstructorTest() {
            InvalidCodeBehindException ex;
            CodeBehindMetadata roslyn = null;
            string source;

            // Custom default constructor.
            // TODO: Should this be allowed?
            source = "public partial class Foo : Json { public Foo() { } }";
            ex = Assert.Throws<InvalidCodeBehindException>(() => {
                roslyn = ParserAnalyzeCode("Foo", source, true);
            });
            Assert.AreEqual((uint)ex.Error, Error.SCERRJSONWITHCONSTRUCTOR);

            // Custom constructor with parameter
            source = "public partial class Foo : Json { public Foo(int bar) { } }";
            ex = Assert.Throws<InvalidCodeBehindException>(() => {
                roslyn = ParserAnalyzeCode("Foo", source, true);
            });
            Assert.AreEqual((uint)ex.Error, Error.SCERRJSONWITHCONSTRUCTOR);
            
            // Inner unmapped class. Should be ignored.
            source = "public partial class Foo : Json { public class Bar { public Bar(int value) { } } }";
            roslyn = ParserAnalyzeCode("Foo", source, true);
        }

        [Test]
        public static void CodeBehindDetectPropertiesAndFieldsTest() {
            string source;
            
            source = "public partial class Foo: Json {"
                    + "private string one; "
                    + "private int two, thrEE; "
                    + "static Int32 Four; "
                    + "protected long Five { get; set; } "
                    + "string Six { get; private set; } "
                    + "public static long seven { get; set; } "
                    + "}";

            var roslyn = ParserAnalyzeCode("Foo", source, true);

            Assert.IsNotNull(roslyn.RootClassInfo);
            Assert.AreEqual(7, roslyn.RootClassInfo.FieldOrPropertyList.Count);
            
            AssertFieldOrPropertyInfo("one", "string", false, roslyn.RootClassInfo.FieldOrPropertyList[0]);
            AssertFieldOrPropertyInfo("two", "int", false, roslyn.RootClassInfo.FieldOrPropertyList[1]);
            AssertFieldOrPropertyInfo("thrEE", "int", false, roslyn.RootClassInfo.FieldOrPropertyList[2]);
            AssertFieldOrPropertyInfo("Four", "Int32", false, roslyn.RootClassInfo.FieldOrPropertyList[3]);
            AssertFieldOrPropertyInfo("Five", "long", true, roslyn.RootClassInfo.FieldOrPropertyList[4]);
            AssertFieldOrPropertyInfo("Six", "string", true, roslyn.RootClassInfo.FieldOrPropertyList[5]);
            AssertFieldOrPropertyInfo("seven", "long", true, roslyn.RootClassInfo.FieldOrPropertyList[6]);
        }

        [Test]
        public static void CodeBehindUnsupportedTemplateInstanceTypeAssignment() {
            var source = "public partial class Foo: Json {"
                        + "  static Foo() {"
                        + "    Type t = typeof(double)"
                        + "    DefaultTemplate.RemainingTime.InstanceType = t;"
                        + "  }"
                        + "}";

            var ex = Assert.Throws<InvalidCodeBehindException>(() => {
                var roslyn = ParserAnalyzeCode("Foo", source, true);
            });
            Assert.AreEqual(InvalidCodeBehindError.TemplateTypeUnsupportedAssignment, ex.Error);

            source = "public partial class Foo: Json {"
                        + "  static Foo() {"
                        + "    DefaultTemplate.RemainingTime.InstanceType = Helper.MethodForGettingType();"
                        + "  }"
                        + "}";

            ex = Assert.Throws<InvalidCodeBehindException>(() => {
                var roslyn = ParserAnalyzeCode("Foo", source, true);
            });
            Assert.AreEqual(InvalidCodeBehindError.TemplateTypeUnsupportedAssignment, ex.Error);

            source = "public partial class Foo: Json {"
                        + "  static Foo() {"
                        + "    var subPage = DefaultTemplate.Page.SubPage;"
                        + "    subPage.RemainingTime.InstanceType = typeof(double);"
                        + "  }"
                        + "}";

            ex = Assert.Throws<InvalidCodeBehindException>(() => {
                var roslyn = ParserAnalyzeCode("Foo", source, true);
            });
            Assert.AreEqual(InvalidCodeBehindError.TemplateTypeUnsupportedAssignment, ex.Error);
        }
        
        [Test]
        public static void CodeBehindDetectTemplateInstanceTypeAssignment() {
            var source = "public partial class Foo: Json {"
                        + "  static Foo() {"
                        + "    DefaultTemplate.ElapsedTime.InstanceType = typeof(double);"
                        + "    DefaultTemplate.Page.InstanceType = typeof(MyOtherJson);"
                        + "    DefaultTemplate.Page.ChildOfPage.InstanceType = typeof(Int64);"
                        + "    DefaultTemplate.Page.SubPage.SuberPage.Value.InstanceType = typeof(decimal);"
                        + "    DefaultTemplate.Items.ElementType.InstanceType = typeof(ReusedItemJson);"
                        + "  }"
                        + "  [Foo_json.Page2]"
                        + "  partial class Page2 : Json {"
                        + "    static Page2() {"
                        + "      DefaultTemplate.PartialTime.InstanceType = typeof(double);"
                        + "    }"
                        + "  }"
                        + "}";
            var roslyn = ParserAnalyzeCode("Foo", source, true);

            Assert.IsNotNull(roslyn.RootClassInfo);
            Assert.AreEqual(2, roslyn.CodeBehindClasses.Count);

            var cbClass = roslyn.RootClassInfo;
            var typeAssignments = cbClass.InstanceTypeAssignments;
            Assert.AreEqual(5, typeAssignments.Count);
            Assert.AreEqual("DefaultTemplate.ElapsedTime", typeAssignments[0].TemplatePath);
            Assert.AreEqual("double", typeAssignments[0].TypeName);
            Assert.AreEqual("DefaultTemplate.Page", typeAssignments[1].TemplatePath);
            Assert.AreEqual("MyOtherJson", typeAssignments[1].TypeName);
            Assert.AreEqual("DefaultTemplate.Page.ChildOfPage", typeAssignments[2].TemplatePath);
            Assert.AreEqual("Int64", typeAssignments[2].TypeName);
            Assert.AreEqual("DefaultTemplate.Page.SubPage.SuberPage.Value", typeAssignments[3].TemplatePath);
            Assert.AreEqual("decimal", typeAssignments[3].TypeName);
            Assert.AreEqual("DefaultTemplate.Items.ElementType", typeAssignments[4].TemplatePath);
            Assert.AreEqual("ReusedItemJson", typeAssignments[4].TypeName);

            cbClass = roslyn.CodeBehindClasses.Find((item) => { return !item.IsRootClass; });
            typeAssignments = cbClass.InstanceTypeAssignments;
            Assert.AreEqual(1, typeAssignments.Count);
            Assert.AreEqual("DefaultTemplate.PartialTime", typeAssignments[0].TemplatePath);
            Assert.AreEqual("double", typeAssignments[0].TypeName);
        }

        private static void AssertFieldOrPropertyInfo(string name, string typeName, bool isProperty, 
                                                      CodeBehindFieldOrPropertyInfo fop) {
            Assert.AreEqual(name, fop.Name);
            Assert.AreEqual(typeName, fop.TypeName);
            Assert.AreEqual(isProperty, fop.IsProperty);
        }
    }
}
