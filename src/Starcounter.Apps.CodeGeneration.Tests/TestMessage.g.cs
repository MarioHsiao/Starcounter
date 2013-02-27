// This is a system generated file. It reflects the Starcounter App Template defined in the file "TestMessage.json"
// DO NOT MODIFY DIRECTLY - CHANGES WILL BE OVERWRITTEN

using System;
using System.Collections;
using System.Collections.Generic;
using Starcounter;
using Starcounter.Internal;
using Starcounter.Internal.JsonPatch;
using Starcounter.Templates;

namespace MySampleNamespace {
public partial class TestMessage {
    public static TestMessageTemplate DefaultTemplate = new TestMessageTemplate();
    public TestMessage() { Template = DefaultTemplate; }
    public TestMessage(TestMessageTemplate template) { Template = template; }
    public new TestMessageTemplate Template { get { return (TestMessageTemplate)base.Template; } set { base.Template = value; } }
    public new TestMessageMetadata Metadata { get { return (TestMessageMetadata)base.Metadata; } }
    public long UserId { get { return GetValue(Template.UserId); } set { SetValue(Template.UserId, value); } }
    public String Username { get { return GetValue(Template.Username); } set { SetValue(Template.Username, value); } }
    public String Password { get { return GetValue(Template.Password); } set { SetValue(Template.Password, value); } }
    public TestMessage.ChildApp Child { get { return GetTypedValue<TestMessage.ChildApp>(Template.Child); } set { SetValue(Template.Child, value); } }
    public Arr<TestMessage.AListApp> AList { get { return GetTypedValue<TestMessage.AListApp>(Template.AList); } set { SetValue<TestMessage.AListApp>(Template.AList, value); } }
    public Decimal ADecimal { get { return GetValue(Template.ADecimal); } set { SetValue(Template.ADecimal, value); } }
    public Double ADouble { get { return GetValue(Template.ADouble); } set { SetValue(Template.ADouble, value); } }
    public String UserLink { get { return GetValue(Template.UserLink); } set { SetValue(Template.UserLink, value); } }
    public Action User { get { return GetValue(Template.User); } set { SetValue(Template.User, value); } }
    public class ChildApp : Puppet {
        public static ChildTApp DefaultTemplate = new ChildTApp();
        public ChildApp() { Template = DefaultTemplate; }
        public ChildApp(ChildTApp template) { Template = template; }
        public new ChildTApp Template { get { return (ChildTApp)base.Template; } set { base.Template = value; } }
        public new ChildObjMetadata Metadata { get { return (ChildObjMetadata)base.Metadata; } }
        public new TestMessage Parent { get { return (TestMessage)base.Parent; } set { base.Parent = value; } }
        public String ChildName { get { return GetValue(Template.ChildName); } set { SetValue(Template.ChildName, value); } }
        public Action Button { get { return GetValue(Template.Button); } set { SetValue(Template.Button, value); } }
        public TestMessage.ChildApp.ASubAppApp ASubApp { get { return GetTypedValue<TestMessage.ChildApp.ASubAppApp>(Template.ASubApp); } set { SetValue(Template.ASubApp, value); } }
        public TestMessage.ChildApp.ASubApp2App ASubApp2 { get { return GetTypedValue<TestMessage.ChildApp.ASubApp2App>(Template.ASubApp2); } set { SetValue(Template.ASubApp2, value); } }
        public class ASubAppApp : Puppet {
            public static ASubAppTApp DefaultTemplate = new ASubAppTApp();
            public ASubAppApp() { Template = DefaultTemplate; }
            public ASubAppApp(ASubAppTApp template) { Template = template; }
            public new ASubAppTApp Template { get { return (ASubAppTApp)base.Template; } set { base.Template = value; } }
            public new ASubAppObjMetadata Metadata { get { return (ASubAppObjMetadata)base.Metadata; } }
            public new ChildApp Parent { get { return (ChildApp)base.Parent; } set { base.Parent = value; } }
            public bool IsInnerApp { get { return GetValue(Template.IsInnerApp); } set { SetValue(Template.IsInnerApp, value); } }
            public class ASubAppTApp : TPuppet {
                public ASubAppTApp()
                    : base() {
                    InstanceType = typeof(TestMessage.ChildApp.ASubAppApp);
                    ClassName = "ASubAppApp";
                    IsInnerApp = Register<TBool>("IsInnerApp", "IsInnerApp");
                }
                public override object CreateInstance(Container parent) { return new ASubAppApp(this) { Parent = (TestMessage.ChildApp)parent }; }
                public TBool IsInnerApp;
            }
            public class ASubAppObjMetadata : ObjMetadata {
                public ASubAppObjMetadata(Puppet app, TPuppet template) : base(app, template) { }
                public new TestMessage.ChildApp.ASubAppApp App { get { return (TestMessage.ChildApp.ASubAppApp)base.App; } }
                public new TestMessage.ChildApp.ASubAppApp.ASubAppTApp Template { get { return (TestMessage.ChildApp.ASubAppApp.ASubAppTApp)base.Template; } }
                public BoolMetadata IsInnerApp { get { return __p_IsInnerApp ?? (__p_IsInnerApp = new BoolMetadata(App, App.Template.IsInnerApp)); } }
                private BoolMetadata __p_IsInnerApp;
            }
        }
        public class ASubApp2App : Puppet {
            public static ASubApp2TApp DefaultTemplate = new ASubApp2TApp();
            public ASubApp2App() { Template = DefaultTemplate; }
            public ASubApp2App(ASubApp2TApp template) { Template = template; }
            public new ASubApp2TApp Template { get { return (ASubApp2TApp)base.Template; } set { base.Template = value; } }
            public new ASubApp2ObjMetadata Metadata { get { return (ASubApp2ObjMetadata)base.Metadata; } }
            public new ChildApp Parent { get { return (ChildApp)base.Parent; } set { base.Parent = value; } }
            public bool IsInnerApp { get { return GetValue(Template.IsInnerApp); } set { SetValue(Template.IsInnerApp, value); } }
            public Arr<TestMessage.ChildApp.ASubApp2App.ASubListApp> ASubList { get { return GetTypedValue<TestMessage.ChildApp.ASubApp2App.ASubListApp>(Template.ASubList); } set { SetValue<TestMessage.ChildApp.ASubApp2App.ASubListApp>(Template.ASubList, value); } }
            public class ASubListApp : Puppet {
                public static ASubListTApp DefaultTemplate = new ASubListTApp();
                public ASubListApp() { Template = DefaultTemplate; }
                public ASubListApp(ASubListTApp template) { Template = template; }
                public new ASubListTApp Template { get { return (ASubListTApp)base.Template; } set { base.Template = value; } }
                public new ASubListObjMetadata Metadata { get { return (ASubListObjMetadata)base.Metadata; } }
                public new Arr<TestMessage.ChildApp.ASubApp2App.ASubListApp> Parent { get { return (Arr<TestMessage.ChildApp.ASubApp2App.ASubListApp>)base.Parent; } set { base.Parent = value; } }
                public String Huh { get { return GetValue(Template.Huh); } set { SetValue(Template.Huh, value); } }
                public class ASubListTApp : TPuppet {
                    public ASubListTApp()
                        : base() {
                        InstanceType = typeof(TestMessage.ChildApp.ASubApp2App.ASubListApp);
                        ClassName = "ASubListApp";
                        Huh = Register<TString>("Huh", "Huh");
                    }
                    public override object CreateInstance(Container parent) { return new ASubListApp(this) { Parent = (Arr<TestMessage.ChildApp.ASubApp2App.ASubListApp>)parent }; }
                    public TString Huh;
                }
                public class ASubListObjMetadata : ObjMetadata {
                    public ASubListObjMetadata(Puppet app, TPuppet template) : base(app, template) { }
                    public new TestMessage.ChildApp.ASubApp2App.ASubListApp App { get { return (TestMessage.ChildApp.ASubApp2App.ASubListApp)base.App; } }
                    public new TestMessage.ChildApp.ASubApp2App.ASubListApp.ASubListTApp Template { get { return (TestMessage.ChildApp.ASubApp2App.ASubListApp.ASubListTApp)base.Template; } }
                    public StringMetadata Huh { get { return __p_Huh ?? (__p_Huh = new StringMetadata(App, App.Template.Huh)); } }
                    private StringMetadata __p_Huh;
                }
            }
            public class ASubApp2TApp : TPuppet {
                public ASubApp2TApp()
                    : base() {
                    InstanceType = typeof(TestMessage.ChildApp.ASubApp2App);
                    ClassName = "ASubApp2App";
                    IsInnerApp = Register<TBool>("IsInnerApp", "IsInnerApp");
                    ASubList = Register<TArr<TestMessage.ChildApp.ASubApp2App.ASubListApp, TestMessage.ChildApp.ASubApp2App.ASubListApp.ASubListTApp>>("ASubList", "ASubList");
                    ASubList.App = TestMessage.ChildApp.ASubApp2App.ASubListApp.DefaultTemplate;
                }
                public override object CreateInstance(Container parent) { return new ASubApp2App(this) { Parent = (TestMessage.ChildApp)parent }; }
                public TBool IsInnerApp;
                public TArr<TestMessage.ChildApp.ASubApp2App.ASubListApp, TestMessage.ChildApp.ASubApp2App.ASubListApp.ASubListTApp> ASubList;
            }
            public class ASubApp2ObjMetadata : ObjMetadata {
                public ASubApp2ObjMetadata(Puppet app, TPuppet template) : base(app, template) { }
                public new TestMessage.ChildApp.ASubApp2App App { get { return (TestMessage.ChildApp.ASubApp2App)base.App; } }
                public new TestMessage.ChildApp.ASubApp2App.ASubApp2TApp Template { get { return (TestMessage.ChildApp.ASubApp2App.ASubApp2TApp)base.Template; } }
                public BoolMetadata IsInnerApp { get { return __p_IsInnerApp ?? (__p_IsInnerApp = new BoolMetadata(App, App.Template.IsInnerApp)); } }
                private BoolMetadata __p_IsInnerApp;
                public ArrMetadata<TestMessage.ChildApp.ASubApp2App.ASubListApp, TestMessage.ChildApp.ASubApp2App.ASubListApp.ASubListTApp> ASubList { get { return __p_ASubList ?? (__p_ASubList = new ArrMetadata<TestMessage.ChildApp.ASubApp2App.ASubListApp, TestMessage.ChildApp.ASubApp2App.ASubListApp.ASubListTApp>(App, App.Template.ASubList)); } }
                private ArrMetadata<TestMessage.ChildApp.ASubApp2App.ASubListApp, TestMessage.ChildApp.ASubApp2App.ASubListApp.ASubListTApp> __p_ASubList;
            }
        }
        public class ChildTApp : TPuppet {
            public ChildTApp()
                : base() {
                InstanceType = typeof(TestMessage.ChildApp);
                ClassName = "ChildApp";
                ChildName = Register<TString>("ChildName", "ChildName");
                Button = Register<TTrigger>("Button", "Button");
                ASubApp = Register<TestMessage.ChildApp.ASubAppApp.ASubAppTApp>("ASubApp", "ASubApp");
                ASubApp2 = Register<TestMessage.ChildApp.ASubApp2App.ASubApp2TApp>("ASubApp2", "ASubApp2");
            }
            public override object CreateInstance(Container parent) { return new ChildApp(this) { Parent = (TestMessage)parent }; }
            public TString ChildName;
            public TTrigger Button;
            public TestMessage.ChildApp.ASubAppApp.ASubAppTApp ASubApp;
            public TestMessage.ChildApp.ASubApp2App.ASubApp2TApp ASubApp2;
        }
        public class ChildObjMetadata : ObjMetadata {
            public ChildObjMetadata(Puppet app, TPuppet template) : base(app, template) { }
            public new TestMessage.ChildApp App { get { return (TestMessage.ChildApp)base.App; } }
            public new TestMessage.ChildApp.ChildTApp Template { get { return (TestMessage.ChildApp.ChildTApp)base.Template; } }
            public StringMetadata ChildName { get { return __p_ChildName ?? (__p_ChildName = new StringMetadata(App, App.Template.ChildName)); } }
            private StringMetadata __p_ChildName;
            public ActionMetadata Button { get { return __p_Button ?? (__p_Button = new ActionMetadata(App, App.Template.Button)); } }
            private ActionMetadata __p_Button;
            public TestMessage.ChildApp.ASubAppApp.ASubAppObjMetadata ASubApp { get { return __p_ASubApp ?? (__p_ASubApp = new TestMessage.ChildApp.ASubAppApp.ASubAppObjMetadata(App, App.Template.ASubApp)); } }
            private TestMessage.ChildApp.ASubAppApp.ASubAppObjMetadata __p_ASubApp;
            public TestMessage.ChildApp.ASubApp2App.ASubApp2ObjMetadata ASubApp2 { get { return __p_ASubApp2 ?? (__p_ASubApp2 = new TestMessage.ChildApp.ASubApp2App.ASubApp2ObjMetadata(App, App.Template.ASubApp2)); } }
            private TestMessage.ChildApp.ASubApp2App.ASubApp2ObjMetadata __p_ASubApp2;
        }
    }
    public class AListApp : Puppet {
        public static AListTApp DefaultTemplate = new AListTApp();
        public AListApp() { Template = DefaultTemplate; }
        public AListApp(AListTApp template) { Template = template; }
        public new AListTApp Template { get { return (AListTApp)base.Template; } set { base.Template = value; } }
        public new AListObjMetadata Metadata { get { return (AListObjMetadata)base.Metadata; } }
        public new Arr<TestMessage.AListApp> Parent { get { return (Arr<TestMessage.AListApp>)base.Parent; } set { base.Parent = value; } }
        public String AValue { get { return GetValue(Template.AValue); } set { SetValue(Template.AValue, value); } }
        public long ANumber { get { return GetValue(Template.ANumber); } set { SetValue(Template.ANumber, value); } }
        public class AListTApp : TPuppet {
            public AListTApp()
                : base() {
                InstanceType = typeof(TestMessage.AListApp);
                ClassName = "AListApp";
                AValue = Register<TString>("AValue", "AValue");
                ANumber = Register<TLong>("ANumber", "ANumber");
            }
            public override object CreateInstance(Container parent) { return new AListApp(this) { Parent = (Arr<TestMessage.AListApp>)parent }; }
            public TString AValue;
            public TLong ANumber;
        }
        public class AListObjMetadata : ObjMetadata {
            public AListObjMetadata(Puppet app, TPuppet template) : base(app, template) { }
            public new TestMessage.AListApp App { get { return (TestMessage.AListApp)base.App; } }
            public new TestMessage.AListApp.AListTApp Template { get { return (TestMessage.AListApp.AListTApp)base.Template; } }
            public StringMetadata AValue { get { return __p_AValue ?? (__p_AValue = new StringMetadata(App, App.Template.AValue)); } }
            private StringMetadata __p_AValue;
            public LongMetadata ANumber { get { return __p_ANumber ?? (__p_ANumber = new LongMetadata(App, App.Template.ANumber)); } }
            private LongMetadata __p_ANumber;
        }
    }
    public class TestMessageTemplate : TPuppet {
        public TestMessageTemplate()
            : base() {
            InstanceType = typeof(TestMessage);
            ClassName = "TestMessage";
            UserId = Register<TLong>("UserId$", "UserId", Editable = true);
            Username = Register<TString>("Username", "Username");
            Password = Register<TString>("Password", "Password");
            Child = Register<TestMessage.ChildApp.ChildTApp>("Child", "Child");
            AList = Register<TArr<TestMessage.AListApp, TestMessage.AListApp.AListTApp>>("AList", "AList");
            AList.App = TestMessage.AListApp.DefaultTemplate;
            ADecimal = Register<TDecimal>("ADecimal", "ADecimal");
            ADouble = Register<TDouble>("ADouble", "ADouble");
            UserLink = Register<TString>("UserLink", "UserLink");
            User = Register<TTrigger>("User", "User");
        }
        public override object CreateInstance(Container parent) { return new TestMessage(this) { Parent = parent }; }
        public TLong UserId;
        public TString Username;
        public TString Password;
        public TestMessage.ChildApp.ChildTApp Child;
        public TArr<TestMessage.AListApp, TestMessage.AListApp.AListTApp> AList;
        public TDecimal ADecimal;
        public TDouble ADouble;
        public TString UserLink;
        public TTrigger User;
    }
    public class TestMessageMetadata : ObjMetadata {
        public TestMessageMetadata(Puppet app, TPuppet template) : base(app, template) { }
        public new TestMessage App { get { return (TestMessage)base.App; } }
        public new TestMessage.TestMessageTemplate Template { get { return (TestMessage.TestMessageTemplate)base.Template; } }
        public LongMetadata UserId { get { return __p_UserId ?? (__p_UserId = new LongMetadata(App, App.Template.UserId)); } }
        private LongMetadata __p_UserId;
        public StringMetadata Username { get { return __p_Username ?? (__p_Username = new StringMetadata(App, App.Template.Username)); } }
        private StringMetadata __p_Username;
        public StringMetadata Password { get { return __p_Password ?? (__p_Password = new StringMetadata(App, App.Template.Password)); } }
        private StringMetadata __p_Password;
        public TestMessage.ChildApp.ChildObjMetadata Child { get { return __p_Child ?? (__p_Child = new TestMessage.ChildApp.ChildObjMetadata(App, App.Template.Child)); } }
        private TestMessage.ChildApp.ChildObjMetadata __p_Child;
        public ArrMetadata<TestMessage.AListApp, TestMessage.AListApp.AListTApp> AList { get { return __p_AList ?? (__p_AList = new ArrMetadata<TestMessage.AListApp, TestMessage.AListApp.AListTApp>(App, App.Template.AList)); } }
        private ArrMetadata<TestMessage.AListApp, TestMessage.AListApp.AListTApp> __p_AList;
        public DecimalMetadata ADecimal { get { return __p_ADecimal ?? (__p_ADecimal = new DecimalMetadata(App, App.Template.ADecimal)); } }
        private DecimalMetadata __p_ADecimal;
        public DoubleMetadata ADouble { get { return __p_ADouble ?? (__p_ADouble = new DoubleMetadata(App, App.Template.ADouble)); } }
        private DoubleMetadata __p_ADouble;
        public StringMetadata UserLink { get { return __p_UserLink ?? (__p_UserLink = new StringMetadata(App, App.Template.UserLink)); } }
        private StringMetadata __p_UserLink;
        public ActionMetadata User { get { return __p_User ?? (__p_User = new ActionMetadata(App, App.Template.User)); } }
        private ActionMetadata __p_User;
    }
    public static class Json {
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
        public class UserId : Input<TestMessage, TLong, int> {
        }
        public class Username : Input<TestMessage, TString, String> {
        }
        public class Password : Input<TestMessage, TString, String> {
        }
        public static class Child {
            public class ChildName : Input<TestMessage.ChildApp, TString, String> {
            }
            public class Button : Input<TestMessage.ChildApp, TTrigger, Action> {
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
            public class ANumber : Input<TestMessage.AListApp, TLong, int> {
            }
        }
        public class ADecimal : Input<TestMessage, TDecimal, Decimal> {
        }
        public class ADouble : Input<TestMessage, TDouble, Double> {
        }
        public class UserLink : Input<TestMessage, TString, String> {
        }
        public class User : Input<TestMessage, TTrigger, Action> {
        }
    }
    public static class TestMessageJsonSerializer{

