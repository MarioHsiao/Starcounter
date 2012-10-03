

using System;
using Starcounter.Templates.Interfaces;
#if CLIENT
namespace Starcounter.Client.Template {
#else
namespace Starcounter.Templates {
#endif

    public class DoubleProperty : Property
#if IAPP
        , IDoubleTemplate
#endif
    {
       private double _DefaultValue = 0;

        public double DefaultValue {
            get { return _DefaultValue; }
            set { _DefaultValue = value; }
        }
        
        public override object DefaultValueAsObject {
            get {
                return DefaultValue;
            }
            set {
                DefaultValue = (double)value;
            }
        }
        
        public override Type InstanceType {
            get { return typeof(double); }
        }
    }
}
