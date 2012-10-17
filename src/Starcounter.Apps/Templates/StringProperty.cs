using System;
using Starcounter.Templates.Interfaces;
using System.Collections.Generic;

#if CLIENT
namespace Starcounter.Client.Template {
#else
namespace Starcounter.Templates {
#endif

    public class StringProperty : Property<string>
#if IAPP
        , IStringTemplate
#endif
    {
        public override void ProcessInput(App app, byte[] rawValue)
        {
            ProcessInput(app, System.Text.Encoding.UTF8.GetString(rawValue));
        }

        private string _DefaultValue = "";

        public string DefaultValue {
            get { return _DefaultValue; }
            set { _DefaultValue = value; }
        }
        
        public override object DefaultValueAsObject {
            get {
                return DefaultValue;
            }
            set {
                DefaultValue = (string)value;
            }
        }

        public override Type InstanceType {
            get { return typeof(string); }
        }
    }
}