    #pragma warning disable 0414
    private static int VerificationOffset0 = 0; // UserId$
    private static int VerificationOffset1 = 8; // Username
    private static int VerificationOffset2 = 17; // Password
    private static int VerificationOffset3 = 26; // Child
    private static int VerificationOffset4 = 32; // AList
    private static int VerificationOffset5 = 38; // ADecimal
    private static int VerificationOffset6 = 47; // ADouble
    private static int VerificationOffset7 = 55; // UserLink
    private static int VerificationOffset8 = 64; // User
    #pragma warning restore 0414
    private static byte[] VerificationBytes = new byte[] {(byte)'U',(byte)'s',(byte)'e',(byte)'r',(byte)'I',(byte)'d',(byte)'$',(byte)' ',(byte)'U',(byte)'s',(byte)'e',(byte)'r',(byte)'n',(byte)'a',(byte)'m',(byte)'e',(byte)' ',(byte)'P',(byte)'a',(byte)'s',(byte)'s',(byte)'w',(byte)'o',(byte)'r',(byte)'d',(byte)' ',(byte)'C',(byte)'h',(byte)'i',(byte)'l',(byte)'d',(byte)' ',(byte)'A',(byte)'L',(byte)'i',(byte)'s',(byte)'t',(byte)' ',(byte)'A',(byte)'D',(byte)'e',(byte)'c',(byte)'i',(byte)'m',(byte)'a',(byte)'l',(byte)' ',(byte)'A',(byte)'D',(byte)'o',(byte)'u',(byte)'b',(byte)'l',(byte)'e',(byte)' ',(byte)'U',(byte)'s',(byte)'e',(byte)'r',(byte)'L',(byte)'i',(byte)'n',(byte)'k',(byte)' ',(byte)'U',(byte)'s',(byte)'e',(byte)'r',(byte)' '};
    private static IntPtr PointerVerificationBytes;

