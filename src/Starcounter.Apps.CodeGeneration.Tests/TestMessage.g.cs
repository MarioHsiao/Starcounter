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
        public class ChildAppTemplate : AppTemplate {
            public ChildAppTemplate()
                : base() {
                InstanceType = typeof(TestMessage.ChildApp);
                ClassName = "ChildApp";
                ChildName = Register<StringProperty>("ChildName", "ChildName");
                Button = Register<ActionProperty>("Button", "Button");
            }
            public override object CreateInstance(AppNode parent) { return new ChildApp(this) { Parent = (TestMessage)parent }; }
            public StringProperty ChildName;
            public ActionProperty Button;
        }
        public class ChildAppMetadata : AppMetadata {
            public ChildAppMetadata(App app, AppTemplate template) : base(app, template) { }
            public new TestMessage.ChildApp App { get { return (TestMessage.ChildApp)base.App; } }
            public new TestMessage.ChildApp.ChildAppTemplate Template { get { return (TestMessage.ChildApp.ChildAppTemplate)base.Template; } }
            public StringMetadata ChildName { get { return __p_ChildName ?? (__p_ChildName = new StringMetadata(App, App.Template.ChildName)); } }
            private StringMetadata __p_ChildName;
            public ActionMetadata Button { get { return __p_Button ?? (__p_Button = new ActionMetadata(App, App.Template.Button)); } }
            private ActionMetadata __p_Button;
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
            UserLink = Register<StringProperty>("UserLink", "UserLink");
            User = Register<ActionProperty>("User", "User");
        }
        public override object CreateInstance(AppNode parent) { return new TestMessage(this) { Parent = parent }; }
        public IntProperty UserId;
        public StringProperty Username;
        public StringProperty Password;
        public TestMessage.ChildApp.ChildAppTemplate Child;
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
        public StringMetadata UserLink { get { return __p_UserLink ?? (__p_UserLink = new StringMetadata(App, App.Template.UserLink)); } }
        private StringMetadata __p_UserLink;
        public ActionMetadata User { get { return __p_User ?? (__p_User = new ActionMetadata(App, App.Template.User)); } }
        private ActionMetadata __p_User;
    }
    public static class Json {
        public class Child : TemplateAttribute {
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
        }
        public class UserLink : Input<TestMessage, StringProperty, String> {
        }
        public class User : Input<TestMessage, ActionProperty, Action> {
        }
    }
    
    public static class ChildAppJsonSerializer{

        public static int VerificationOffset0 = 0; // ChildName
        public static byte[] VerificationBytes = new byte[] {(byte)'C',(byte)'h',(byte)'i',(byte)'l',(byte)'d',(byte)'N',(byte)'a',(byte)'m',(byte)'e',(byte)' '};
        public static IntPtr PointerVerificationBytes;

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
                    while (true) {
                        if (*pfrag == '"')
                            break;
                        if (*pfrag == '}') {
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
                    nextSize --;
                    if (nextSize<0 || (*pfrag) != (*pver) ) {
                        throw new Exception("Deserialization failed. Verification failed.");
                    }
                    pfrag++;
                    pver++;
                    pfrag++;
                    while (*pfrag == ' ') {
                        pfrag++;
                        nextSize--;
                        if (nextSize < 0)
                             throw new Exception("Deserialization failed.");
                    }
                    pfrag++;
                    nextSize--;
                    if (nextSize < 0)
                        throw new Exception("Deserialization failed.");
                    while (*pfrag == ' ') {
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
                }
            }
            throw new Exception("Deserialization of App failed.");
        }
    }

    
    public static class TestMessageJsonSerializer{

        public static int VerificationOffset0 = 0; // UserId$
        public static int VerificationOffset1 = 8; // Username
        public static int VerificationOffset2 = 17; // Password
        public static int VerificationOffset3 = 26; // UserLink
        public static byte[] VerificationBytes = new byte[] {(byte)'U',(byte)'s',(byte)'e',(byte)'r',(byte)'I',(byte)'d',(byte)'$',(byte)' ',(byte)'U',(byte)'s',(byte)'e',(byte)'r',(byte)'n',(byte)'a',(byte)'m',(byte)'e',(byte)' ',(byte)'P',(byte)'a',(byte)'s',(byte)'s',(byte)'w',(byte)'o',(byte)'r',(byte)'d',(byte)' ',(byte)'U',(byte)'s',(byte)'e',(byte)'r',(byte)'L',(byte)'i',(byte)'n',(byte)'k',(byte)' '};
        public static IntPtr PointerVerificationBytes;

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
                    while (true) {
                        if (*pfrag == '"')
                            break;
                        if (*pfrag == '}') {
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
                                    pfrag++;
                                    while (*pfrag == ' ') {
                                        pfrag++;
                                        nextSize--;
                                        if (nextSize < 0)
                                             throw new Exception("Deserialization failed.");
                                    }
                                    pfrag++;
                                    nextSize--;
                                    if (nextSize < 0)
                                        throw new Exception("Deserialization failed.");
                                    while (*pfrag == ' ') {
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
                                    pfrag++;
                                    while (*pfrag == ' ') {
                                        pfrag++;
                                        nextSize--;
                                        if (nextSize < 0)
                                             throw new Exception("Deserialization failed.");
                                    }
                                    pfrag++;
                                    nextSize--;
                                    if (nextSize < 0)
                                        throw new Exception("Deserialization failed.");
                                    while (*pfrag == ' ') {
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
                                    pver = ((byte*)PointerVerificationBytes + VerificationOffset3 + 5);
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
                                    pfrag++;
                                    while (*pfrag == ' ') {
                                        pfrag++;
                                        nextSize--;
                                        if (nextSize < 0)
                                             throw new Exception("Deserialization failed.");
                                    }
                                    pfrag++;
                                    nextSize--;
                                    if (nextSize < 0)
                                        throw new Exception("Deserialization failed.");
                                    while (*pfrag == ' ') {
                                        pfrag++;
                                        nextSize--;
                                        if (nextSize < 0)
                                             throw new Exception("Deserialization failed.");
                                    }
                                    String val3;
                                    if (JsonHelper.ParseString((IntPtr)pfrag, nextSize, out val3, out valueSize)) {
                                        app.UserLink = val3;
                                        nextSize -= valueSize;
                                        if (nextSize < 0) {
                                            throw new Exception("Unable to deserialize App. Unexpected end of content");
                                        }
                                        pfrag += valueSize;
                                    } else {
                                        throw new Exception("Unable to deserialize App. Content not compatible.");
                                    }
                                   break;
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
                            pfrag++;
                            while (*pfrag == ' ') {
                                pfrag++;
                                nextSize--;
                                if (nextSize < 0)
                                     throw new Exception("Deserialization failed.");
                            }
                            pfrag++;
                            nextSize--;
                            if (nextSize < 0)
                                throw new Exception("Deserialization failed.");
                            while (*pfrag == ' ') {
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
                    }
                }
            }
            throw new Exception("Deserialization of App failed.");
        }
    }

}
}
