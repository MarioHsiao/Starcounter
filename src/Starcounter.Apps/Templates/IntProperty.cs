

using System;
using Starcounter.Templates.Interfaces;
#if CLIENT
namespace Starcounter.Client.Template {
#else
namespace Starcounter.Templates {
#endif

    public class IntProperty : Property
#if IAPP
        , IIntTemplate
#endif
    {

        private int _DefaultValue = 0;

        public int DefaultValue {
            get { return _DefaultValue; }
            set { _DefaultValue = value; }
        }

        public override object DefaultValueAsObject {
            get {
                return DefaultValue;
            }
            set {
                DefaultValue = (int)value;
            }
        }
        public override Type InstanceType {
            get { return typeof(int); }
        }
    }
}
