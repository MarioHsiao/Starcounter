using System;
using System.IO;
using NUnit.Framework;
using Starcounter.XSON.Metadata;
using System.Text;
using Starcounter.XSON.PartialClassGenerator;

namespace Starcounter.Internal.XSON.PartialClassGeneration.Tests {

    public class CodeBehindParserTests {

        private static CodeBehindMetadata ParserAnalyze(string className, string path) {
            var parser = new RoslynCodeBehindParser(className, File.ReadAllText(path), path);
            return parser.ParseToMetadata();
        }

        private static CodeBehindMetadata ParserAnalyzeCode(string className, string sourceCode) {
            var parser = new RoslynCodeBehindParser(className, sourceCode, className + ".cs");
            return parser.ParseToMetadata();
        }

        [Test]
        public static void CodeBehindSimpleAnalyzeTest() {
            var metadata = ParserAnalyze("Simple", @"Input\simple.json.cs");

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

        [Test]
        public static void CodeBehindComplexAnalyzeTest() {
            var metadata = ParserAnalyze("Complex", @"Input\complex.json.cs");

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

        [Test]
        public static void CodeBehindIncorrectAnalyzeTest() {
            var ex = Assert.Throws<InvalidCodeBehindException>(() => ParserAnalyze("Incorrect", @"Input\incorrect.json.cs"));
            Assert.IsTrue(ex.Error == InvalidCodeBehindError.ClassGeneric);

            ex = Assert.Throws<InvalidCodeBehindException>(() => ParserAnalyze("Incorrect2", @"Input\incorrect2.json.cs"));
            Assert.IsTrue(ex.Error == InvalidCodeBehindError.DefineInstanceConstructor);

            ex = Assert.Throws<InvalidCodeBehindException>(() => ParserAnalyze("Incorrect3", @"Input\incorrect3.json.cs"));
            Assert.IsTrue(ex.Error == InvalidCodeBehindError.ClassNotPartial);

            ex = Assert.Throws<InvalidCodeBehindException>(() => ParserAnalyze("Incorrect4", @"Input\incorrect4.json.cs"));
            Assert.IsTrue(ex.Error == InvalidCodeBehindError.MultipleMappingAttributes);

            ex = Assert.Throws<InvalidCodeBehindException>(() => ParserAnalyze("Incorrect5", @"Input\incorrect5.json.cs"));
            Assert.IsTrue(ex.Error == InvalidCodeBehindError.RootClassWithCustomMapping);

            ex = Assert.Throws<InvalidCodeBehindException>(() => ParserAnalyze("Incorrect6", @"Input\incorrect6.json.cs"));
            Assert.IsTrue(ex.Error == InvalidCodeBehindError.MultipleRootClasses);

            var source = "public partial class Foo : Json { public static void Handle(Input.Bar bar) {} }";
            ex = Assert.Throws<InvalidCodeBehindException>(() => ParserAnalyzeCode("Foo", source));
            Assert.IsTrue(ex.Error == InvalidCodeBehindError.InputHandlerStatic);

            source = "public partial class Foo : Json { public void Handle(Input.Bar bar, Second illegal) {} }";
            ex = Assert.Throws<InvalidCodeBehindException>(() => ParserAnalyzeCode("Foo", source));
            Assert.IsTrue(ex.Error == InvalidCodeBehindError.InputHandlerBadParameterCount);

            source = "public partial class Foo : Json { public void Handle<T>(Input.Bar bar) {} }";
            ex = Assert.Throws<InvalidCodeBehindException>(() => ParserAnalyzeCode("Foo", source));
            Assert.IsTrue(ex.Error == InvalidCodeBehindError.InputHandlerHasTypeParameters);

            source = "public partial class Foo : Json { public void Handle(ref Input.Bar bar) {} }";
            ex = Assert.Throws<InvalidCodeBehindException>(() => ParserAnalyzeCode("Foo", source));
            Assert.IsTrue(ex.Error == InvalidCodeBehindError.InputHandlerWithRefParameter);

            source = "public partial class Foo : Json { public int Handle(Input.Bar bar) {} }";
            ex = Assert.Throws<InvalidCodeBehindException>(() => ParserAnalyzeCode("Foo", source));
            Assert.IsTrue(ex.Error == InvalidCodeBehindError.InputHandlerNotVoidReturnType);

            source = "public partial class Foo : Json { public abstract void Handle(Input.Bar bar); }";
            ex = Assert.Throws<InvalidCodeBehindException>(() => ParserAnalyzeCode("Foo", source));
            Assert.IsTrue(ex.Error == InvalidCodeBehindError.InputHandlerAbstract);
        }

        [Test]
        public static void CodeBehindParsersEqualityTest() {
            var source = "namespace Foo { namespace Bar { public partial class Fubar {} } }";
            var roslyn = ParserAnalyzeCode("Fubar", source);

            // Fail because of bug in mono parser
            // Assert.AreEqual(mono.RootClassInfo.Namespace, roslyn.RootClassInfo.Namespace);
            Assert.True(roslyn.RootClassInfo.Namespace == "Foo.Bar");

            source = "namespace Foo { { public partial class Bar { [Bar_json.Foo] public partial class Foo { [Bar_json.Foo.Fubar] public partial class Fubar {} } } }";
            roslyn = ParserAnalyzeCode("Bar", source);
            
            var c2 = roslyn.FindClassInfo("*.Foo.Fubar");
            Assert.NotNull(c2);
            
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
            roslyn = ParserAnalyzeCode("Foo", source);
            
            sb.Clear();
            sb.Append("public partial class Foo { ");
            sb.Append("  public void Handle(Input.Bar bar) {}");
            sb.Append("  public void Handle(Input.Bar2 bar) {}");
            sb.Append("}");

            source = sb.ToString();
            roslyn = ParserAnalyzeCode("Foo", source);
            
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
            var ex = Assert.Throws<InvalidCodeBehindException>(() => { var roslyn = ParserAnalyzeCode("Foo", source); });
            Assert.True(ex.Error == InvalidCodeBehindError.ClassNotPartial);
            Assert.True(ex.Line == 0);
            Assert.True(ex.EndLine == 0);

            source = Environment.NewLine; // 0
            source += "using Starcounter" + Environment.NewLine; // 1
            source += "public /*partial*/ class Foo {}"; // 2
            source += Environment.NewLine;

            ex = Assert.Throws<InvalidCodeBehindException>(() => { var roslyn = ParserAnalyzeCode("Foo", source); });
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
            ex = Assert.Throws<InvalidCodeBehindException>(() => { var roslyn = ParserAnalyzeCode("Foo", source); });
            Assert.True(ex.Error == InvalidCodeBehindError.InputHandlerNotVoidReturnType);
            Assert.True(ex.Line == 3);
        }

        [Test]
        public static void CodeBehindBoundtoNullableTest() {
            // Using IBound<T> where T is a nullable type does not get parsed correctly
            // in the old Mono-parser, there it's simply gets truncated.

            var source = "public partial class Foo : Json, IBound<MyStruct?> {}";
            var roslyn = ParserAnalyzeCode("Foo", source);

            Assert.IsNotNull(roslyn.RootClassInfo);
            Assert.AreEqual("MyStruct?", roslyn.RootClassInfo.BoundDataClass);

            source = "public partial class Foo : Json, IBound<Nullable<MyStruct>> {}";
            roslyn = ParserAnalyzeCode("Foo", source);

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
                roslyn = ParserAnalyzeCode("Foo", source);
            });
            Assert.AreEqual((uint)ex.Error, Error.SCERRJSONWITHCONSTRUCTOR);

            // Custom constructor with parameter
            source = "public partial class Foo : Json { public Foo(int bar) { } }";
            ex = Assert.Throws<InvalidCodeBehindException>(() => {
                roslyn = ParserAnalyzeCode("Foo", source);
            });
            Assert.AreEqual((uint)ex.Error, Error.SCERRJSONWITHCONSTRUCTOR);
            
            // Inner unmapped class. Should be ignored.
            source = "public partial class Foo : Json { public class Bar { public Bar(int value) { } } }";
            roslyn = ParserAnalyzeCode("Foo", source);
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

            var roslyn = ParserAnalyzeCode("Foo", source);

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
                var roslyn = ParserAnalyzeCode("Foo", source);
            });
            Assert.AreEqual(InvalidCodeBehindError.TemplateTypeUnsupportedAssignment, ex.Error);

