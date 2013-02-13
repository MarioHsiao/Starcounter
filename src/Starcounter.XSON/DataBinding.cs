using System;

namespace Starcounter.Templates.DataBinding {
    internal class DataBinding<TValue> {
        private Func<Obj, TValue> dataGetter;
        private Action<Obj, TValue> dataSetter;

        internal DataBinding(Func<Obj, TValue> dataGetter)
            : this(dataGetter, null) {
        }

        internal DataBinding(Func<Obj, TValue> dataGetter, Action<Obj, TValue> dataSetter) {
            this.dataGetter = dataGetter;
            this.dataSetter = dataSetter;
        }

        /// <summary>
        /// Returns the value from the underlying Entity in the App
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        internal TValue GetValue(Obj app) {
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
        internal void SetValue(Obj app, TValue value) {
            if (dataSetter != null) {
                dataSetter(app, value);
            }
        }
    }
}
