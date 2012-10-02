
using System;
using Starcounter.Templates.Interfaces;
#if CLIENT
namespace Starcounter.Client.Template {
#else
namespace Starcounter.Templates {
#endif
    /// <summary>
    /// Defines a boolean property in an App object. 
    /// </summary>
    public class BoolProperty : Property
#if IAPP
        , IBoolTemplate
#endif
    {
        private bool _DefaultValue = false;

        /// <summary>
        /// The default value for a boolean property is false. For the
        /// property defined by this template, you can set an alternative
        /// default value (i.e. true).
        /// </summary>
        public bool DefaultValue {
            get { return _DefaultValue; }
            set { _DefaultValue = value; }
        }

        /// <summary>
        /// Will return a boxed version of the DefaultValue property.
        /// </summary>
        public override object DefaultValueAsObject {
            get {
                return DefaultValue;
            }
            set {
                DefaultValue = (bool)value;
            }
        }

        /// <summary>
        /// Will return the Boolean runtime type
        /// </summary>
        public override Type InstanceType {
            get { return typeof(bool); }
        }
    }
}
