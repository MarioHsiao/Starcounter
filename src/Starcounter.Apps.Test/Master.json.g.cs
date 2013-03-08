// This is a system generated file. It reflects the Starcounter App Template defined in the file "unknown"
// DO NOT MODIFY DIRECTLY - CHANGES WILL BE OVERWRITTEN

using System;
using System.Collections.Generic;
using Starcounter;
using Starcounter.Internal;
using Starcounter.Templates;

public partial class Master {
    public static MasterTemplate DefaultTemplate = new MasterTemplate();
    public Master() {
        Template = DefaultTemplate;
    }
    public new MasterTemplate Template { get { return (MasterTemplate)base.Template; } set { base.Template = value; } }
    public new MasterMetadata Metadata { get { return (MasterMetadata)base.Metadata; } }
    public Puppet Page { get { return Get<Puppet>(Template.Page); } set { this.Set(Template.Page, value); } }
    public String Test { get { return this.Get(Template.Test); } set { this.Set(Template.Test, value); } }
    public class MasterTemplate : TPuppet {
        public MasterTemplate()
            : base() {
            InstanceType = typeof(Master);
            ClassName = "Master";
            Page = Add<TPuppet>("Page");
            Test = Add<TString>("Test");
            Test.AddHandler( 
                (Obj app, TValue<string> prop, string value) => { return (new Input.Test() { App = (Master)app, Template = (TString)prop, Value = value } ) ; },
                (Obj app, Input<string> Input) => ((Master)app).Handle((Input.Test)Input) );
        }
        public TPuppet Page;
        public TString Test;
    }
    public class MasterMetadata : ObjMetadata {
        public MasterMetadata(Puppet app, TPuppet template) : base(app, template) { }
        public new Master App { get { return (Master)base.App; } }
        public new Master.MasterTemplate Template { get { return (Master.MasterTemplate)base.Template; } }
        public ObjMetadata Page { get { return __p_Page ?? (__p_Page = new ObjMetadata(App, App.Template.Page)); } } private ObjMetadata __p_Page;
        public StringMetadata Test { get { return __p_Test ?? (__p_Test = new StringMetadata(App, App.Template.Test)); } } private StringMetadata __p_Test;
    }
    public static class Json {
    }
    public static class Input {
        public class Test : Input<Master, TString, String> {
        }
    }
}
