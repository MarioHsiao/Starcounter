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


        public void ProcessInput( App app, string value ) {
            Input<String> input = null;

            if (CustomInputEventCreator != null)
                input = CustomInputEventCreator.Invoke(app, this, value);

            if (input != null) {
                foreach (var h in CustomInputHandlers) {
                    h.Invoke(app, input);
                }
                if (!input.Cancelled) {
                    Console.WriteLine("Setting value after custom handler: " + value);
                    app.SetValue(this, value);
                }
                else {
                    Console.WriteLine("Handler cancelled: " + value);
                }
            }
            else {
                Console.WriteLine("Setting value after no handler: " + value);
                app.SetValue(this, value);
            }
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