    static TestMessageJsonSerializer() {
        PointerVerificationBytes = BitsAndBytes.Alloc(VerificationBytes.Length); // TODO. Free when program exists
        BitsAndBytes.SlowMemCopy( PointerVerificationBytes, VerificationBytes, (uint)VerificationBytes.Length);
    }

    public static int Serialize(IntPtr buffer, int bufferSize, TestMessage app) {
        byte[] tmpArr = new byte[1024];
        int valSize;
        unsafe {
            byte* pfrag = (byte*)buffer;
            byte* pver = null;
            int nextSize = bufferSize;
            if ((nextSize - 2) < 0)
                throw new Exception("Buffer too small.");
            nextSize -= 2;
            *pfrag++ = (byte)'{';
            valSize = JsonHelper.WriteString((IntPtr)pfrag, nextSize, "UserId$", tmpArr);
            if (valSize == -1)
                throw new Exception("Buffer too small.");
            nextSize -= valSize;
            pfrag += valSize;
            nextSize--;
            if (nextSize < 0)
                throw new Exception("Buffer too small.");
            *pfrag++ = (byte)':';
            valSize = JsonHelper.WriteInt((IntPtr)pfrag, nextSize, app.UserId);
            if (valSize == -1)
                throw new Exception("Buffer too small.");
            nextSize -= valSize;
            pfrag += valSize;
            nextSize--;
            if (nextSize < 0)
                throw new Exception("Buffer too small.");
            *pfrag++ = (byte)',';
            valSize = JsonHelper.WriteString((IntPtr)pfrag, nextSize, "Username", tmpArr);
            if (valSize == -1)
                throw new Exception("Buffer too small.");
            nextSize -= valSize;
            pfrag += valSize;
            nextSize--;
            if (nextSize < 0)
                throw new Exception("Buffer too small.");
            *pfrag++ = (byte)':';
            valSize = JsonHelper.WriteString((IntPtr)pfrag, nextSize, app.Username, tmpArr);
            if (valSize == -1)
                throw new Exception("Buffer too small.");
            nextSize -= valSize;
            pfrag += valSize;
            nextSize--;
            if (nextSize < 0)
                throw new Exception("Buffer too small.");
            *pfrag++ = (byte)',';
            valSize = JsonHelper.WriteString((IntPtr)pfrag, nextSize, "Password", tmpArr);
            if (valSize == -1)
                throw new Exception("Buffer too small.");
            nextSize -= valSize;
            pfrag += valSize;
            nextSize--;
            if (nextSize < 0)
                throw new Exception("Buffer too small.");
            *pfrag++ = (byte)':';
            valSize = JsonHelper.WriteString((IntPtr)pfrag, nextSize, app.Password, tmpArr);
            if (valSize == -1)
                throw new Exception("Buffer too small.");
            nextSize -= valSize;
            pfrag += valSize;
            nextSize--;
            if (nextSize < 0)
                throw new Exception("Buffer too small.");
            *pfrag++ = (byte)',';
            valSize = JsonHelper.WriteString((IntPtr)pfrag, nextSize, "Child", tmpArr);
            if (valSize == -1)
                throw new Exception("Buffer too small.");
            nextSize -= valSize;
            pfrag += valSize;
            nextSize--;
            if (nextSize < 0)
                throw new Exception("Buffer too small.");
            *pfrag++ = (byte)':';
            valSize = ChildAppJsonSerializer.Serialize((IntPtr)pfrag, nextSize, app.Child);
            if (valSize == -1)
                throw new Exception("Buffer too small.");
            nextSize -= valSize;
            pfrag += valSize;
            nextSize--;
            if (nextSize < 0)
                throw new Exception("Buffer too small.");
            *pfrag++ = (byte)',';
            valSize = JsonHelper.WriteString((IntPtr)pfrag, nextSize, "AList", tmpArr);
            if (valSize == -1)
                throw new Exception("Buffer too small.");
            nextSize -= valSize;
            pfrag += valSize;
            nextSize--;
            if (nextSize < 0)
                throw new Exception("Buffer too small.");
            *pfrag++ = (byte)':';
            if ((nextSize - 2) < 0)
                throw new Exception("Buffer too small.");
            *pfrag++ = (byte)'[';
            nextSize -= 2;
            for(int i = 0; i < app.AList.Count; i++) {
                var listApp = app.AList[i];
                valSize = AListAppJsonSerializer.Serialize((IntPtr)pfrag, nextSize, listApp);
                if (valSize == -1)
                    throw new Exception("Buffer too small.");
                nextSize -= valSize;
                pfrag += valSize;
                if ((i+1) < app.AList.Count) {
                    nextSize--;
                    if (nextSize < 0)
                        throw new Exception("Buffer too small.");
                    *pfrag++ = (byte)',';
                }
            }
            *pfrag++ = (byte)']';
            nextSize--;
            if (nextSize < 0)
                throw new Exception("Buffer too small.");
            *pfrag++ = (byte)',';
            valSize = JsonHelper.WriteString((IntPtr)pfrag, nextSize, "ADecimal", tmpArr);
            if (valSize == -1)
                throw new Exception("Buffer too small.");
            nextSize -= valSize;
            pfrag += valSize;
            nextSize--;
            if (nextSize < 0)
                throw new Exception("Buffer too small.");
            *pfrag++ = (byte)':';
            valSize = JsonHelper.WriteDecimal((IntPtr)pfrag, nextSize, app.ADecimal, tmpArr);
            if (valSize == -1)
                throw new Exception("Buffer too small.");
            nextSize -= valSize;
            pfrag += valSize;
            nextSize--;
            if (nextSize < 0)
                throw new Exception("Buffer too small.");
            *pfrag++ = (byte)',';
            valSize = JsonHelper.WriteString((IntPtr)pfrag, nextSize, "ADouble", tmpArr);
            if (valSize == -1)
                throw new Exception("Buffer too small.");
            nextSize -= valSize;
            pfrag += valSize;
            nextSize--;
            if (nextSize < 0)
                throw new Exception("Buffer too small.");
            *pfrag++ = (byte)':';
            valSize = JsonHelper.WriteDouble((IntPtr)pfrag, nextSize, app.ADouble, tmpArr);
            if (valSize == -1)
                throw new Exception("Buffer too small.");
            nextSize -= valSize;
            pfrag += valSize;
            nextSize--;
            if (nextSize < 0)
                throw new Exception("Buffer too small.");
            *pfrag++ = (byte)',';
            valSize = JsonHelper.WriteString((IntPtr)pfrag, nextSize, "UserLink", tmpArr);
            if (valSize == -1)
                throw new Exception("Buffer too small.");
            nextSize -= valSize;
            pfrag += valSize;
            nextSize--;
            if (nextSize < 0)
                throw new Exception("Buffer too small.");
            *pfrag++ = (byte)':';
            valSize = JsonHelper.WriteString((IntPtr)pfrag, nextSize, app.UserLink, tmpArr);
            if (valSize == -1)
                throw new Exception("Buffer too small.");
            nextSize -= valSize;
            pfrag += valSize;
            nextSize--;
            if (nextSize < 0)
                throw new Exception("Buffer too small.");
            *pfrag++ = (byte)',';
            valSize = JsonHelper.WriteString((IntPtr)pfrag, nextSize, "User", tmpArr);
            if (valSize == -1)
                throw new Exception("Buffer too small.");
            nextSize -= valSize;
            pfrag += valSize;
            nextSize--;
            if (nextSize < 0)
                throw new Exception("Buffer too small.");
            *pfrag++ = (byte)':';
            valSize = JsonHelper.WriteNull((IntPtr)pfrag, nextSize);
            if (valSize == -1)
                throw new Exception("Buffer too small.");
            nextSize -= valSize;
            pfrag += valSize;
            *pfrag++ = (byte)'}';
            return (bufferSize - nextSize);
        }
    }
    public static TestMessage Deserialize(IntPtr buffer, int bufferSize, out int usedSize) {
        int valueSize;
        TestMessage app = new TestMessage();
        unsafe {
            byte* pfrag = (byte*)buffer;
            byte* pver = null;
            int nextSize = bufferSize;
            while (nextSize > 0) {
                // Skip until start of next property or end of current object.
                while (true) {
                    if (*pfrag == '"')
                        break;
                    if (*pfrag == '}') {
                        pfrag++;
                        nextSize--;
                        usedSize = bufferSize - nextSize;
                        return app;
                    }
                    pfrag++;
                    nextSize--;
                    if (nextSize < 0)
                         throw new Exception("Deserialization failed.");
                }
                pfrag++;
                nextSize--;
                if (nextSize < 0)
                    throw new Exception("Deserialization failed.");
                switch (*pfrag) {
                    case (byte)'U':
                        pfrag++;
                        nextSize--;
                        pver = ((byte*)PointerVerificationBytes + VerificationOffset0 + 1);
                        nextSize -= 2;
                        if (nextSize<0 || (*(UInt16*)pfrag) != (*(UInt16*)pver) ) {
                            throw new Exception("Deserialization failed. Verification failed.");
                        }
                        pfrag += 2;
                        pver += 2;
                        nextSize --;
                        if (nextSize<0 || (*pfrag) != (*pver) ) {
                            throw new Exception("Deserialization failed. Verification failed.");
                        }
                        pfrag++;
                        pver++;
                        switch (*pfrag) {
                            case (byte)'I':
                                pfrag++;
                                nextSize--;
                                pver = ((byte*)PointerVerificationBytes + VerificationOffset0 + 5);
                                nextSize -= 2;
                                if (nextSize<0 || (*(UInt16*)pfrag) != (*(UInt16*)pver) ) {
                                    throw new Exception("Deserialization failed. Verification failed.");
                                }
                                pfrag += 2;
                                pver += 2;
                                // Skip until start of value to parse.
                                while (*pfrag != ':') {
                                    pfrag++;
                                    nextSize--;
                                    if (nextSize < 0)
                                         throw new Exception("Deserialization failed.");
                                }
                                pfrag++; // Skip ':' or ','
                                nextSize--;
                                if (nextSize < 0)
                                    throw new Exception("Deserialization failed.");
                                while (*pfrag == ' ' || *pfrag == '\n' || *pfrag == '\r') {
                                    pfrag++;
                                    nextSize--;
                                    if (nextSize < 0)
                                         throw new Exception("Deserialization failed.");
                                }
                                Int64 val0;
                                if (JsonHelper.ParseInt((IntPtr)pfrag, nextSize, out val0, out valueSize)) {
                                    app.UserId = val0;
                                    nextSize -= valueSize;
                                    if (nextSize < 0) {
                                        throw new Exception("Unable to deserialize App. Unexpected end of content");
                                    }
                                    pfrag += valueSize;
                                } else {
                                    throw new Exception("Unable to deserialize App. Content not compatible.");
                                }
                               break;
                            case (byte)'n':
                                pfrag++;
                                nextSize--;
                                pver = ((byte*)PointerVerificationBytes + VerificationOffset1 + 5);
                                nextSize -= 2;
                                if (nextSize<0 || (*(UInt16*)pfrag) != (*(UInt16*)pver) ) {
                                    throw new Exception("Deserialization failed. Verification failed.");
                                }
                                pfrag += 2;
                                pver += 2;
                                nextSize --;
                                if (nextSize<0 || (*pfrag) != (*pver) ) {
                                    throw new Exception("Deserialization failed. Verification failed.");
                                }
                                pfrag++;
                                pver++;
                                // Skip until start of value to parse.
                                while (*pfrag != ':') {
                                    pfrag++;
                                    nextSize--;
                                    if (nextSize < 0)
                                         throw new Exception("Deserialization failed.");
                                }
                                pfrag++; // Skip ':' or ','
                                nextSize--;
                                if (nextSize < 0)
                                    throw new Exception("Deserialization failed.");
                                while (*pfrag == ' ' || *pfrag == '\n' || *pfrag == '\r') {
                                    pfrag++;
                                    nextSize--;
                                    if (nextSize < 0)
                                         throw new Exception("Deserialization failed.");
                                }
                                String val1;
                                if (JsonHelper.ParseString((IntPtr)pfrag, nextSize, out val1, out valueSize)) {
                                    app.Username = val1;
                                    nextSize -= valueSize;
                                    if (nextSize < 0) {
                                        throw new Exception("Unable to deserialize App. Unexpected end of content");
                                    }
                                    pfrag += valueSize;
                                } else {
                                    throw new Exception("Unable to deserialize App. Content not compatible.");
                                }
                               break;
                            case (byte)'L':
                                pfrag++;
                                nextSize--;
                                pver = ((byte*)PointerVerificationBytes + VerificationOffset7 + 5);
                                nextSize -= 2;
                                if (nextSize<0 || (*(UInt16*)pfrag) != (*(UInt16*)pver) ) {
                                    throw new Exception("Deserialization failed. Verification failed.");
                                }
                                pfrag += 2;
                                pver += 2;
                                nextSize --;
                                if (nextSize<0 || (*pfrag) != (*pver) ) {
                                    throw new Exception("Deserialization failed. Verification failed.");
                                }
                                pfrag++;
                                pver++;
                                // Skip until start of value to parse.
                                while (*pfrag != ':') {
                                    pfrag++;
                                    nextSize--;
                                    if (nextSize < 0)
                                         throw new Exception("Deserialization failed.");
                                }
                                pfrag++; // Skip ':' or ','
                                nextSize--;
                                if (nextSize < 0)
                                    throw new Exception("Deserialization failed.");
                                while (*pfrag == ' ' || *pfrag == '\n' || *pfrag == '\r') {
                                    pfrag++;
                                    nextSize--;
                                    if (nextSize < 0)
                                         throw new Exception("Deserialization failed.");
                                }
                                String val7;
                                if (JsonHelper.ParseString((IntPtr)pfrag, nextSize, out val7, out valueSize)) {
                                    app.UserLink = val7;
                                    nextSize -= valueSize;
                                    if (nextSize < 0) {
                                        throw new Exception("Unable to deserialize App. Unexpected end of content");
                                    }
                                    pfrag += valueSize;
                                } else {
                                    throw new Exception("Unable to deserialize App. Content not compatible.");
                                }
                               break;
                            case (byte)' ':
                            case (byte)'"':
                                pfrag++;
                                nextSize--;
                                // Skip until start of value to parse.
                                while (*pfrag != ':') {
                                    pfrag++;
                                    nextSize--;
                                    if (nextSize < 0)
                                         throw new Exception("Deserialization failed.");
                                }
                                pfrag++; // Skip ':' or ','
                                nextSize--;
                                if (nextSize < 0)
                                    throw new Exception("Deserialization failed.");
                                while (*pfrag == ' ' || *pfrag == '\n' || *pfrag == '\r') {
                                    pfrag++;
                                    nextSize--;
                                    if (nextSize < 0)
                                         throw new Exception("Deserialization failed.");
                                }
                                if (JsonHelper.IsNullValue((IntPtr)pfrag, nextSize, out valueSize)) {
                                    nextSize -= valueSize;
                                    if (nextSize < 0) {
                                        throw new Exception("Unable to deserialize App. Unexpected end of content");
                                    }
                                    pfrag += valueSize;
                                } else {
                                    throw new Exception("Unable to deserialize App. Content not compatible.");
                                }
                               break;
                            default:
                                throw new Exception("Property not belonging to this app found in content.");
                        }
                       break;
                    case (byte)'P':
                        pfrag++;
                        nextSize--;
                        pver = ((byte*)PointerVerificationBytes + VerificationOffset2 + 1);
                        nextSize -= 4;
                        if (nextSize<0 || (*(UInt32*)pfrag) !=  (*(UInt32*)pver) ) {
                            throw new Exception("Deserialization failed. Verification failed.");
                        }
                        pfrag += 4;
                        pver += 4;
                        nextSize -= 2;
                        if (nextSize<0 || (*(UInt16*)pfrag) != (*(UInt16*)pver) ) {
                            throw new Exception("Deserialization failed. Verification failed.");
                        }
                        pfrag += 2;
                        pver += 2;
                        nextSize --;
                        if (nextSize<0 || (*pfrag) != (*pver) ) {
                            throw new Exception("Deserialization failed. Verification failed.");
                        }
                        pfrag++;
                        pver++;
                        // Skip until start of value to parse.
                        while (*pfrag != ':') {
                            pfrag++;
                            nextSize--;
                            if (nextSize < 0)
                                 throw new Exception("Deserialization failed.");
                        }
                        pfrag++; // Skip ':' or ','
                        nextSize--;
                        if (nextSize < 0)
                            throw new Exception("Deserialization failed.");
                        while (*pfrag == ' ' || *pfrag == '\n' || *pfrag == '\r') {
                            pfrag++;
                            nextSize--;
                            if (nextSize < 0)
                                 throw new Exception("Deserialization failed.");
                        }
                        String val2;
                        if (JsonHelper.ParseString((IntPtr)pfrag, nextSize, out val2, out valueSize)) {
                            app.Password = val2;
                            nextSize -= valueSize;
                            if (nextSize < 0) {
                                throw new Exception("Unable to deserialize App. Unexpected end of content");
                            }
                            pfrag += valueSize;
                        } else {
                            throw new Exception("Unable to deserialize App. Content not compatible.");
                        }
                       break;
                    case (byte)'C':
                        pfrag++;
                        nextSize--;
                        pver = ((byte*)PointerVerificationBytes + VerificationOffset3 + 1);
                        nextSize -= 4;
                        if (nextSize<0 || (*(UInt32*)pfrag) !=  (*(UInt32*)pver) ) {
                            throw new Exception("Deserialization failed. Verification failed.");
                        }
                        pfrag += 4;
                        pver += 4;
                        // Skip until start of value to parse.
                        while (*pfrag != ':') {
                            pfrag++;
                            nextSize--;
                            if (nextSize < 0)
                                 throw new Exception("Deserialization failed.");
                        }
                        pfrag++; // Skip ':' or ','
                        nextSize--;
                        if (nextSize < 0)
                            throw new Exception("Deserialization failed.");
                        while (*pfrag == ' ' || *pfrag == '\n' || *pfrag == '\r') {
                            pfrag++;
                            nextSize--;
                            if (nextSize < 0)
                                 throw new Exception("Deserialization failed.");
                        }
                        var val3 = ChildAppJsonSerializer.Deserialize((IntPtr)pfrag, nextSize, out valueSize);
                            app.Child = val3;
                            nextSize -= valueSize;
                            if (nextSize < 0) {
                                throw new Exception("Unable to deserialize App. Unexpected end of content");
                            }
                            pfrag += valueSize;
                       break;
                    case (byte)'A':
                        pfrag++;
                        nextSize--;
                        switch (*pfrag) {
                            case (byte)'L':
                                pfrag++;
                                nextSize--;
                                pver = ((byte*)PointerVerificationBytes + VerificationOffset4 + 2);
                                nextSize -= 2;
                                if (nextSize<0 || (*(UInt16*)pfrag) != (*(UInt16*)pver) ) {
                                    throw new Exception("Deserialization failed. Verification failed.");
                                }
                                pfrag += 2;
                                pver += 2;
                                nextSize --;
                                if (nextSize<0 || (*pfrag) != (*pver) ) {
                                    throw new Exception("Deserialization failed. Verification failed.");
                                }
                                pfrag++;
                                pver++;
                                // Skip until start of value to parse.
                                while (*pfrag != ':') {
                                    pfrag++;
                                    nextSize--;
                                    if (nextSize < 0)
                                         throw new Exception("Deserialization failed.");
                                }
                                pfrag++; // Skip ':' or ','
                                nextSize--;
                                if (nextSize < 0)
                                    throw new Exception("Deserialization failed.");
                                while (*pfrag == ' ' || *pfrag == '\n' || *pfrag == '\r') {
                                    pfrag++;
                                    nextSize--;
                                    if (nextSize < 0)
                                         throw new Exception("Deserialization failed.");
                                }
                                if (*pfrag++ == '[') {
                                    nextSize--;
                                    while (*pfrag != '{' && *pfrag != ']') { // find first object or end of array
                                        pfrag++;
                                        nextSize--;
                                    }
                                    if (*pfrag != ']') {
                                    while (nextSize > 0) {
                                        var val4 = AListAppJsonSerializer.Deserialize((IntPtr)pfrag, nextSize, out valueSize);
                                            app.AList.Add(val4);
                                            nextSize -= valueSize;
                                            if (nextSize < 0) {
                                                throw new Exception("Unable to deserialize App. Unexpected end of content");
                                            }
                                            pfrag += valueSize;
                                            // Skip until start of value to parse.
                                            while (*pfrag != ',') {
                                                if (*pfrag == ']')
                                                    break;
                                                pfrag++;
                                                nextSize--;
                                                if (nextSize < 0)
                                                     throw new Exception("Deserialization failed.");
                                            }
                                            if (*pfrag == ']')
                                                break;
                                            pfrag++; // Skip ':' or ','
                                            nextSize--;
                                            if (nextSize < 0)
                                                throw new Exception("Deserialization failed.");
                                            while (*pfrag == ' ' || *pfrag == '\n' || *pfrag == '\r') {
                                                pfrag++;
                                                nextSize--;
                                                if (nextSize < 0)
                                                     throw new Exception("Deserialization failed.");
                                            }
                                    }
                                    }
                                } else
                                    throw new Exception("Invalid array value");
                               break;
                            case (byte)'D':
                                pfrag++;
                                nextSize--;
                                switch (*pfrag) {
                                    case (byte)'e':
                                        pfrag++;
                                        nextSize--;
                                        pver = ((byte*)PointerVerificationBytes + VerificationOffset5 + 3);
                                        nextSize -= 4;
                                        if (nextSize<0 || (*(UInt32*)pfrag) !=  (*(UInt32*)pver) ) {
                                            throw new Exception("Deserialization failed. Verification failed.");
                                        }
                                        pfrag += 4;
                                        pver += 4;
                                        nextSize --;
                                        if (nextSize<0 || (*pfrag) != (*pver) ) {
                                            throw new Exception("Deserialization failed. Verification failed.");
                                        }
                                        pfrag++;
                                        pver++;
                                        // Skip until start of value to parse.
                                        while (*pfrag != ':') {
                                            pfrag++;
                                            nextSize--;
                                            if (nextSize < 0)
                                                 throw new Exception("Deserialization failed.");
                                        }
                                        pfrag++; // Skip ':' or ','
                                        nextSize--;
                                        if (nextSize < 0)
                                            throw new Exception("Deserialization failed.");
                                        while (*pfrag == ' ' || *pfrag == '\n' || *pfrag == '\r') {
                                            pfrag++;
                                            nextSize--;
                                            if (nextSize < 0)
                                                 throw new Exception("Deserialization failed.");
                                        }
                                        Decimal val5;
                                        if (JsonHelper.ParseDecimal((IntPtr)pfrag, nextSize, out val5, out valueSize)) {
                                            app.ADecimal = val5;
                                            nextSize -= valueSize;
                                            if (nextSize < 0) {
                                                throw new Exception("Unable to deserialize App. Unexpected end of content");
                                            }
                                            pfrag += valueSize;
                                        } else {
                                            throw new Exception("Unable to deserialize App. Content not compatible.");
                                        }
                                       break;
                                    case (byte)'o':
                                        pfrag++;
                                        nextSize--;
                                        pver = ((byte*)PointerVerificationBytes + VerificationOffset6 + 3);
                                        nextSize -= 4;
                                        if (nextSize<0 || (*(UInt32*)pfrag) !=  (*(UInt32*)pver) ) {
                                            throw new Exception("Deserialization failed. Verification failed.");
                                        }
                                        pfrag += 4;
                                        pver += 4;
                                        // Skip until start of value to parse.
                                        while (*pfrag != ':') {
                                            pfrag++;
                                            nextSize--;
                                            if (nextSize < 0)
                                                 throw new Exception("Deserialization failed.");
                                        }
                                        pfrag++; // Skip ':' or ','
                                        nextSize--;
                                        if (nextSize < 0)
                                            throw new Exception("Deserialization failed.");
                                        while (*pfrag == ' ' || *pfrag == '\n' || *pfrag == '\r') {
                                            pfrag++;
                                            nextSize--;
                                            if (nextSize < 0)
                                                 throw new Exception("Deserialization failed.");
                                        }
                                        Double val6;
                                        if (JsonHelper.ParseDouble((IntPtr)pfrag, nextSize, out val6, out valueSize)) {
                                            app.ADouble = val6;
                                            nextSize -= valueSize;
                                            if (nextSize < 0) {
                                                throw new Exception("Unable to deserialize App. Unexpected end of content");
                                            }
                                            pfrag += valueSize;
                                        } else {
                                            throw new Exception("Unable to deserialize App. Content not compatible.");
                                        }
                                       break;
                                    default:
                                        throw new Exception("Property not belonging to this app found in content.");
                                }
                               break;
                            default:
                                throw new Exception("Property not belonging to this app found in content.");
                        }
                       break;
                    default:
                        throw new Exception("Property not belonging to this app found in content.");
                }
            }
        }
        throw new Exception("Deserialization of App failed.");
    }
}

