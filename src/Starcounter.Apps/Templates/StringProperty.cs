using System;
using Starcounter.Templates.Interfaces;

#if CLIENT
namespace Starcounter.Client.Template {
#else
namespace Starcounter.Templates {
#endif

    public class StringProperty : Property
#if IAPP
        , IStringTemplate
#endif
    {

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
