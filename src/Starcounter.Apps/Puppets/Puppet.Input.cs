using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter {
    public partial class Puppet<T> {

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <param name="template"></param>
        /// <param name="value"></param>
        public override void ProcessInput<V>(TValue<V> template, V value) {
            Input<V> input = null;

            if (template.CustomInputEventCreator != null)
                input = template.CustomInputEventCreator.Invoke(this, template, value);

            if (input != null) {
                foreach (var h in template.CustomInputHandlers) {
                    h.Invoke(this, input);
                }
                if (!input.Cancelled) {
                    Debug.WriteLine("Setting value after custom handler: " + input.Value);
                    this.Set<V>((TValue<V>)template, input.Value);
                }
                else {
                    Debug.WriteLine("Handler cancelled: " + value);
                }
            }
            else {
                Debug.WriteLine("Setting value after no handler: " + value);
                this.Set<V>((TValue<V>)template, value);
            }
        }
    }
}