    public static class ChildAppJsonSerializer{

    #pragma warning disable 0414
    private static int VerificationOffset0 = 0; // ChildName
    private static int VerificationOffset1 = 10; // Button
    private static int VerificationOffset2 = 17; // ASubApp
    private static int VerificationOffset3 = 25; // ASubApp2
    #pragma warning restore 0414
    private static byte[] VerificationBytes = new byte[] {(byte)'C',(byte)'h',(byte)'i',(byte)'l',(byte)'d',(byte)'N',(byte)'a',(byte)'m',(byte)'e',(byte)' ',(byte)'B',(byte)'u',(byte)'t',(byte)'t',(byte)'o',(byte)'n',(byte)' ',(byte)'A',(byte)'S',(byte)'u',(byte)'b',(byte)'A',(byte)'p',(byte)'p',(byte)' ',(byte)'A',(byte)'S',(byte)'u',(byte)'b',(byte)'A',(byte)'p',(byte)'p',(byte)'2',(byte)' '};
    private static IntPtr PointerVerificationBytes;

    static ChildAppJsonSerializer() {
        PointerVerificationBytes = BitsAndBytes.Alloc(VerificationBytes.Length); // TODO. Free when program exists
        BitsAndBytes.SlowMemCopy( PointerVerificationBytes, VerificationBytes, (uint)VerificationBytes.Length);
    }

