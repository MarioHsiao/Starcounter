using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter {
    public partial class Puppet {

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="template"></param>
        /// <param name="value"></param>
        public void ProcessInput<T>(TValue<T> template, T value) {
            Input<T> input = null;

            if (template.CustomInputEventCreator != null)
                input = template.CustomInputEventCreator.Invoke(this, template, value);

            if (input != null) {
                foreach (var h in template.CustomInputHandlers) {
                    h.Invoke(this, input);
                }
                if (!input.Cancelled) {
                    Debug.WriteLine("Setting value after custom handler: " + input.Value);
                    SetValue((TValue<T>)template, input.Value);
                }
                else {
                    Debug.WriteLine("Handler cancelled: " + value);
                }
            }
            else {
                Debug.WriteLine("Setting value after no handler: " + value);
                SetValue((TValue<T>)template, value);
            }
        }
    }
}
