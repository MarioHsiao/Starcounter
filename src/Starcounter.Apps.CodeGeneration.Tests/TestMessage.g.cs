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
    public int UserId { get { return GetValue(Template.UserId); } set { SetValue(Template.UserId, value); } }
    public String Username { get { return GetValue(Template.Username); } set { SetValue(Template.Username, value); } }
    public String Password { get { return GetValue(Template.Password); } set { SetValue(Template.Password, value); } }
    public TestMessage.ChildApp Child { get { return GetValue<TestMessage.ChildApp>(Template.Child); } set { SetValue(Template.Child, value); } }
    public Listing<TestMessage.AListApp> AList { get { return GetValue<TestMessage.AListApp>(Template.AList); } set { SetValue<TestMessage.AListApp>(Template.AList, value); } }
    public String UserLink { get { return GetValue(Template.UserLink); } set { SetValue(Template.UserLink, value); } }
    public Action User { get { return GetValue(Template.User); } set { SetValue(Template.User, value); } }
    public class ChildApp : App {
        public static ChildAppTemplate DefaultTemplate = new ChildAppTemplate();
        public ChildApp() { Template = DefaultTemplate; }
        public ChildApp(ChildAppTemplate template) { Template = template; }
        public new ChildAppTemplate Template { get { return (ChildAppTemplate)base.Template; } set { base.Template = value; } }
        public new ChildAppMetadata Metadata { get { return (ChildAppMetadata)base.Metadata; } }
        public new TestMessage Parent { get { return (TestMessage)base.Parent; } set { base.Parent = value; } }
        public String ChildName { get { return GetValue(Template.ChildName); } set { SetValue(Template.ChildName, value); } }
        public Action Button { get { return GetValue(Template.Button); } set { SetValue(Template.Button, value); } }
        public TestMessage.ChildApp.ASubAppApp ASubApp { get { return GetValue<TestMessage.ChildApp.ASubAppApp>(Template.ASubApp); } set { SetValue(Template.ASubApp, value); } }
        public TestMessage.ChildApp.ASubApp2App ASubApp2 { get { return GetValue<TestMessage.ChildApp.ASubApp2App>(Template.ASubApp2); } set { SetValue(Template.ASubApp2, value); } }
        public class ASubAppApp : App {
            public static ASubAppAppTemplate DefaultTemplate = new ASubAppAppTemplate();
            public ASubAppApp() { Template = DefaultTemplate; }
            public ASubAppApp(ASubAppAppTemplate template) { Template = template; }
            public new ASubAppAppTemplate Template { get { return (ASubAppAppTemplate)base.Template; } set { base.Template = value; } }
            public new ASubAppAppMetadata Metadata { get { return (ASubAppAppMetadata)base.Metadata; } }
            public new ChildApp Parent { get { return (ChildApp)base.Parent; } set { base.Parent = value; } }
            public bool IsInnerApp { get { return GetValue(Template.IsInnerApp); } set { SetValue(Template.IsInnerApp, value); } }
            public class ASubAppAppTemplate : AppTemplate {
                public ASubAppAppTemplate()
                    : base() {
                    InstanceType = typeof(TestMessage.ChildApp.ASubAppApp);
                    ClassName = "ASubAppApp";
                    IsInnerApp = Register<BoolProperty>("IsInnerApp", "IsInnerApp");
                }
                public override object CreateInstance(AppNode parent) { return new ASubAppApp(this) { Parent = (TestMessage.ChildApp)parent }; }
                public BoolProperty IsInnerApp;
            }
            public class ASubAppAppMetadata : AppMetadata {
                public ASubAppAppMetadata(App app, AppTemplate template) : base(app, template) { }
                public new TestMessage.ChildApp.ASubAppApp App { get { return (TestMessage.ChildApp.ASubAppApp)base.App; } }
                public new TestMessage.ChildApp.ASubAppApp.ASubAppAppTemplate Template { get { return (TestMessage.ChildApp.ASubAppApp.ASubAppAppTemplate)base.Template; } }
                public BoolMetadata IsInnerApp { get { return __p_IsInnerApp ?? (__p_IsInnerApp = new BoolMetadata(App, App.Template.IsInnerApp)); } }
                private BoolMetadata __p_IsInnerApp;
            }
        }
        public class ASubApp2App : App {
            public static ASubApp2AppTemplate DefaultTemplate = new ASubApp2AppTemplate();
            public ASubApp2App() { Template = DefaultTemplate; }
            public ASubApp2App(ASubApp2AppTemplate template) { Template = template; }
            public new ASubApp2AppTemplate Template { get { return (ASubApp2AppTemplate)base.Template; } set { base.Template = value; } }
            public new ASubApp2AppMetadata Metadata { get { return (ASubApp2AppMetadata)base.Metadata; } }
            public new ChildApp Parent { get { return (ChildApp)base.Parent; } set { base.Parent = value; } }
            public bool IsInnerApp { get { return GetValue(Template.IsInnerApp); } set { SetValue(Template.IsInnerApp, value); } }
            public Listing<TestMessage.ChildApp.ASubApp2App.ASubListApp> ASubList { get { return GetValue<TestMessage.ChildApp.ASubApp2App.ASubListApp>(Template.ASubList); } set { SetValue<TestMessage.ChildApp.ASubApp2App.ASubListApp>(Template.ASubList, value); } }
            public class ASubListApp : App {
                public static ASubListAppTemplate DefaultTemplate = new ASubListAppTemplate();
                public ASubListApp() { Template = DefaultTemplate; }
                public ASubListApp(ASubListAppTemplate template) { Template = template; }
                public new ASubListAppTemplate Template { get { return (ASubListAppTemplate)base.Template; } set { base.Template = value; } }
                public new ASubListAppMetadata Metadata { get { return (ASubListAppMetadata)base.Metadata; } }
                public new Listing<TestMessage.ChildApp.ASubApp2App.ASubListApp> Parent { get { return (Listing<TestMessage.ChildApp.ASubApp2App.ASubListApp>)base.Parent; } set { base.Parent = value; } }
                public String Huh { get { return GetValue(Template.Huh); } set { SetValue(Template.Huh, value); } }
                public class ASubListAppTemplate : AppTemplate {
                    public ASubListAppTemplate()
                        : base() {
                        InstanceType = typeof(TestMessage.ChildApp.ASubApp2App.ASubListApp);
                        ClassName = "ASubListApp";
                        Huh = Register<StringProperty>("Huh", "Huh");
                    }
                    public override object CreateInstance(AppNode parent) { return new ASubListApp(this) { Parent = (Listing<TestMessage.ChildApp.ASubApp2App.ASubListApp>)parent }; }
                    public StringProperty Huh;
                }
                public class ASubListAppMetadata : AppMetadata {
                    public ASubListAppMetadata(App app, AppTemplate template) : base(app, template) { }
                    public new TestMessage.ChildApp.ASubApp2App.ASubListApp App { get { return (TestMessage.ChildApp.ASubApp2App.ASubListApp)base.App; } }
                    public new TestMessage.ChildApp.ASubApp2App.ASubListApp.ASubListAppTemplate Template { get { return (TestMessage.ChildApp.ASubApp2App.ASubListApp.ASubListAppTemplate)base.Template; } }
                    public StringMetadata Huh { get { return __p_Huh ?? (__p_Huh = new StringMetadata(App, App.Template.Huh)); } }
                    private StringMetadata __p_Huh;
                }
            }
            public class ASubApp2AppTemplate : AppTemplate {
                public ASubApp2AppTemplate()
                    : base() {
                    InstanceType = typeof(TestMessage.ChildApp.ASubApp2App);
                    ClassName = "ASubApp2App";
                    IsInnerApp = Register<BoolProperty>("IsInnerApp", "IsInnerApp");
                    ASubList = Register<ListingProperty<TestMessage.ChildApp.ASubApp2App.ASubListApp, TestMessage.ChildApp.ASubApp2App.ASubListApp.ASubListAppTemplate>>("ASubList", "ASubList");
                    ASubList.App = TestMessage.ChildApp.ASubApp2App.ASubListApp.DefaultTemplate;
                }
                public override object CreateInstance(AppNode parent) { return new ASubApp2App(this) { Parent = (TestMessage.ChildApp)parent }; }
                public BoolProperty IsInnerApp;
                public ListingProperty<TestMessage.ChildApp.ASubApp2App.ASubListApp, TestMessage.ChildApp.ASubApp2App.ASubListApp.ASubListAppTemplate> ASubList;
            }
            public class ASubApp2AppMetadata : AppMetadata {
                public ASubApp2AppMetadata(App app, AppTemplate template) : base(app, template) { }
                public new TestMessage.ChildApp.ASubApp2App App { get { return (TestMessage.ChildApp.ASubApp2App)base.App; } }
                public new TestMessage.ChildApp.ASubApp2App.ASubApp2AppTemplate Template { get { return (TestMessage.ChildApp.ASubApp2App.ASubApp2AppTemplate)base.Template; } }
                public BoolMetadata IsInnerApp { get { return __p_IsInnerApp ?? (__p_IsInnerApp = new BoolMetadata(App, App.Template.IsInnerApp)); } }
                private BoolMetadata __p_IsInnerApp;
                public ListingMetadata<TestMessage.ChildApp.ASubApp2App.ASubListApp, TestMessage.ChildApp.ASubApp2App.ASubListApp.ASubListAppTemplate> ASubList { get { return __p_ASubList ?? (__p_ASubList = new ListingMetadata<TestMessage.ChildApp.ASubApp2App.ASubListApp, TestMessage.ChildApp.ASubApp2App.ASubListApp.ASubListAppTemplate>(App, App.Template.ASubList)); } }
                private ListingMetadata<TestMessage.ChildApp.ASubApp2App.ASubListApp, TestMessage.ChildApp.ASubApp2App.ASubListApp.ASubListAppTemplate> __p_ASubList;
            }
        }
        public class ChildAppTemplate : AppTemplate {
            public ChildAppTemplate()
                : base() {
                InstanceType = typeof(TestMessage.ChildApp);
                ClassName = "ChildApp";
                ChildName = Register<StringProperty>("ChildName", "ChildName");
                Button = Register<ActionProperty>("Button", "Button");
                ASubApp = Register<TestMessage.ChildApp.ASubAppApp.ASubAppAppTemplate>("ASubApp", "ASubApp");
                ASubApp2 = Register<TestMessage.ChildApp.ASubApp2App.ASubApp2AppTemplate>("ASubApp2", "ASubApp2");
            }
            public override object CreateInstance(AppNode parent) { return new ChildApp(this) { Parent = (TestMessage)parent }; }
            public StringProperty ChildName;
            public ActionProperty Button;
            public TestMessage.ChildApp.ASubAppApp.ASubAppAppTemplate ASubApp;
            public TestMessage.ChildApp.ASubApp2App.ASubApp2AppTemplate ASubApp2;
        }
        public class ChildAppMetadata : AppMetadata {
            public ChildAppMetadata(App app, AppTemplate template) : base(app, template) { }
            public new TestMessage.ChildApp App { get { return (TestMessage.ChildApp)base.App; } }
            public new TestMessage.ChildApp.ChildAppTemplate Template { get { return (TestMessage.ChildApp.ChildAppTemplate)base.Template; } }
            public StringMetadata ChildName { get { return __p_ChildName ?? (__p_ChildName = new StringMetadata(App, App.Template.ChildName)); } }
            private StringMetadata __p_ChildName;
            public ActionMetadata Button { get { return __p_Button ?? (__p_Button = new ActionMetadata(App, App.Template.Button)); } }
            private ActionMetadata __p_Button;
            public TestMessage.ChildApp.ASubAppApp.ASubAppAppMetadata ASubApp { get { return __p_ASubApp ?? (__p_ASubApp = new TestMessage.ChildApp.ASubAppApp.ASubAppAppMetadata(App, App.Template.ASubApp)); } }
            private TestMessage.ChildApp.ASubAppApp.ASubAppAppMetadata __p_ASubApp;
            public TestMessage.ChildApp.ASubApp2App.ASubApp2AppMetadata ASubApp2 { get { return __p_ASubApp2 ?? (__p_ASubApp2 = new TestMessage.ChildApp.ASubApp2App.ASubApp2AppMetadata(App, App.Template.ASubApp2)); } }
            private TestMessage.ChildApp.ASubApp2App.ASubApp2AppMetadata __p_ASubApp2;
        }
    }
    public class AListApp : App {
        public static AListAppTemplate DefaultTemplate = new AListAppTemplate();
        public AListApp() { Template = DefaultTemplate; }
        public AListApp(AListAppTemplate template) { Template = template; }
        public new AListAppTemplate Template { get { return (AListAppTemplate)base.Template; } set { base.Template = value; } }
        public new AListAppMetadata Metadata { get { return (AListAppMetadata)base.Metadata; } }
        public new Listing<TestMessage.AListApp> Parent { get { return (Listing<TestMessage.AListApp>)base.Parent; } set { base.Parent = value; } }
        public String AValue { get { return GetValue(Template.AValue); } set { SetValue(Template.AValue, value); } }
        public int ANumber { get { return GetValue(Template.ANumber); } set { SetValue(Template.ANumber, value); } }
        public class AListAppTemplate : AppTemplate {
            public AListAppTemplate()
                : base() {
                InstanceType = typeof(TestMessage.AListApp);
                ClassName = "AListApp";
                AValue = Register<StringProperty>("AValue", "AValue");
                ANumber = Register<IntProperty>("ANumber", "ANumber");
            }
            public override object CreateInstance(AppNode parent) { return new AListApp(this) { Parent = (Listing<TestMessage.AListApp>)parent }; }
            public StringProperty AValue;
            public IntProperty ANumber;
        }
        public class AListAppMetadata : AppMetadata {
            public AListAppMetadata(App app, AppTemplate template) : base(app, template) { }
            public new TestMessage.AListApp App { get { return (TestMessage.AListApp)base.App; } }
            public new TestMessage.AListApp.AListAppTemplate Template { get { return (TestMessage.AListApp.AListAppTemplate)base.Template; } }
            public StringMetadata AValue { get { return __p_AValue ?? (__p_AValue = new StringMetadata(App, App.Template.AValue)); } }
            private StringMetadata __p_AValue;
            public IntMetadata ANumber { get { return __p_ANumber ?? (__p_ANumber = new IntMetadata(App, App.Template.ANumber)); } }
            private IntMetadata __p_ANumber;
        }
    }
    public class TestMessageTemplate : AppTemplate {
        public TestMessageTemplate()
            : base() {
            InstanceType = typeof(TestMessage);
            ClassName = "TestMessage";
            UserId = Register<IntProperty>("UserId$", "UserId", Editable = true);
            Username = Register<StringProperty>("Username", "Username");
            Password = Register<StringProperty>("Password", "Password");
            Child = Register<TestMessage.ChildApp.ChildAppTemplate>("Child", "Child");
            AList = Register<ListingProperty<TestMessage.AListApp, TestMessage.AListApp.AListAppTemplate>>("AList", "AList");
            AList.App = TestMessage.AListApp.DefaultTemplate;
            UserLink = Register<StringProperty>("UserLink", "UserLink");
            User = Register<ActionProperty>("User", "User");
        }
        public override object CreateInstance(AppNode parent) { return new TestMessage(this) { Parent = parent }; }
        public IntProperty UserId;
        public StringProperty Username;
        public StringProperty Password;
        public TestMessage.ChildApp.ChildAppTemplate Child;
        public ListingProperty<TestMessage.AListApp, TestMessage.AListApp.AListAppTemplate> AList;
        public StringProperty UserLink;
        public ActionProperty User;
    }
    public class TestMessageMetadata : AppMetadata {
        public TestMessageMetadata(App app, AppTemplate template) : base(app, template) { }
        public new TestMessage App { get { return (TestMessage)base.App; } }
        public new TestMessage.TestMessageTemplate Template { get { return (TestMessage.TestMessageTemplate)base.Template; } }
        public IntMetadata UserId { get { return __p_UserId ?? (__p_UserId = new IntMetadata(App, App.Template.UserId)); } }
        private IntMetadata __p_UserId;
        public StringMetadata Username { get { return __p_Username ?? (__p_Username = new StringMetadata(App, App.Template.Username)); } }
        private StringMetadata __p_Username;
        public StringMetadata Password { get { return __p_Password ?? (__p_Password = new StringMetadata(App, App.Template.Password)); } }
        private StringMetadata __p_Password;
        public TestMessage.ChildApp.ChildAppMetadata Child { get { return __p_Child ?? (__p_Child = new TestMessage.ChildApp.ChildAppMetadata(App, App.Template.Child)); } }
        private TestMessage.ChildApp.ChildAppMetadata __p_Child;
        public ListingMetadata<TestMessage.AListApp, TestMessage.AListApp.AListAppTemplate> AList { get { return __p_AList ?? (__p_AList = new ListingMetadata<TestMessage.AListApp, TestMessage.AListApp.AListAppTemplate>(App, App.Template.AList)); } }
        private ListingMetadata<TestMessage.AListApp, TestMessage.AListApp.AListAppTemplate> __p_AList;
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
        public class UserId : Input<TestMessage, IntProperty, int> {
        }
        public class Username : Input<TestMessage, StringProperty, String> {
        }
        public class Password : Input<TestMessage, StringProperty, String> {
        }
        public static class Child {
            public class ChildName : Input<TestMessage.ChildApp, StringProperty, String> {
            }
            public class Button : Input<TestMessage.ChildApp, ActionProperty, Action> {
            }
            public static class ASubApp {
                public class IsInnerApp : Input<TestMessage.ChildApp.ASubAppApp, BoolProperty, bool> {
                }
            }
            public static class ASubApp2 {
                public class IsInnerApp : Input<TestMessage.ChildApp.ASubApp2App, BoolProperty, bool> {
                }
                public static class ASubList {
                    public class Huh : Input<TestMessage.ChildApp.ASubApp2App.ASubListApp, StringProperty, String> {
                    }
                }
            }
        }
        public static class AList {
            public class AValue : Input<TestMessage.AListApp, StringProperty, String> {
            }
            public class ANumber : Input<TestMessage.AListApp, IntProperty, int> {
            }
        }
        public class UserLink : Input<TestMessage, StringProperty, String> {
        }
        public class User : Input<TestMessage, ActionProperty, Action> {
        }
    }
    public static class TestMessageJsonSerializer{

    #pragma warning disable 0414
    private static int VerificationOffset0 = 0; // UserId$
    private static int VerificationOffset1 = 8; // Username
    private static int VerificationOffset2 = 17; // Password
    private static int VerificationOffset3 = 26; // Child
    private static int VerificationOffset4 = 32; // AList
    private static int VerificationOffset5 = 38; // UserLink
    #pragma warning restore 0414
    private static byte[] VerificationBytes = new byte[] {(byte)'U',(byte)'s',(byte)'e',(byte)'r',(byte)'I',(byte)'d',(byte)'$',(byte)' ',(byte)'U',(byte)'s',(byte)'e',(byte)'r',(byte)'n',(byte)'a',(byte)'m',(byte)'e',(byte)' ',(byte)'P',(byte)'a',(byte)'s',(byte)'s',(byte)'w',(byte)'o',(byte)'r',(byte)'d',(byte)' ',(byte)'C',(byte)'h',(byte)'i',(byte)'l',(byte)'d',(byte)' ',(byte)'A',(byte)'L',(byte)'i',(byte)'s',(byte)'t',(byte)' ',(byte)'U',(byte)'s',(byte)'e',(byte)'r',(byte)'L',(byte)'i',(byte)'n',(byte)'k',(byte)' '};
    private static IntPtr PointerVerificationBytes;

    static TestMessageJsonSerializer() {
        PointerVerificationBytes = BitsAndBytes.Alloc(VerificationBytes.Length); // TODO. Free when program exists
        BitsAndBytes.SlowMemCopy( PointerVerificationBytes, VerificationBytes, (uint)VerificationBytes.Length);
    }

    public static bool Serialize(IntPtr fragment, int size) {
        return false;
    }
    public static TestMessage Deserialize(IntPtr buffer, int bufferSize, out int usedSize) {
        int valueSize;
        TestMessage app = new TestMessage();
        unsafe {
            byte* pfrag = (byte*)buffer;
            byte* pver;
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
                                Int32 val0;
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
                                pver = ((byte*)PointerVerificationBytes + VerificationOffset5 + 5);
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
                                String val5;
                                if (JsonHelper.ParseString((IntPtr)pfrag, nextSize, out val5, out valueSize)) {
                                    app.UserLink = val5;
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
                        ChildApp val3;
                         val3 = ChildAppJsonSerializer.Deserialize((IntPtr)pfrag, nextSize, out valueSize);
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
                        pver = ((byte*)PointerVerificationBytes + VerificationOffset4 + 1);
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
                        while (nextSize > 0) {
                            AListApp val4;
                             val4 = AListAppJsonSerializer.Deserialize((IntPtr)pfrag, nextSize, out valueSize);
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
    private static int VerificationOffset1 = 10; // ASubApp
    private static int VerificationOffset2 = 18; // ASubApp2
    #pragma warning restore 0414
    private static byte[] VerificationBytes = new byte[] {(byte)'C',(byte)'h',(byte)'i',(byte)'l',(byte)'d',(byte)'N',(byte)'a',(byte)'m',(byte)'e',(byte)' ',(byte)'A',(byte)'S',(byte)'u',(byte)'b',(byte)'A',(byte)'p',(byte)'p',(byte)' ',(byte)'A',(byte)'S',(byte)'u',(byte)'b',(byte)'A',(byte)'p',(byte)'p',(byte)'2',(byte)' '};
    private static IntPtr PointerVerificationBytes;

    static ChildAppJsonSerializer() {
        PointerVerificationBytes = BitsAndBytes.Alloc(VerificationBytes.Length); // TODO. Free when program exists
        BitsAndBytes.SlowMemCopy( PointerVerificationBytes, VerificationBytes, (uint)VerificationBytes.Length);
    }

    public static bool Serialize(IntPtr fragment, int size) {
        return false;
    }
    public static ChildApp Deserialize(IntPtr buffer, int bufferSize, out int usedSize) {
        int valueSize;
        ChildApp app = new ChildApp();
        unsafe {
            byte* pfrag = (byte*)buffer;
            byte* pver;
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
                        switch (*pfrag) {
                            case (byte)' ':
                            case (byte)'\r':
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
                                ChildApp.ASubAppApp val1;
                                 val1 = ASubAppAppJsonSerializer.Deserialize((IntPtr)pfrag, nextSize, out valueSize);
                                    app.ASubApp = val1;
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
                                ChildApp.ASubApp2App val2;
                                 val2 = ASubApp2AppJsonSerializer.Deserialize((IntPtr)pfrag, nextSize, out valueSize);
                                    app.ASubApp2 = val2;
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

    public static bool Serialize(IntPtr fragment, int size) {
        return false;
    }
    public static ChildApp.ASubAppApp Deserialize(IntPtr buffer, int bufferSize, out int usedSize) {
        int valueSize;
        ChildApp.ASubAppApp app = new ChildApp.ASubAppApp();
        unsafe {
            byte* pfrag = (byte*)buffer;
            byte* pver;
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

    public static bool Serialize(IntPtr fragment, int size) {
        return false;
    }
    public static ChildApp.ASubApp2App Deserialize(IntPtr buffer, int bufferSize, out int usedSize) {
        int valueSize;
        ChildApp.ASubApp2App app = new ChildApp.ASubApp2App();
        unsafe {
            byte* pfrag = (byte*)buffer;
            byte* pver;
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
                        while (nextSize > 0) {
                            ChildApp.ASubApp2App.ASubListApp val1;
                             val1 = ASubListAppJsonSerializer.Deserialize((IntPtr)pfrag, nextSize, out valueSize);
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

    public static bool Serialize(IntPtr fragment, int size) {
        return false;
    }
    public static ChildApp.ASubApp2App.ASubListApp Deserialize(IntPtr buffer, int bufferSize, out int usedSize) {
        int valueSize;
        ChildApp.ASubApp2App.ASubListApp app = new ChildApp.ASubApp2App.ASubListApp();
        unsafe {
            byte* pfrag = (byte*)buffer;
            byte* pver;
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

    public static bool Serialize(IntPtr fragment, int size) {
        return false;
    }
    public static AListApp Deserialize(IntPtr buffer, int bufferSize, out int usedSize) {
        int valueSize;
        AListApp app = new AListApp();
        unsafe {
            byte* pfrag = (byte*)buffer;
            byte* pver;
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
                        Int32 val1;
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