    public static int Serialize(IntPtr buffer, int bufferSize, ChildApp app) {
        byte[] tmpArr = new byte[1024];
        int valSize;
        unsafe {
            byte* pfrag = (byte*)buffer;
            byte* pver = null;
            int nextSize = bufferSize;
            if ((nextSize - 2) < 0)
                throw new Exception("Buffer too small.");
            nextSize -= 2;
            *pfrag++ = (byte)'{';
            valSize = JsonHelper.WriteString((IntPtr)pfrag, nextSize, "ChildName", tmpArr);
            if (valSize == -1)
                throw new Exception("Buffer too small.");
            nextSize -= valSize;
            pfrag += valSize;
            nextSize--;
            if (nextSize < 0)
                throw new Exception("Buffer too small.");
            *pfrag++ = (byte)':';
            valSize = JsonHelper.WriteString((IntPtr)pfrag, nextSize, app.ChildName, tmpArr);
            if (valSize == -1)
                throw new Exception("Buffer too small.");
            nextSize -= valSize;
            pfrag += valSize;
            nextSize--;
            if (nextSize < 0)
                throw new Exception("Buffer too small.");
            *pfrag++ = (byte)',';
            valSize = JsonHelper.WriteString((IntPtr)pfrag, nextSize, "Button", tmpArr);
            if (valSize == -1)
                throw new Exception("Buffer too small.");
            nextSize -= valSize;
            pfrag += valSize;
            nextSize--;
            if (nextSize < 0)
                throw new Exception("Buffer too small.");
            *pfrag++ = (byte)':';
            valSize = JsonHelper.WriteNull((IntPtr)pfrag, nextSize);
            if (valSize == -1)
                throw new Exception("Buffer too small.");
            nextSize -= valSize;
            pfrag += valSize;
            nextSize--;
            if (nextSize < 0)
                throw new Exception("Buffer too small.");
            *pfrag++ = (byte)',';
            valSize = JsonHelper.WriteString((IntPtr)pfrag, nextSize, "ASubApp", tmpArr);
            if (valSize == -1)
                throw new Exception("Buffer too small.");
            nextSize -= valSize;
            pfrag += valSize;
            nextSize--;
            if (nextSize < 0)
                throw new Exception("Buffer too small.");
            *pfrag++ = (byte)':';
            valSize = ASubAppAppJsonSerializer.Serialize((IntPtr)pfrag, nextSize, app.ASubApp);
            if (valSize == -1)
                throw new Exception("Buffer too small.");
            nextSize -= valSize;
            pfrag += valSize;
            nextSize--;
            if (nextSize < 0)
                throw new Exception("Buffer too small.");
            *pfrag++ = (byte)',';
            valSize = JsonHelper.WriteString((IntPtr)pfrag, nextSize, "ASubApp2", tmpArr);
            if (valSize == -1)
                throw new Exception("Buffer too small.");
            nextSize -= valSize;
            pfrag += valSize;
            nextSize--;
            if (nextSize < 0)
                throw new Exception("Buffer too small.");
            *pfrag++ = (byte)':';
            valSize = ASubApp2AppJsonSerializer.Serialize((IntPtr)pfrag, nextSize, app.ASubApp2);
            if (valSize == -1)
                throw new Exception("Buffer too small.");
            nextSize -= valSize;
            pfrag += valSize;
            *pfrag++ = (byte)'}';
            return (bufferSize - nextSize);
        }
    }
    public static ChildApp Deserialize(IntPtr buffer, int bufferSize, out int usedSize) {
        int valueSize;
        ChildApp app = new ChildApp();
        unsafe {
            byte* pfrag = (byte*)buffer;
            byte* pver = null;
            int nextSize = bufferSize;
            while (nextSize > 0) {
                // Skip until start of next property or end of current object.
                while (true) {
                    if (*pfrag == '"')
                        break;
                    if (*pfrag == '}') {
                        pfrag++;
                        nextSize--;
                        usedSize = bufferSize - nextSize;
                        return app;
                    }
                    pfrag++;
                    nextSize--;
                    if (nextSize < 0)
                         throw new Exception("Deserialization failed.");
                }
                pfrag++;
                nextSize--;
                if (nextSize < 0)
                    throw new Exception("Deserialization failed.");
                switch (*pfrag) {
                    case (byte)'C':
                        pfrag++;
                        nextSize--;
                        pver = ((byte*)PointerVerificationBytes + VerificationOffset0 + 1);
                        nextSize -= 8;
                        if (nextSize<0 || (*(UInt64*)pfrag) != (*(UInt64*)pver) ) {
                            throw new Exception("Deserialization failed. Verification failed.");
                        }
                        pfrag += 8;
                        pver += 8;
                        // Skip until start of value to parse.
                        while (*pfrag != ':') {
                            pfrag++;
                            nextSize--;
                            if (nextSize < 0)
                                 throw new Exception("Deserialization failed.");
                        }
                        pfrag++; // Skip ':' or ','
                        nextSize--;
                        if (nextSize < 0)
                            throw new Exception("Deserialization failed.");
                        while (*pfrag == ' ' || *pfrag == '\n' || *pfrag == '\r') {
                            pfrag++;
                            nextSize--;
                            if (nextSize < 0)
                                 throw new Exception("Deserialization failed.");
                        }
                        String val0;
                        if (JsonHelper.ParseString((IntPtr)pfrag, nextSize, out val0, out valueSize)) {
                            app.ChildName = val0;
                            nextSize -= valueSize;
                            if (nextSize < 0) {
                                throw new Exception("Unable to deserialize App. Unexpected end of content");
                            }
                            pfrag += valueSize;
                        } else {
                            throw new Exception("Unable to deserialize App. Content not compatible.");
                        }
                       break;
                    case (byte)'B':
                        pfrag++;
                        nextSize--;
                        pver = ((byte*)PointerVerificationBytes + VerificationOffset1 + 1);
                        nextSize -= 4;
                        if (nextSize<0 || (*(UInt32*)pfrag) !=  (*(UInt32*)pver) ) {
                            throw new Exception("Deserialization failed. Verification failed.");
                        }
                        pfrag += 4;
                        pver += 4;
                        nextSize --;
                        if (nextSize<0 || (*pfrag) != (*pver) ) {
                            throw new Exception("Deserialization failed. Verification failed.");
                        }
                        pfrag++;
                        pver++;
                        // Skip until start of value to parse.
                        while (*pfrag != ':') {
                            pfrag++;
                            nextSize--;
                            if (nextSize < 0)
                                 throw new Exception("Deserialization failed.");
                        }
                        pfrag++; // Skip ':' or ','
                        nextSize--;
                        if (nextSize < 0)
                            throw new Exception("Deserialization failed.");
                        while (*pfrag == ' ' || *pfrag == '\n' || *pfrag == '\r') {
                            pfrag++;
                            nextSize--;
                            if (nextSize < 0)
                                 throw new Exception("Deserialization failed.");
                        }
                        if (JsonHelper.IsNullValue((IntPtr)pfrag, nextSize, out valueSize)) {
                            nextSize -= valueSize;
                            if (nextSize < 0) {
                                throw new Exception("Unable to deserialize App. Unexpected end of content");
                            }
                            pfrag += valueSize;
                        } else {
                            throw new Exception("Unable to deserialize App. Content not compatible.");
                        }
                       break;
                    case (byte)'A':
                        pfrag++;
                        nextSize--;
                        pver = ((byte*)PointerVerificationBytes + VerificationOffset2 + 1);
                        nextSize -= 4;
                        if (nextSize<0 || (*(UInt32*)pfrag) !=  (*(UInt32*)pver) ) {
                            throw new Exception("Deserialization failed. Verification failed.");
                        }
                        pfrag += 4;
                        pver += 4;
                        nextSize -= 2;
                        if (nextSize<0 || (*(UInt16*)pfrag) != (*(UInt16*)pver) ) {
                            throw new Exception("Deserialization failed. Verification failed.");
                        }
                        pfrag += 2;
                        pver += 2;
                        switch (*pfrag) {
                            case (byte)' ':
                            case (byte)'"':
                                pfrag++;
                                nextSize--;
                                // Skip until start of value to parse.
                                while (*pfrag != ':') {
                                    pfrag++;
                                    nextSize--;
                                    if (nextSize < 0)
                                         throw new Exception("Deserialization failed.");
                                }
                                pfrag++; // Skip ':' or ','
                                nextSize--;
                                if (nextSize < 0)
                                    throw new Exception("Deserialization failed.");
                                while (*pfrag == ' ' || *pfrag == '\n' || *pfrag == '\r') {
                                    pfrag++;
                                    nextSize--;
                                    if (nextSize < 0)
                                         throw new Exception("Deserialization failed.");
                                }
                                var val2 = ASubAppAppJsonSerializer.Deserialize((IntPtr)pfrag, nextSize, out valueSize);
                                    app.ASubApp = val2;
                                    nextSize -= valueSize;
                                    if (nextSize < 0) {
                                        throw new Exception("Unable to deserialize App. Unexpected end of content");
                                    }
                                    pfrag += valueSize;
                               break;
                            case (byte)'2':
                                pfrag++;
                                nextSize--;
                                // Skip until start of value to parse.
                                while (*pfrag != ':') {
                                    pfrag++;
                                    nextSize--;
                                    if (nextSize < 0)
                                         throw new Exception("Deserialization failed.");
                                }
                                pfrag++; // Skip ':' or ','
                                nextSize--;
                                if (nextSize < 0)
                                    throw new Exception("Deserialization failed.");
                                while (*pfrag == ' ' || *pfrag == '\n' || *pfrag == '\r') {
                                    pfrag++;
                                    nextSize--;
                                    if (nextSize < 0)
                                         throw new Exception("Deserialization failed.");
                                }
                                var val3 = ASubApp2AppJsonSerializer.Deserialize((IntPtr)pfrag, nextSize, out valueSize);
                                    app.ASubApp2 = val3;
                                    nextSize -= valueSize;
                                    if (nextSize < 0) {
                                        throw new Exception("Unable to deserialize App. Unexpected end of content");
                                    }
                                    pfrag += valueSize;
                               break;
                            default:
                                throw new Exception("Property not belonging to this app found in content.");
                        }
                       break;
                    default:
                        throw new Exception("Property not belonging to this app found in content.");
                }
            }
        }
        throw new Exception("Deserialization of App failed.");
    }
}