            source = "public partial class Foo: Json {"
                        + "  static Foo() {"
                        + "    DefaultTemplate.RemainingTime.InstanceType = Helper.MethodForGettingType();"
                        + "  }"
                        + "}";

            ex = Assert.Throws<InvalidCodeBehindException>(() => {
                var roslyn = ParserAnalyzeCode("Foo", source);
            });
            Assert.AreEqual(InvalidCodeBehindError.TemplateTypeUnsupportedAssignment, ex.Error);

            source = "public partial class Foo: Json {"
                        + "  static Foo() {"
                        + "    var subPage = DefaultTemplate.Page.SubPage;"
                        + "    subPage.RemainingTime.InstanceType = typeof(double);"
                        + "  }"
                        + "}";

            ex = Assert.Throws<InvalidCodeBehindException>(() => {
                var roslyn = ParserAnalyzeCode("Foo", source);
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
            var roslyn = ParserAnalyzeCode("Foo", source);

            Assert.IsNotNull(roslyn.RootClassInfo);
            Assert.AreEqual(2, roslyn.CodeBehindClasses.Count);

            var cbClass = roslyn.RootClassInfo;
            var typeAssignments = cbClass.InstanceTypeAssignments;
            Assert.AreEqual(5, typeAssignments.Count);
            Assert.AreEqual("DefaultTemplate.ElapsedTime", typeAssignments[0].TemplatePath);
            Assert.AreEqual("double", typeAssignments[0].Value);
            Assert.AreEqual("DefaultTemplate.Page", typeAssignments[1].TemplatePath);
            Assert.AreEqual("MyOtherJson", typeAssignments[1].Value);
            Assert.AreEqual("DefaultTemplate.Page.ChildOfPage", typeAssignments[2].TemplatePath);
            Assert.AreEqual("Int64", typeAssignments[2].Value);
            Assert.AreEqual("DefaultTemplate.Page.SubPage.SuberPage.Value", typeAssignments[3].TemplatePath);
            Assert.AreEqual("decimal", typeAssignments[3].Value);
            Assert.AreEqual("DefaultTemplate.Items.ElementType", typeAssignments[4].TemplatePath);
            Assert.AreEqual("ReusedItemJson", typeAssignments[4].Value);

            cbClass = roslyn.CodeBehindClasses.Find((item) => { return !item.IsRootClass; });
            typeAssignments = cbClass.InstanceTypeAssignments;
            Assert.AreEqual(1, typeAssignments.Count);
            Assert.AreEqual("DefaultTemplate.PartialTime", typeAssignments[0].TemplatePath);
            Assert.AreEqual("double", typeAssignments[0].Value);
        }

        private static void AssertFieldOrPropertyInfo(string name, string typeName, bool isProperty, 
                                                      CodeBehindFieldOrPropertyInfo fop) {
            Assert.AreEqual(name, fop.Name);
            Assert.AreEqual(typeName, fop.TypeName);
            Assert.AreEqual(isProperty, fop.IsProperty);
        }

        [Test]
        public static void CodeBehindDetectIBoundAndIExplicitBound() {
            string source;
            string dataType = "MyDataType";
            string dataType2 = "SubType";
            string dataType3 = "SubType2";

            // IBound<T> : Bound to T, explicit flag should be false.
            source = "public partial class Foo: Json, IBound<" + dataType + "> { }";
            var roslyn = ParserAnalyzeCode("Foo", source);
            AssertBoundToMetadata(roslyn.RootClassInfo, dataType, false);

            // IExplicitBound<T> : Bound to T, explicit flag should be true.
            source = "public partial class Foo: Json, IExplicitBound<" + dataType + "> { }";
            roslyn = ParserAnalyzeCode("Foo", source);
            AssertBoundToMetadata(roslyn.RootClassInfo, dataType, true);

            // Both IBound<T> and IExplicitBound<T> : IExplicitBound<T> should be used.
            source = "public partial class Foo: Json, IBound<" + dataType + ">, IExplicitBound<" + dataType + "> { }";
            roslyn = ParserAnalyzeCode("Foo", source);
            AssertBoundToMetadata(roslyn.RootClassInfo, dataType, true);

            source = "public partial class Foo: Json, IExplicitBound<" + dataType + ">, IBound<" + dataType + "> { }";
            roslyn = ParserAnalyzeCode("Foo", source);
            AssertBoundToMetadata(roslyn.RootClassInfo, dataType, true);

            // No interface added : No datatype set and explicit flag should be false.
            source = "public partial class Foo: Json { }";
            roslyn = ParserAnalyzeCode("Foo", source);
            AssertBoundToMetadata(roslyn.RootClassInfo, null, false);

            // IExplicitBound<T> : Bound to T, explicit flag should be true.
            source = "public partial class Foo: Json, IExplicitBound<" + dataType + "> {"
                   + "   [Foo_json.Page]"
                   + "   partial class FooPage : IExplicitBound< " + dataType2 + "{ }"
                   + "   [Foo_json.Page2]"
                   + "   partial class FooPage2 : IBound< " + dataType3 + "{ }" // Will not be explicitly bound.
                   + "   [Foo_json.Page3]"
                   + "   partial class FooPage3 : Json { }"; // Will not be explicitly bound.

            roslyn = ParserAnalyzeCode("Foo", source);
            AssertBoundToMetadata(roslyn.RootClassInfo, dataType, true);
            AssertBoundToMetadata(roslyn.CodeBehindClasses.Find((classInfo) => classInfo.ClassName == "FooPage"), dataType2, true);
            AssertBoundToMetadata(roslyn.CodeBehindClasses.Find((classInfo) => classInfo.ClassName == "FooPage2"), dataType3, false);
            AssertBoundToMetadata(roslyn.CodeBehindClasses.Find((classInfo) => classInfo.ClassName == "FooPage3"), null, false);
        }

        private static void AssertBoundToMetadata(CodeBehindClassInfo classInfo, string boundToType, bool explicitlyBound) {
            Assert.IsNotNull(classInfo);
            Assert.AreEqual(boundToType, classInfo.BoundDataClass);
            Assert.AreEqual(explicitlyBound, classInfo.ExplicitlyBound);
        }

        [Test]
        public static void CodeBehindUnsupportedTemplateBindAssignment() {
            var source = "public partial class Foo: Json, IExplicitBound<Bar> {"
                        + "  static Foo() {"
                        + "    var t = DefaultTemplate;"
                        + @"    t.RemainingTime.Bind = ""bind"";"
                        + "  }"
                        + "}";

            var ex = Assert.Throws<InvalidCodeBehindException>(() => {
                var roslyn = ParserAnalyzeCode("Foo", source);
            });
            Assert.AreEqual(InvalidCodeBehindError.TemplateBindInvalidAssignment, ex.Error);

            source = "public partial class Foo: Json, IExplicitBound<Bar>{"
                        + "  static Foo() {"
                        + "    var subPage = DefaultTemplate.Page.SubPage;"
                        + @"    subPage.RemainingTime.Bind = ""bind"";"
                        + "  }"
                        + "}";

            ex = Assert.Throws<InvalidCodeBehindException>(() => {
                var roslyn = ParserAnalyzeCode("Foo", source);
            });
            Assert.AreEqual(InvalidCodeBehindError.TemplateBindInvalidAssignment, ex.Error);

            // No errors currently if IExplicitBound<T> is not used.
            source = "public partial class Foo: Json {"
                       + "  static Foo() {"
                       + "    var subPage = DefaultTemplate.Page.SubPage;"
                       + @"    subPage.RemainingTime.Bind = ""bind"";"
                       + "  }"
                       + "}";

            Assert.DoesNotThrow(() => { var roslyn = ParserAnalyzeCode("Foo", source); });
        }

        [Test]
        public static void CodeBehindDetectTemplateBindAssignment() {
            var source = "public partial class Foo: Json, IExplicitBound<Bar> {"
                        + "  static Foo() {"
                        + @"    DefaultTemplate.ElapsedTime.Bind = ""elapsedtime"";"
                        + @"    DefaultTemplate.Page.Bind = null;"
                        + @"    DefaultTemplate.Page.ChildOfPage.Bind = ""childofpage"";"
                        + @"    DefaultTemplate.Page.SubPage.SuberPage.Value.Bind = ""value"";"
                        + @"    DefaultTemplate.Items.ElementType.Bind = ""elementtype"";"
                        + "  }"
                        + "  [Foo_json.Page2]"
                        + "  partial class Page2 : Json, IExplicitBound<Bar> {"
                        + "    static Page2() {"
                        + @"      DefaultTemplate.PartialTime.Bind = ""partialtime"";"
                        + "    }"
                        + "  }"
                        + "}";
            var roslyn = ParserAnalyzeCode("Foo", source);

            Assert.IsNotNull(roslyn.RootClassInfo);
            Assert.AreEqual(2, roslyn.CodeBehindClasses.Count);

            var cbClass = roslyn.RootClassInfo;
            var bindAssignments = cbClass.BindAssignments;
            Assert.AreEqual(5, bindAssignments.Count);
            Assert.AreEqual("DefaultTemplate.ElapsedTime", bindAssignments[0].TemplatePath);
            Assert.AreEqual("elapsedtime", bindAssignments[0].Value);
            Assert.AreEqual("DefaultTemplate.Page", bindAssignments[1].TemplatePath);
            Assert.IsNull(bindAssignments[1].Value);
            Assert.AreEqual("DefaultTemplate.Page.ChildOfPage", bindAssignments[2].TemplatePath);
            Assert.AreEqual("childofpage", bindAssignments[2].Value);
            Assert.AreEqual("DefaultTemplate.Page.SubPage.SuberPage.Value", bindAssignments[3].TemplatePath);
            Assert.AreEqual("value", bindAssignments[3].Value);
            Assert.AreEqual("DefaultTemplate.Items.ElementType", bindAssignments[4].TemplatePath);
            Assert.AreEqual("elementtype", bindAssignments[4].Value);

            cbClass = roslyn.CodeBehindClasses.Find((item) => { return !item.IsRootClass; });
            bindAssignments = cbClass.BindAssignments;
            Assert.AreEqual(1, bindAssignments.Count);
            Assert.AreEqual("DefaultTemplate.PartialTime", bindAssignments[0].TemplatePath);
            Assert.AreEqual("partialtime", bindAssignments[0].Value);
        }

        [Test]
        public static void CodeBehindDetectTemplateBindAssignmentWithOtherSyntax() {
            // When some other assignments is used, we need to treat the template as custom bound.
            // Which means we wont add any code to test the binding compile-time, but it should still
            // be a valid assignment (so no exception can be raised!).

            var source = "public partial class Foo: Json, IExplicitBound<Bar> {"
                        + "  static Foo() {"
                        + "    DefaultTemplate.ElapsedTime.Bind = GetBinding();"
                        + "    DefaultTemplate.Page.Bind = nameof(Bar.Foo)"
                        + @"    DefaultTemplate.Page.ChildOfPage.Bind = nameof(Bar.Foo) + ""."" + nameof(Foo.Bar);"
                        + "  }"
                        + "}";

            CodeBehindMetadata roslyn = null;
            Assert.DoesNotThrow(() => {
                roslyn = ParserAnalyzeCode("Foo", source);
            });

            Assert.IsNotNull(roslyn.RootClassInfo);

            var cbClass = roslyn.RootClassInfo;
            var bindAssignments = cbClass.BindAssignments;
            Assert.AreEqual(3, bindAssignments.Count);
            Assert.AreEqual("DefaultTemplate.ElapsedTime", bindAssignments[0].TemplatePath);
            Assert.IsNull(bindAssignments[0].Value);
            Assert.AreEqual("DefaultTemplate.Page", bindAssignments[1].TemplatePath);
            Assert.IsNull(bindAssignments[1].Value);
            Assert.AreEqual("DefaultTemplate.Page.ChildOfPage", bindAssignments[2].TemplatePath);
            Assert.IsNull(bindAssignments[2].Value);
        }
    }
}
