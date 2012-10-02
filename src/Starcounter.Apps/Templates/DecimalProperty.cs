

using System;
using Starcounter.Templates.Interfaces;
#if CLIENT
namespace Starcounter.Client.Template {
#else
namespace Starcounter.Templates {
#endif

    public class DecimalProperty : Property
#if IAPP
        , IDecimalTemplate
#endif
    {
        decimal _DefaultValue = 0;

        public decimal DefaultValue {
            get { return _DefaultValue; }
            set { _DefaultValue = value; }
        }

        public override object DefaultValueAsObject {
            get {
                return DefaultValue;
            }
            set {
                DefaultValue = (decimal)value;
            }
        }
        public override Type InstanceType {
            get { return typeof(decimal); }
        }
    }
}