    public static class ASubAppAppJsonSerializer{

    #pragma warning disable 0414
    private static int VerificationOffset0 = 0; // IsInnerApp
    #pragma warning restore 0414
    private static byte[] VerificationBytes = new byte[] {(byte)'I',(byte)'s',(byte)'I',(byte)'n',(byte)'n',(byte)'e',(byte)'r',(byte)'A',(byte)'p',(byte)'p',(byte)' '};
    private static IntPtr PointerVerificationBytes;

    static ASubAppAppJsonSerializer() {
        PointerVerificationBytes = BitsAndBytes.Alloc(VerificationBytes.Length); // TODO. Free when program exists
        BitsAndBytes.SlowMemCopy( PointerVerificationBytes, VerificationBytes, (uint)VerificationBytes.Length);
    }

    public static int Serialize(IntPtr buffer, int bufferSize, ChildApp.ASubAppApp app) {
        byte[] tmpArr = new byte[1024];
        int valSize;
        unsafe {
            byte* pfrag = (byte*)buffer;
            byte* pver = null;
            int nextSize = bufferSize;
            if ((nextSize - 2) < 0)
                throw new Exception("Buffer too small.");
            nextSize -= 2;
            *pfrag++ = (byte)'{';
            valSize = JsonHelper.WriteString((IntPtr)pfrag, nextSize, "IsInnerApp", tmpArr);
            if (valSize == -1)
                throw new Exception("Buffer too small.");
            nextSize -= valSize;
            pfrag += valSize;
            nextSize--;
            if (nextSize < 0)
                throw new Exception("Buffer too small.");
            *pfrag++ = (byte)':';
            valSize = JsonHelper.WriteBool((IntPtr)pfrag, nextSize, app.IsInnerApp);
            if (valSize == -1)
                throw new Exception("Buffer too small.");
            nextSize -= valSize;
            pfrag += valSize;
            *pfrag++ = (byte)'}';
            return (bufferSize - nextSize);
        }
    }
    public static ChildApp.ASubAppApp Deserialize(IntPtr buffer, int bufferSize, out int usedSize) {
        int valueSize;
        ChildApp.ASubAppApp app = new ChildApp.ASubAppApp();
        unsafe {
            byte* pfrag = (byte*)buffer;
            byte* pver = null;
            int nextSize = bufferSize;
            while (nextSize > 0) {
                // Skip until start of next property or end of current object.
                while (true) {
                    if (*pfrag == '"')
                        break;
                    if (*pfrag == '}') {
                        pfrag++;
                        nextSize--;
                        usedSize = bufferSize - nextSize;
                        return app;
                    }
                    pfrag++;
                    nextSize--;
                    if (nextSize < 0)
                         throw new Exception("Deserialization failed.");
                }
                pfrag++;
                nextSize--;
                if (nextSize < 0)
                    throw new Exception("Deserialization failed.");
                pver = ((byte*)PointerVerificationBytes + VerificationOffset0 + 0);
                nextSize -= 8;
                if (nextSize<0 || (*(UInt64*)pfrag) != (*(UInt64*)pver) ) {
                    throw new Exception("Deserialization failed. Verification failed.");
                }
                pfrag += 8;
                pver += 8;
                nextSize -= 2;
                if (nextSize<0 || (*(UInt16*)pfrag) != (*(UInt16*)pver) ) {
                    throw new Exception("Deserialization failed. Verification failed.");
                }
                pfrag += 2;
                pver += 2;
                // Skip until start of value to parse.
                while (*pfrag != ':') {
                    pfrag++;
                    nextSize--;
                    if (nextSize < 0)
                         throw new Exception("Deserialization failed.");
                }
                pfrag++; // Skip ':' or ','
                nextSize--;
                if (nextSize < 0)
                    throw new Exception("Deserialization failed.");
                while (*pfrag == ' ' || *pfrag == '\n' || *pfrag == '\r') {
                    pfrag++;
                    nextSize--;
                    if (nextSize < 0)
                         throw new Exception("Deserialization failed.");
                }
                Boolean val0;
                if (JsonHelper.ParseBoolean((IntPtr)pfrag, nextSize, out val0, out valueSize)) {
                    app.IsInnerApp = val0;
                    nextSize -= valueSize;
                    if (nextSize < 0) {
                        throw new Exception("Unable to deserialize App. Unexpected end of content");
                    }
                    pfrag += valueSize;
                } else {
                    throw new Exception("Unable to deserialize App. Content not compatible.");
                }
            }
        }
        throw new Exception("Deserialization of App failed.");
    }
}

