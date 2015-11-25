
using Starcounter.Binding;
using Starcounter.Query.Execution;
using System;

namespace Starcounter.ObjectView {

    /// <summary>
    /// Provide the ability for clients to enumerate values of any 
    /// <see cref="IObjectView"/> as string data.
    /// </summary>
    public class ViewReader {
        readonly ValueFormatter formatter;
        readonly DefaultPropertyReader defaultReader;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vf"></param>
        public ViewReader(ValueFormatter vf) {
            formatter = vf;
            defaultReader = new DefaultPropertyReader(formatter);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="view"></param>
        /// <param name="receiver"></param>
        public void AllValues(IObjectView view, Action<IPropertyBinding, string, string> receiver) {
            var row = view as Row;
            if (row != null) {
                AllValuesFromRow(row, receiver);
            }
            else {
                AllValuesFromDefault(view, receiver);
            }
        }

        void AllValuesFromRow(Row row, Action<IPropertyBinding, string, string> receiver) {
            var binding = row.TypeBinding;
            for (int i = 0; i < binding.PropertyCount; i++) {
                var property = binding.GetPropertyBinding(i);

                string displayName;
                var value = row.GetPropertyStringValue(i, formatter, out displayName);

                receiver(property, displayName, value);
            }
        }

        void AllValuesFromDefault(IObjectView view, Action<IPropertyBinding, string, string> receiver) {
            var binding = view.TypeBinding;

            for (int i = 0; i < binding.PropertyCount; i++) {
                var property = binding.GetPropertyBinding(i);

                var value = defaultReader.GetPropertyStringValue(view, binding, property);

                receiver(property, property.Name, value);
            }
        }
    }
}
