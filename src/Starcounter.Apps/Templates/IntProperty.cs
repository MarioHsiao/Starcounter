

using System;
using Starcounter.Internal;
using Starcounter.Templates.Interfaces;
#if CLIENT
namespace Starcounter.Client.Template {
#else
namespace Starcounter.Templates {
#endif

    public class IntProperty : Property<int>
#if IAPP
        , IIntTemplate
#endif
    {

        private int _DefaultValue = 0;

        public override void ProcessInput(App app, byte[] rawValue)
        {
            int v = (int)Utf8Helper.IntFastParseFromAscii(rawValue, 0, (uint)rawValue.Length);
            ProcessInput(app, v);
        }

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