    public static class ASubApp2AppJsonSerializer{

    #pragma warning disable 0414
    private static int VerificationOffset0 = 0; // IsInnerApp
    private static int VerificationOffset1 = 11; // ASubList
    #pragma warning restore 0414
    private static byte[] VerificationBytes = new byte[] {(byte)'I',(byte)'s',(byte)'I',(byte)'n',(byte)'n',(byte)'e',(byte)'r',(byte)'A',(byte)'p',(byte)'p',(byte)' ',(byte)'A',(byte)'S',(byte)'u',(byte)'b',(byte)'L',(byte)'i',(byte)'s',(byte)'t',(byte)' '};
    private static IntPtr PointerVerificationBytes;

    static ASubApp2AppJsonSerializer() {
        PointerVerificationBytes = BitsAndBytes.Alloc(VerificationBytes.Length); // TODO. Free when program exists
        BitsAndBytes.SlowMemCopy( PointerVerificationBytes, VerificationBytes, (uint)VerificationBytes.Length);
    }

    public static int Serialize(IntPtr buffer, int bufferSize, ChildApp.ASubApp2App app) {
        byte[] tmpArr = new byte[1024];
        int valSize;
        unsafe {
            byte* pfrag = (byte*)buffer;
            byte* pver = null;
            int nextSize = bufferSize;
            if ((nextSize - 2) < 0)
                throw new Exception("Buffer too small.");
            nextSize -= 2;
            *pfrag++ = (byte)'{';
            valSize = JsonHelper.WriteString((IntPtr)pfrag, nextSize, "IsInnerApp", tmpArr);
            if (valSize == -1)
                throw new Exception("Buffer too small.");
            nextSize -= valSize;
            pfrag += valSize;
            nextSize--;
            if (nextSize < 0)
                throw new Exception("Buffer too small.");
            *pfrag++ = (byte)':';
            valSize = JsonHelper.WriteBool((IntPtr)pfrag, nextSize, app.IsInnerApp);
            if (valSize == -1)
                throw new Exception("Buffer too small.");
            nextSize -= valSize;
            pfrag += valSize;
            nextSize--;
            if (nextSize < 0)
                throw new Exception("Buffer too small.");
            *pfrag++ = (byte)',';
            valSize = JsonHelper.WriteString((IntPtr)pfrag, nextSize, "ASubList", tmpArr);
            if (valSize == -1)
                throw new Exception("Buffer too small.");
            nextSize -= valSize;
            pfrag += valSize;
            nextSize--;
            if (nextSize < 0)
                throw new Exception("Buffer too small.");
            *pfrag++ = (byte)':';
            if ((nextSize - 2) < 0)
                throw new Exception("Buffer too small.");
            *pfrag++ = (byte)'[';
            nextSize -= 2;
            for(int i = 0; i < app.ASubList.Count; i++) {
                var listApp = app.ASubList[i];
                valSize = ASubListAppJsonSerializer.Serialize((IntPtr)pfrag, nextSize, listApp);
                if (valSize == -1)
                    throw new Exception("Buffer too small.");
                nextSize -= valSize;
                pfrag += valSize;
                if ((i+1) < app.ASubList.Count) {
                    nextSize--;
                    if (nextSize < 0)
                        throw new Exception("Buffer too small.");
                    *pfrag++ = (byte)',';
                }
            }
            *pfrag++ = (byte)']';
            *pfrag++ = (byte)'}';
            return (bufferSize - nextSize);
        }
    }
    public static ChildApp.ASubApp2App Deserialize(IntPtr buffer, int bufferSize, out int usedSize) {
        int valueSize;
        ChildApp.ASubApp2App app = new ChildApp.ASubApp2App();
        unsafe {
            byte* pfrag = (byte*)buffer;
            byte* pver = null;
            int nextSize = bufferSize;
            while (nextSize > 0) {
                // Skip until start of next property or end of current object.
                while (true) {
                    if (*pfrag == '"')
                        break;
                    if (*pfrag == '}') {
                        pfrag++;
                        nextSize--;
                        usedSize = bufferSize - nextSize;
                        return app;
                    }
                    pfrag++;
                    nextSize--;
                    if (nextSize < 0)
                         throw new Exception("Deserialization failed.");
                }
                pfrag++;
                nextSize--;
                if (nextSize < 0)
                    throw new Exception("Deserialization failed.");
                switch (*pfrag) {
                    case (byte)'I':
                        pfrag++;
                        nextSize--;
                        pver = ((byte*)PointerVerificationBytes + VerificationOffset0 + 1);
                        nextSize -= 8;
                        if (nextSize<0 || (*(UInt64*)pfrag) != (*(UInt64*)pver) ) {
                            throw new Exception("Deserialization failed. Verification failed.");
                        }
                        pfrag += 8;
                        pver += 8;
                        nextSize --;
                        if (nextSize<0 || (*pfrag) != (*pver) ) {
                            throw new Exception("Deserialization failed. Verification failed.");
                        }
                        pfrag++;
                        pver++;
                        // Skip until start of value to parse.
                        while (*pfrag != ':') {
                            pfrag++;
                            nextSize--;
                            if (nextSize < 0)
                                 throw new Exception("Deserialization failed.");
                        }
                        pfrag++; // Skip ':' or ','
                        nextSize--;
                        if (nextSize < 0)
                            throw new Exception("Deserialization failed.");
                        while (*pfrag == ' ' || *pfrag == '\n' || *pfrag == '\r') {
                            pfrag++;
                            nextSize--;
                            if (nextSize < 0)
                                 throw new Exception("Deserialization failed.");
                        }
                        Boolean val0;
                        if (JsonHelper.ParseBoolean((IntPtr)pfrag, nextSize, out val0, out valueSize)) {
                            app.IsInnerApp = val0;
                            nextSize -= valueSize;
                            if (nextSize < 0) {
                                throw new Exception("Unable to deserialize App. Unexpected end of content");
                            }
                            pfrag += valueSize;
                        } else {
                            throw new Exception("Unable to deserialize App. Content not compatible.");
                        }
                       break;
                    case (byte)'A':
                        pfrag++;
                        nextSize--;
                        pver = ((byte*)PointerVerificationBytes + VerificationOffset1 + 1);
                        nextSize -= 4;
                        if (nextSize<0 || (*(UInt32*)pfrag) !=  (*(UInt32*)pver) ) {
                            throw new Exception("Deserialization failed. Verification failed.");
                        }
                        pfrag += 4;
                        pver += 4;
                        nextSize -= 2;
                        if (nextSize<0 || (*(UInt16*)pfrag) != (*(UInt16*)pver) ) {
                            throw new Exception("Deserialization failed. Verification failed.");
                        }
                        pfrag += 2;
                        pver += 2;
                        nextSize --;
                        if (nextSize<0 || (*pfrag) != (*pver) ) {
                            throw new Exception("Deserialization failed. Verification failed.");
                        }
                        pfrag++;
                        pver++;
                        // Skip until start of value to parse.
                        while (*pfrag != ':') {
                            pfrag++;
                            nextSize--;
                            if (nextSize < 0)
                                 throw new Exception("Deserialization failed.");
                        }
                        pfrag++; // Skip ':' or ','
                        nextSize--;
                        if (nextSize < 0)
                            throw new Exception("Deserialization failed.");
                        while (*pfrag == ' ' || *pfrag == '\n' || *pfrag == '\r') {
                            pfrag++;
                            nextSize--;
                            if (nextSize < 0)
                                 throw new Exception("Deserialization failed.");
                        }
                        if (*pfrag++ == '[') {
                            nextSize--;
                            while (*pfrag != '{' && *pfrag != ']') { // find first object or end of array
                                pfrag++;
                                nextSize--;
                            }
                            if (*pfrag != ']') {
                            while (nextSize > 0) {
                                var val1 = ASubListAppJsonSerializer.Deserialize((IntPtr)pfrag, nextSize, out valueSize);
                                    app.ASubList.Add(val1);
                                    nextSize -= valueSize;
                                    if (nextSize < 0) {
                                        throw new Exception("Unable to deserialize App. Unexpected end of content");
                                    }
                                    pfrag += valueSize;
                                    // Skip until start of value to parse.
                                    while (*pfrag != ',') {
                                        if (*pfrag == ']')
                                            break;
                                        pfrag++;
                                        nextSize--;
                                        if (nextSize < 0)
                                             throw new Exception("Deserialization failed.");
                                    }
                                    if (*pfrag == ']')
                                        break;
                                    pfrag++; // Skip ':' or ','
                                    nextSize--;
                                    if (nextSize < 0)
                                        throw new Exception("Deserialization failed.");
                                    while (*pfrag == ' ' || *pfrag == '\n' || *pfrag == '\r') {
                                        pfrag++;
                                        nextSize--;
                                        if (nextSize < 0)
                                             throw new Exception("Deserialization failed.");
                                    }
                            }
                            }
                        } else
                            throw new Exception("Invalid array value");
                       break;
                    default:
                        throw new Exception("Property not belonging to this app found in content.");
                }
            }
        }
        throw new Exception("Deserialization of App failed.");
    }
}

    public static class ASubListAppJsonSerializer{

    #pragma warning disable 0414
    private static int VerificationOffset0 = 0; // Huh
    #pragma warning restore 0414
    private static byte[] VerificationBytes = new byte[] {(byte)'H',(byte)'u',(byte)'h',(byte)' '};
    private static IntPtr PointerVerificationBytes;

    static ASubListAppJsonSerializer() {
        PointerVerificationBytes = BitsAndBytes.Alloc(VerificationBytes.Length); // TODO. Free when program exists
        BitsAndBytes.SlowMemCopy( PointerVerificationBytes, VerificationBytes, (uint)VerificationBytes.Length);
    }

