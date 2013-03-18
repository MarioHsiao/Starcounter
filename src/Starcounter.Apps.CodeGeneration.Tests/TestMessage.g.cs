// This is a system generated file. It reflects the Starcounter App Template defined in the file "unknown"
// DO NOT MODIFY DIRECTLY - CHANGES WILL BE OVERWRITTEN

using System;
using System.Collections;
using System.Collections.Generic;
using Starcounter.Advanced;
using Starcounter;
using Starcounter.Internal;
using Starcounter.Internal.JsonPatch;
using Starcounter.Templates;

namespace MySampleNamespace {
public partial class TestMessage {
    public static TTestMessage DefaultTemplate = new TTestMessage();
    public TestMessage() { Template = DefaultTemplate; }
    public TestMessage(TTestMessage template) { Template = template; }
    public new TTestMessage Template { get { return (TTestMessage)base.Template; } set { base.Template = value; } }
    public new TestMessageMetadata Metadata { get { return (TestMessageMetadata)base.Metadata; } }
    public long UserId { get { return Get(Template.UserId); } set { Set(Template.UserId, value); } }
    public String Username { get { return Get(Template.Username); } set { Set(Template.Username, value); } }
    public String Password { get { return Get(Template.Password); } set { Set(Template.Password, value); } }
    public TestMessage.ChildApp Child { get { return Get<TestMessage.ChildApp>(Template.Child); } set { Set(Template.Child, value); } }
    public Arr<TestMessage.AListApp> AList { get { return Get<TestMessage.AListApp>(Template.AList); } set { Set<TestMessage.AListApp>(Template.AList, value); } }
    public Decimal ADecimal { get { return Get(Template.ADecimal); } set { Set(Template.ADecimal, value); } }
    public Double ADouble { get { return Get(Template.ADouble); } set { Set(Template.ADouble, value); } }
    public String UserLink { get { return Get(Template.UserLink); } set { Set(Template.UserLink, value); } }
    public class ChildApp : Json {
        public static TChildApp DefaultTemplate = new TChildApp();
        public ChildApp() { Template = DefaultTemplate; }
        public ChildApp(TChildApp template) { Template = template; }
        public new TChildApp Template { get { return (TChildApp)base.Template; } set { base.Template = value; } }
        public new ChildAppMetadata Metadata { get { return (ChildAppMetadata)base.Metadata; } }
        public new TestMessage Parent { get { return (TestMessage)base.Parent; } set { base.Parent = value; } }
        public String ChildName { get { return Get(Template.ChildName); } set { Set(Template.ChildName, value); } }
        public TestMessage.ChildApp.ASubAppApp ASubApp { get { return Get<TestMessage.ChildApp.ASubAppApp>(Template.ASubApp); } set { Set(Template.ASubApp, value); } }
        public TestMessage.ChildApp.ASubApp2App ASubApp2 { get { return Get<TestMessage.ChildApp.ASubApp2App>(Template.ASubApp2); } set { Set(Template.ASubApp2, value); } }
        public class ASubAppApp : Json {
            public static TASubAppApp DefaultTemplate = new TASubAppApp();
            public ASubAppApp() { Template = DefaultTemplate; }
            public ASubAppApp(TASubAppApp template) { Template = template; }
            public new TASubAppApp Template { get { return (TASubAppApp)base.Template; } set { base.Template = value; } }
            public new ASubAppAppMetadata Metadata { get { return (ASubAppAppMetadata)base.Metadata; } }
            public new ChildApp Parent { get { return (ChildApp)base.Parent; } set { base.Parent = value; } }
            public bool IsInnerApp { get { return Get(Template.IsInnerApp); } set { Set(Template.IsInnerApp, value); } }
            public class TASubAppApp : TJson {
                public TASubAppApp()
                    : base() {
                    InstanceType = typeof(TestMessage.ChildApp.ASubAppApp);
                    ClassName = "ASubAppApp";
                    IsInnerApp = Add<TBool>("IsInnerApp");
                }
                public override object CreateInstance(Container parent) { return new ASubAppApp(this) { Parent = (TestMessage.ChildApp)parent }; }
                public TBool IsInnerApp;
            }
            public class ASubAppAppMetadata : ObjMetadata {
                public ASubAppAppMetadata(Json obj, TJson template) : base(obj, template) { }
                public new TestMessage.ChildApp.ASubAppApp App { get { return (TestMessage.ChildApp.ASubAppApp)base.App; } }
                public new TestMessage.ChildApp.ASubAppApp.TASubAppApp Template { get { return (TestMessage.ChildApp.ASubAppApp.TASubAppApp)base.Template; } }
                public BoolMetadata IsInnerApp { get { return __p_IsInnerApp ?? (__p_IsInnerApp = new BoolMetadata(App, App.Template.IsInnerApp)); } }
                private BoolMetadata __p_IsInnerApp;
            }
        }
        public class ASubApp2App : Json {
            public static TASubApp2App DefaultTemplate = new TASubApp2App();
            public ASubApp2App() { Template = DefaultTemplate; }
            public ASubApp2App(TASubApp2App template) { Template = template; }
            public new TASubApp2App Template { get { return (TASubApp2App)base.Template; } set { base.Template = value; } }
            public new ASubApp2AppMetadata Metadata { get { return (ASubApp2AppMetadata)base.Metadata; } }
            public new ChildApp Parent { get { return (ChildApp)base.Parent; } set { base.Parent = value; } }
            public bool IsInnerApp { get { return Get(Template.IsInnerApp); } set { Set(Template.IsInnerApp, value); } }
            public Arr<TestMessage.ChildApp.ASubApp2App.ASubListApp> ASubList { get { return Get<TestMessage.ChildApp.ASubApp2App.ASubListApp>(Template.ASubList); } set { Set<TestMessage.ChildApp.ASubApp2App.ASubListApp>(Template.ASubList, value); } }
            public class ASubListApp : Json {
                public static TASubListApp DefaultTemplate = new TASubListApp();
                public ASubListApp() { Template = DefaultTemplate; }
                public ASubListApp(TASubListApp template) { Template = template; }
                public new TASubListApp Template { get { return (TASubListApp)base.Template; } set { base.Template = value; } }
                public new ASubListAppMetadata Metadata { get { return (ASubListAppMetadata)base.Metadata; } }
                public new Arr<TestMessage.ChildApp.ASubApp2App.ASubListApp> Parent { get { return (Arr<TestMessage.ChildApp.ASubApp2App.ASubListApp>)base.Parent; } set { base.Parent = value; } }
                public String Huh { get { return Get(Template.Huh); } set { Set(Template.Huh, value); } }
                public class TASubListApp : TJson {
                    public TASubListApp()
                        : base() {
                        InstanceType = typeof(TestMessage.ChildApp.ASubApp2App.ASubListApp);
                        ClassName = "ASubListApp";
                        Huh = Add<TString>("Huh");
                    }
                    public override object CreateInstance(Container parent) { return new ASubListApp(this) { Parent = (Arr<TestMessage.ChildApp.ASubApp2App.ASubListApp>)parent }; }
                    public TString Huh;
                }
                public class ASubListAppMetadata : ObjMetadata {
                    public ASubListAppMetadata(Json obj, TJson template) : base(obj, template) { }
                    public new TestMessage.ChildApp.ASubApp2App.ASubListApp App { get { return (TestMessage.ChildApp.ASubApp2App.ASubListApp)base.App; } }
                    public new TestMessage.ChildApp.ASubApp2App.ASubListApp.TASubListApp Template { get { return (TestMessage.ChildApp.ASubApp2App.ASubListApp.TASubListApp)base.Template; } }
                    public StringMetadata Huh { get { return __p_Huh ?? (__p_Huh = new StringMetadata(App, App.Template.Huh)); } }
                    private StringMetadata __p_Huh;
                }
            }
            public class TASubApp2App : TJson {
                public TASubApp2App()
                    : base() {
                    InstanceType = typeof(TestMessage.ChildApp.ASubApp2App);
                    ClassName = "ASubApp2App";
                    IsInnerApp = Add<TBool>("IsInnerApp");
                    ASubList = Add<TArr<TestMessage.ChildApp.ASubApp2App.ASubListApp, TestMessage.ChildApp.ASubApp2App.ASubListApp.TASubListApp>>("ASubList");
                    ASubList.App = TestMessage.ChildApp.ASubApp2App.ASubListApp.DefaultTemplate;
                }
                public override object CreateInstance(Container parent) { return new ASubApp2App(this) { Parent = (TestMessage.ChildApp)parent }; }
                public TBool IsInnerApp;
                public TArr<TestMessage.ChildApp.ASubApp2App.ASubListApp, TestMessage.ChildApp.ASubApp2App.ASubListApp.TASubListApp> ASubList;
            }
            public class ASubApp2AppMetadata : ObjMetadata {
                public ASubApp2AppMetadata(Json obj, TJson template) : base(obj, template) { }
                public new TestMessage.ChildApp.ASubApp2App App { get { return (TestMessage.ChildApp.ASubApp2App)base.App; } }
                public new TestMessage.ChildApp.ASubApp2App.TASubApp2App Template { get { return (TestMessage.ChildApp.ASubApp2App.TASubApp2App)base.Template; } }
                public BoolMetadata IsInnerApp { get { return __p_IsInnerApp ?? (__p_IsInnerApp = new BoolMetadata(App, App.Template.IsInnerApp)); } }
                private BoolMetadata __p_IsInnerApp;
                public ArrMetadata<TestMessage.ChildApp.ASubApp2App.ASubListApp, TestMessage.ChildApp.ASubApp2App.ASubListApp.TASubListApp> ASubList { get { return __p_ASubList ?? (__p_ASubList = new ArrMetadata<TestMessage.ChildApp.ASubApp2App.ASubListApp, TestMessage.ChildApp.ASubApp2App.ASubListApp.TASubListApp>(App, App.Template.ASubList)); } }
                private ArrMetadata<TestMessage.ChildApp.ASubApp2App.ASubListApp, TestMessage.ChildApp.ASubApp2App.ASubListApp.TASubListApp> __p_ASubList;
            }
        }
        public class TChildApp : TJson {
            public TChildApp()
                : base() {
                InstanceType = typeof(TestMessage.ChildApp);
                ClassName = "ChildApp";
                ChildName = Add<TString>("ChildName");
                ASubApp = Add<TestMessage.ChildApp.ASubAppApp.TASubAppApp>("ASubApp");
                ASubApp2 = Add<TestMessage.ChildApp.ASubApp2App.TASubApp2App>("ASubApp2");
            }
            public override object CreateInstance(Container parent) { return new ChildApp(this) { Parent = (TestMessage)parent }; }
            public TString ChildName;
            public TestMessage.ChildApp.ASubAppApp.TASubAppApp ASubApp;
            public TestMessage.ChildApp.ASubApp2App.TASubApp2App ASubApp2;
        }
        public class ChildAppMetadata : ObjMetadata {
            public ChildAppMetadata(Json obj, TJson template) : base(obj, template) { }
            public new TestMessage.ChildApp App { get { return (TestMessage.ChildApp)base.App; } }
            public new TestMessage.ChildApp.TChildApp Template { get { return (TestMessage.ChildApp.TChildApp)base.Template; } }
            public StringMetadata ChildName { get { return __p_ChildName ?? (__p_ChildName = new StringMetadata(App, App.Template.ChildName)); } }
            private StringMetadata __p_ChildName;
            public TestMessage.ChildApp.ASubAppApp.ASubAppAppMetadata ASubApp { get { return __p_ASubApp ?? (__p_ASubApp = new TestMessage.ChildApp.ASubAppApp.ASubAppAppMetadata(App, App.Template.ASubApp)); } }
            private TestMessage.ChildApp.ASubAppApp.ASubAppAppMetadata __p_ASubApp;
            public TestMessage.ChildApp.ASubApp2App.ASubApp2AppMetadata ASubApp2 { get { return __p_ASubApp2 ?? (__p_ASubApp2 = new TestMessage.ChildApp.ASubApp2App.ASubApp2AppMetadata(App, App.Template.ASubApp2)); } }
            private TestMessage.ChildApp.ASubApp2App.ASubApp2AppMetadata __p_ASubApp2;
        }
    }
    public class AListApp : Json {
        public static TAListApp DefaultTemplate = new TAListApp();
        public AListApp() { Template = DefaultTemplate; }
        public AListApp(TAListApp template) { Template = template; }
        public new TAListApp Template { get { return (TAListApp)base.Template; } set { base.Template = value; } }
        public new AListAppMetadata Metadata { get { return (AListAppMetadata)base.Metadata; } }
        public new Arr<TestMessage.AListApp> Parent { get { return (Arr<TestMessage.AListApp>)base.Parent; } set { base.Parent = value; } }
        public String AValue { get { return Get(Template.AValue); } set { Set(Template.AValue, value); } }
        public long ANumber { get { return Get(Template.ANumber); } set { Set(Template.ANumber, value); } }
        public class TAListApp : TJson {
            public TAListApp()
                : base() {
                InstanceType = typeof(TestMessage.AListApp);
                ClassName = "AListApp";
                AValue = Add<TString>("AValue");
                ANumber = Add<TLong>("ANumber");
            }
            public override object CreateInstance(Container parent) { return new AListApp(this) { Parent = (Arr<TestMessage.AListApp>)parent }; }
            public TString AValue;
            public TLong ANumber;
        }
        public class AListAppMetadata : ObjMetadata {
            public AListAppMetadata(Json obj, TJson template) : base(obj, template) { }
            public new TestMessage.AListApp App { get { return (TestMessage.AListApp)base.App; } }
            public new TestMessage.AListApp.TAListApp Template { get { return (TestMessage.AListApp.TAListApp)base.Template; } }
            public StringMetadata AValue { get { return __p_AValue ?? (__p_AValue = new StringMetadata(App, App.Template.AValue)); } }
            private StringMetadata __p_AValue;
            public LongMetadata ANumber { get { return __p_ANumber ?? (__p_ANumber = new LongMetadata(App, App.Template.ANumber)); } }
            private LongMetadata __p_ANumber;
        }
    }
    public class TTestMessage : TJson {
        public TTestMessage()
            : base() {
            InstanceType = typeof(TestMessage);
            ClassName = "TestMessage";
            UserId = Add<TLong>("UserId$");
            UserId.Editable = true;
            Username = Add<TString>("Username");
            Password = Add<TString>("Password");
            Child = Add<TestMessage.ChildApp.TChildApp>("Child");
            AList = Add<TArr<TestMessage.AListApp, TestMessage.AListApp.TAListApp>>("AList");
            AList.App = TestMessage.AListApp.DefaultTemplate;
            ADecimal = Add<TDecimal>("ADecimal");
            ADouble = Add<TDouble>("ADouble");
            UserLink = Add<TString>("UserLink");
        }
        public override object CreateInstance(Container parent) { return new TestMessage(this) { Parent = parent }; }
        public TLong UserId;
        public TString Username;
        public TString Password;
        public TestMessage.ChildApp.TChildApp Child;
        public TArr<TestMessage.AListApp, TestMessage.AListApp.TAListApp> AList;
        public TDecimal ADecimal;
        public TDouble ADouble;
        public TString UserLink;
    }
    public class TestMessageMetadata : ObjMetadata {
        public TestMessageMetadata(Json obj, TJson template) : base(obj, template) { }
        public new TestMessage App { get { return (TestMessage)base.App; } }
        public new TestMessage.TTestMessage Template { get { return (TestMessage.TTestMessage)base.Template; } }
        public LongMetadata UserId { get { return __p_UserId ?? (__p_UserId = new LongMetadata(App, App.Template.UserId)); } }
        private LongMetadata __p_UserId;
        public StringMetadata Username { get { return __p_Username ?? (__p_Username = new StringMetadata(App, App.Template.Username)); } }
        private StringMetadata __p_Username;
        public StringMetadata Password { get { return __p_Password ?? (__p_Password = new StringMetadata(App, App.Template.Password)); } }
        private StringMetadata __p_Password;
        public TestMessage.ChildApp.ChildAppMetadata Child { get { return __p_Child ?? (__p_Child = new TestMessage.ChildApp.ChildAppMetadata(App, App.Template.Child)); } }
        private TestMessage.ChildApp.ChildAppMetadata __p_Child;
        public ArrMetadata<TestMessage.AListApp, TestMessage.AListApp.TAListApp> AList { get { return __p_AList ?? (__p_AList = new ArrMetadata<TestMessage.AListApp, TestMessage.AListApp.TAListApp>(App, App.Template.AList)); } }
        private ArrMetadata<TestMessage.AListApp, TestMessage.AListApp.TAListApp> __p_AList;
        public DecimalMetadata ADecimal { get { return __p_ADecimal ?? (__p_ADecimal = new DecimalMetadata(App, App.Template.ADecimal)); } }
        private DecimalMetadata __p_ADecimal;
        public DoubleMetadata ADouble { get { return __p_ADouble ?? (__p_ADouble = new DoubleMetadata(App, App.Template.ADouble)); } }
        private DoubleMetadata __p_ADouble;
        public StringMetadata UserLink { get { return __p_UserLink ?? (__p_UserLink = new StringMetadata(App, App.Template.UserLink)); } }
        private StringMetadata __p_UserLink;
    }
    public static class json {
        public class Child : TemplateAttribute {
            public class ASubApp : TemplateAttribute {
            }
            public class ASubApp2 : TemplateAttribute {
                public class ASubList : TemplateAttribute {
                }
            }
        }
        public class AList : TemplateAttribute {
        }
    }
    public static class Input {
        public class UserId : Input<TestMessage, TLong, long> {
        }
        public class Username : Input<TestMessage, TString, String> {
        }
        public class Password : Input<TestMessage, TString, String> {
        }
        public static class Child {
            public class ChildName : Input<TestMessage.ChildApp, TString, String> {
            }
            public static class ASubApp {
                public class IsInnerApp : Input<TestMessage.ChildApp.ASubAppApp, TBool, bool> {
                }
            }
            public static class ASubApp2 {
                public class IsInnerApp : Input<TestMessage.ChildApp.ASubApp2App, TBool, bool> {
                }
                public static class ASubList {
                    public class Huh : Input<TestMessage.ChildApp.ASubApp2App.ASubListApp, TString, String> {
                    }
                }
            }
        }
        public static class AList {
            public class AValue : Input<TestMessage.AListApp, TString, String> {
            }
            public class ANumber : Input<TestMessage.AListApp, TLong, long> {
            }
        }
        public class ADecimal : Input<TestMessage, TDecimal, Decimal> {
        }
        public class ADouble : Input<TestMessage, TDouble, Double> {
        }
        public class UserLink : Input<TestMessage, TString, String> {
        }
    }
}
}
