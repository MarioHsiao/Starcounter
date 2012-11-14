using System;

namespace Starcounter.Templates.DataBinding {
    internal class DataBinding<TValue> {
        private Func<App, TValue> dataGetter;
        private Action<App, TValue> dataSetter;

        internal DataBinding(Func<App, TValue> dataGetter)
            : this(dataGetter, null) {
        }

        internal DataBinding(Func<App, TValue> dataGetter, Action<App, TValue> dataSetter) {
            this.dataGetter = dataGetter;
            this.dataSetter = dataSetter;
        }

        /// <summary>
        /// Returns the value from the underlying Entity in the App
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        internal TValue GetValue(App app) {
            if (dataGetter != null) {
                return dataGetter(app);
            }
            return default(TValue);
        }

        /// <summary>
        /// Sets the value on the underlying entity in the App.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="value"></param>
        internal void SetValue(App app, TValue value) {
            if (dataSetter != null) {
                dataSetter(app, value);
            }
        }
    }
}
