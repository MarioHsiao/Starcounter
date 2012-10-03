
using System;
using Starcounter.Templates.Interfaces;
#if CLIENT
namespace Starcounter.Client.Template {
#else
namespace Starcounter.Templates {
#endif

    public class OidProperty : Property
#if IAPP
        , IOidTemplate
#endif
    {

        public UInt64 DefaultValue { get; set; }

        public override object DefaultValueAsObject {
            get { return DefaultValue; }
            set { DefaultValue = (UInt64)value; }
        }
        public override Type InstanceType {
            get { return typeof(UInt64); }
        }
    }
}
