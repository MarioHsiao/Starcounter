
using System.Diagnostics;
using Starcounter.Templates;

namespace Starcounter {
    public partial class Json {

        /// <summary>
        /// As messages are not kept at the server, it does not make sense to interact with
        /// them using "user input".
        /// </summary>
        /// <typeparam name="V">The type of the input value</typeparam>
        /// <param name="template">The property having changed</param>
        /// <param name="value">The new value of the property</param>
        public void ProcessInput<V>(Property<V> template, V value) {
            Input<V> input = null;

            if (template.CustomInputEventCreator != null)
                input = template.CustomInputEventCreator.Invoke(this, template, value);

            if (input != null) {
                foreach (var h in template.CustomInputHandlers) {
                    h.Invoke(this, input);
                }
                if (!input.Cancelled) {
                    Debug.WriteLine("Setting value after custom handler: " + input.Value);
					((Property<V>)template).Setter(this, input.Value);
                }
                else {
                    Debug.WriteLine("Handler cancelled: " + value);
                }
            }
            else {
                Debug.WriteLine("Setting value after no handler: " + value);
				((Property<V>)template).Setter(this, value);
            }
        }

    }
}