    public static int Serialize(IntPtr buffer, int bufferSize, ChildApp.ASubApp2App.ASubListApp app) {
        byte[] tmpArr = new byte[1024];
        int valSize;
        unsafe {
            byte* pfrag = (byte*)buffer;
            byte* pver = null;
            int nextSize = bufferSize;
            if ((nextSize - 2) < 0)
                throw new Exception("Buffer too small.");
            nextSize -= 2;
            *pfrag++ = (byte)'{';
            valSize = JsonHelper.WriteString((IntPtr)pfrag, nextSize, "Huh", tmpArr);
            if (valSize == -1)
                throw new Exception("Buffer too small.");
            nextSize -= valSize;
            pfrag += valSize;
            nextSize--;
            if (nextSize < 0)
                throw new Exception("Buffer too small.");
            *pfrag++ = (byte)':';
            valSize = JsonHelper.WriteString((IntPtr)pfrag, nextSize, app.Huh, tmpArr);
            if (valSize == -1)
                throw new Exception("Buffer too small.");
            nextSize -= valSize;
            pfrag += valSize;
            *pfrag++ = (byte)'}';
            return (bufferSize - nextSize);
        }
    }
    public static ChildApp.ASubApp2App.ASubListApp Deserialize(IntPtr buffer, int bufferSize, out int usedSize) {
        int valueSize;
        ChildApp.ASubApp2App.ASubListApp app = new ChildApp.ASubApp2App.ASubListApp();
        unsafe {
            byte* pfrag = (byte*)buffer;
            byte* pver = null;
            int nextSize = bufferSize;
            while (nextSize > 0) {
                // Skip until start of next property or end of current object.
                while (true) {
                    if (*pfrag == '"')
                        break;
                    if (*pfrag == '}') {
                        pfrag++;
                        nextSize--;
                        usedSize = bufferSize - nextSize;
                        return app;
                    }
                    pfrag++;
                    nextSize--;
                    if (nextSize < 0)
                         throw new Exception("Deserialization failed.");
                }
                pfrag++;
                nextSize--;
                if (nextSize < 0)
                    throw new Exception("Deserialization failed.");
                pver = ((byte*)PointerVerificationBytes + VerificationOffset0 + 0);
                nextSize -= 2;
                if (nextSize<0 || (*(UInt16*)pfrag) != (*(UInt16*)pver) ) {
                    throw new Exception("Deserialization failed. Verification failed.");
                }
                pfrag += 2;
                pver += 2;
                nextSize --;
                if (nextSize<0 || (*pfrag) != (*pver) ) {
                    throw new Exception("Deserialization failed. Verification failed.");
                }
                pfrag++;
                pver++;
                // Skip until start of value to parse.
                while (*pfrag != ':') {
                    pfrag++;
                    nextSize--;
                    if (nextSize < 0)
                         throw new Exception("Deserialization failed.");
                }
                pfrag++; // Skip ':' or ','
                nextSize--;
                if (nextSize < 0)
                    throw new Exception("Deserialization failed.");
                while (*pfrag == ' ' || *pfrag == '\n' || *pfrag == '\r') {
                    pfrag++;
                    nextSize--;
                    if (nextSize < 0)
                         throw new Exception("Deserialization failed.");
                }
                String val0;
                if (JsonHelper.ParseString((IntPtr)pfrag, nextSize, out val0, out valueSize)) {
                    app.Huh = val0;
                    nextSize -= valueSize;
                    if (nextSize < 0) {
                        throw new Exception("Unable to deserialize App. Unexpected end of content");
                    }
                    pfrag += valueSize;
                } else {
                    throw new Exception("Unable to deserialize App. Content not compatible.");
                }
            }
        }
        throw new Exception("Deserialization of App failed.");
    }
}

    public static class AListAppJsonSerializer{

    #pragma warning disable 0414
    private static int VerificationOffset0 = 0; // AValue
    private static int VerificationOffset1 = 7; // ANumber
    #pragma warning restore 0414
    private static byte[] VerificationBytes = new byte[] {(byte)'A',(byte)'V',(byte)'a',(byte)'l',(byte)'u',(byte)'e',(byte)' ',(byte)'A',(byte)'N',(byte)'u',(byte)'m',(byte)'b',(byte)'e',(byte)'r',(byte)' '};
    private static IntPtr PointerVerificationBytes;

    static AListAppJsonSerializer() {
        PointerVerificationBytes = BitsAndBytes.Alloc(VerificationBytes.Length); // TODO. Free when program exists
        BitsAndBytes.SlowMemCopy( PointerVerificationBytes, VerificationBytes, (uint)VerificationBytes.Length);
    }

    public static int Serialize(IntPtr buffer, int bufferSize, AListApp app) {
        byte[] tmpArr = new byte[1024];
        int valSize;
        unsafe {
            byte* pfrag = (byte*)buffer;
            byte* pver = null;
            int nextSize = bufferSize;
            if ((nextSize - 2) < 0)
                throw new Exception("Buffer too small.");
            nextSize -= 2;
            *pfrag++ = (byte)'{';
            valSize = JsonHelper.WriteString((IntPtr)pfrag, nextSize, "AValue", tmpArr);
            if (valSize == -1)
                throw new Exception("Buffer too small.");
            nextSize -= valSize;
            pfrag += valSize;
            nextSize--;
            if (nextSize < 0)
                throw new Exception("Buffer too small.");
            *pfrag++ = (byte)':';
            valSize = JsonHelper.WriteString((IntPtr)pfrag, nextSize, app.AValue, tmpArr);
            if (valSize == -1)
                throw new Exception("Buffer too small.");
            nextSize -= valSize;
            pfrag += valSize;
            nextSize--;
            if (nextSize < 0)
                throw new Exception("Buffer too small.");
            *pfrag++ = (byte)',';
            valSize = JsonHelper.WriteString((IntPtr)pfrag, nextSize, "ANumber", tmpArr);
            if (valSize == -1)
                throw new Exception("Buffer too small.");
            nextSize -= valSize;
            pfrag += valSize;
            nextSize--;
            if (nextSize < 0)
                throw new Exception("Buffer too small.");
            *pfrag++ = (byte)':';
            valSize = JsonHelper.WriteInt((IntPtr)pfrag, nextSize, app.ANumber);
            if (valSize == -1)
                throw new Exception("Buffer too small.");
            nextSize -= valSize;
            pfrag += valSize;
            *pfrag++ = (byte)'}';
            return (bufferSize - nextSize);
        }
    }
    public static AListApp Deserialize(IntPtr buffer, int bufferSize, out int usedSize) {
        int valueSize;
        AListApp app = new AListApp();
        unsafe {
            byte* pfrag = (byte*)buffer;
            byte* pver = null;
            int nextSize = bufferSize;
            while (nextSize > 0) {
                // Skip until start of next property or end of current object.
                while (true) {
                    if (*pfrag == '"')
                        break;
                    if (*pfrag == '}') {
                        pfrag++;
                        nextSize--;
                        usedSize = bufferSize - nextSize;
                        return app;
                    }
                    pfrag++;
                    nextSize--;
                    if (nextSize < 0)
                         throw new Exception("Deserialization failed.");
                }
                pfrag++;
                nextSize--;
                if (nextSize < 0)
                    throw new Exception("Deserialization failed.");
                pver = ((byte*)PointerVerificationBytes + VerificationOffset0 + 0);
                nextSize --;
                if (nextSize<0 || (*pfrag) != (*pver) ) {
                    throw new Exception("Deserialization failed. Verification failed.");
                }
                pfrag++;
                pver++;
                switch (*pfrag) {
                    case (byte)'V':
                        pfrag++;
                        nextSize--;
                        pver = ((byte*)PointerVerificationBytes + VerificationOffset0 + 2);
                        nextSize -= 4;
                        if (nextSize<0 || (*(UInt32*)pfrag) !=  (*(UInt32*)pver) ) {
                            throw new Exception("Deserialization failed. Verification failed.");
                        }
                        pfrag += 4;
                        pver += 4;
                        // Skip until start of value to parse.
                        while (*pfrag != ':') {
                            pfrag++;
                            nextSize--;
                            if (nextSize < 0)
                                 throw new Exception("Deserialization failed.");
                        }
                        pfrag++; // Skip ':' or ','
                        nextSize--;
                        if (nextSize < 0)
                            throw new Exception("Deserialization failed.");
                        while (*pfrag == ' ' || *pfrag == '\n' || *pfrag == '\r') {
                            pfrag++;
                            nextSize--;
                            if (nextSize < 0)
                                 throw new Exception("Deserialization failed.");
                        }
                        String val0;
                        if (JsonHelper.ParseString((IntPtr)pfrag, nextSize, out val0, out valueSize)) {
                            app.AValue = val0;
                            nextSize -= valueSize;
                            if (nextSize < 0) {
                                throw new Exception("Unable to deserialize App. Unexpected end of content");
                            }
                            pfrag += valueSize;
                        } else {
                            throw new Exception("Unable to deserialize App. Content not compatible.");
                        }
                       break;
                    case (byte)'N':
                        pfrag++;
                        nextSize--;
                        pver = ((byte*)PointerVerificationBytes + VerificationOffset1 + 2);
                        nextSize -= 4;
                        if (nextSize<0 || (*(UInt32*)pfrag) !=  (*(UInt32*)pver) ) {
                            throw new Exception("Deserialization failed. Verification failed.");
                        }
                        pfrag += 4;
                        pver += 4;
                        nextSize --;
                        if (nextSize<0 || (*pfrag) != (*pver) ) {
                            throw new Exception("Deserialization failed. Verification failed.");
                        }
                        pfrag++;
                        pver++;
                        // Skip until start of value to parse.
                        while (*pfrag != ':') {
                            pfrag++;
                            nextSize--;
                            if (nextSize < 0)
                                 throw new Exception("Deserialization failed.");
                        }
                        pfrag++; // Skip ':' or ','
                        nextSize--;
                        if (nextSize < 0)
                            throw new Exception("Deserialization failed.");
                        while (*pfrag == ' ' || *pfrag == '\n' || *pfrag == '\r') {
                            pfrag++;
                            nextSize--;
                            if (nextSize < 0)
                                 throw new Exception("Deserialization failed.");
                        }
                        Int64 val1;
                        if (JsonHelper.ParseInt((IntPtr)pfrag, nextSize, out val1, out valueSize)) {
                            app.ANumber = val1;
                            nextSize -= valueSize;
                            if (nextSize < 0) {
                                throw new Exception("Unable to deserialize App. Unexpected end of content");
                            }
                            pfrag += valueSize;
                        } else {
                            throw new Exception("Unable to deserialize App. Content not compatible.");
                        }
                       break;
                    default:
                        throw new Exception("Property not belonging to this app found in content.");
                }
            }
        }
        throw new Exception("Deserialization of App failed.");
    }
}

}
}
