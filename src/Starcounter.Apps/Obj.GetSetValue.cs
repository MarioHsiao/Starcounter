// ***********************************************************************
// <copyright file="App.GetSetValue.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Templates;
using System;
using System.ComponentModel;
using Starcounter.Apps;
using Starcounter.Advanced;

namespace Starcounter {

    public partial class Obj {
        /// <summary>
        /// Reads the value for a given property in this Obj. This method returns all values boxed
        /// as a CLR object. Whenever possible, use the GetValue function specific to a type instead
        /// (i.e. non abstract value templates such as for example GetValue( BoolTemplate property ).
        /// </summary>
        /// <param name="property">The template representing the property to read</param>
        /// <returns>The value of the property</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public object GetValue(TValue property) {
            if (property.Bound)
                return property.GetBoundValueAsObject(this);

#if QUICKTUPLE
            return _Values[property.Index];
#else
            throw new JockeNotImplementedException();
#endif
        }

        /// <summary>
        /// Sets the value for a given property in this Obj. This method takes the new value
        /// as a boxed CLR object. Whenever possible, use the SetValue function specific to a type instead
        /// (i.e. non abstract value templates such as for example SetValue( BoolTemplate property, bool value ).
        /// </summary>
        /// <param name="property">The template representing the property to set</param>
        /// <param name="value">The new value to assign to the property</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void SetValue(TValue property, object value) {
            if (property.Bound) {
                property.SetBoundValueAsObject(this, value);
                this.HasChanged(property);
                return;
            }

#if QUICKTUPLE
            _Values[property.Index] = value;
#else
            throw new JockeNotImplementedException();
#endif
            this.HasChanged(property);
        }

        /// <summary>
        /// Reads the boolean value for a given property in this App.
        /// </summary>
        /// <param name="property">The template representing the App property</param>
        /// <returns>The value of the property</returns>
        /// <exception cref="Starcounter.JockeNotImplementedException"></exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool GetValue(TBool property) {
#if QUICKTUPLE

            if (property.Bound)
                return property.GetBoundValue(this);
            return _Values[property.Index];
#else
            throw new JockeNotImplementedException();
#endif
        }

        /// <summary>
        /// Sets the value for a given boolean property in this App.
        /// </summary>
        /// <param name="property">The template representing the App property</param>
        /// <param name="value">The new value to assign to the property</param>
        /// <exception cref="Starcounter.JockeNotImplementedException"></exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void SetValue(TBool property, bool value) {
            if (property.Bound) {
                property.SetBoundValue(this, value);
                HasChanged(property);
                //                ChangeLog.UpdateValue(this, property);
                return;
            }

#if QUICKTUPLE
            _Values[property.Index] = value;
#else
            throw new JockeNotImplementedException();
#endif
            HasChanged(property);
            // ChangeLog.UpdateValue(this, property);
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>System.Decimal.</returns>
        /// <exception cref="Starcounter.JockeNotImplementedException"></exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public decimal GetValue(TDecimal property) {
#if QUICKTUPLE
            if (property.Bound)
                return property.GetBoundValue(this);

            return _Values[property.Index];
#else
            throw new JockeNotImplementedException();
#endif
        }

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="Starcounter.JockeNotImplementedException"></exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void SetValue(TDecimal property, decimal value) {
            if (property.Bound) {
                property.SetBoundValue(this, value);
                HasChanged(property);
                //                ChangeLog.UpdateValue(this, property);
                return;
            }

#if QUICKTUPLE
            _Values[property.Index] = value;
#else
            throw new JockeNotImplementedException();
#endif
            this.HasChanged(property);
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>System.Double.</returns>
        /// <exception cref="Starcounter.JockeNotImplementedException"></exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public double GetValue(TDouble property) {
            if (property.Bound)
                return property.GetBoundValue(this);

#if QUICKTUPLE
            return _Values[property.Index];
#else
            throw new JockeNoaatImplementedException();
#endif
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>System.Int32.</returns>
        /// <exception cref="Starcounter.JockeNotImplementedException"></exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public long GetValue(TLong property) {
            if (property.Bound)
                return property.GetBoundValue(this);

#if QUICKTUPLE
            return _Values[property.Index];
#else
            throw new JockeNotImplementedException();
#endif
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>System.UInt64.</returns>
        /// <exception cref="Starcounter.JockeNotImplementedException"></exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ulong GetValue(TOid property) {
            if (property.Bound)
                return property.GetBoundValue(this);

#if QUICKTUPLE
            return _Values[property.Index];
#else
            throw new JockeNotImplementedException();
#endif
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="Starcounter.JockeNotImplementedException"></exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public string GetValue(TString property) {
            if (property.Bound)
                return property.GetBoundValue(this);

#if QUICKTUPLE
            return _Values[property.Index];
#else
            throw new JockeNotImplementedException();
#endif
        }

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="Starcounter.JockeNotImplementedException"></exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void SetValue(TString property, string value) {
            if (property.Bound) {
                property.SetBoundValue(this, value);
                this.HasChanged(property);
                return;
            }

#if QUICKTUPLE
            _Values[property.Index] = value;
#else
            throw new JockeNotImplementedException();
#endif
            this.HasChanged(property);
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>Arr.</returns>
        /// <exception cref="Starcounter.JockeNotImplementedException"></exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Arr GetValue(TObjArr property) {
#if QUICKTUPLE
            return _Values[property.Index];
#else
            throw new JockeNotImplementedException();
#endif
        }

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="data">The data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void SetValue(TObjArr property, SqlResult data) {
            Arr current = _Values[property.Index];
            if (current != null) {
                current.Clear();
                current.notEnumeratedResult = data;
                current.InitializeAfterImplicitConversion(this, property);
            }
        }

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="templ">The templ.</param>
        /// <param name="data">The data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void SetValue(TObjArr templ, Arr data) {
            Arr current = _Values[templ.Index];
            if (current != null)
                current.Clear();

            data.InitializeAfterImplicitConversion(this, templ);
            _Values[templ.Index] = data;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="property">The property.</param>
        /// <returns>Arr{``0}.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Arr<T> GetTypedValue<T>(TObjArr property) where T : Obj, new() {
#if QUICKTUPLE
            return (Arr<T>)(_Values[property.Index]);
#else
            throw new NotImplementedException();
#endif
        }

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="templ">The templ.</param>
        /// <param name="data">The data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void SetValue<T>(TObjArr templ, SqlResult data) where T : Obj, new() {
            Arr<T> newList;
            Arr<T> current = _Values[templ.Index];
            if (current != null)
                current.Clear();

            newList = data;
            newList.InitializeAfterImplicitConversion(this, templ);

            _Values[templ.Index] = newList;
        }

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="templ">The templ.</param>
        /// <param name="data">The data.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void SetValue<T>(TObjArr templ, Arr<T> data) where T : Obj, new() {
            Arr<T> current = _Values[templ.Index];
            if (current != null)
                current.Clear();

            data.InitializeAfterImplicitConversion(this, templ);
            _Values[templ.Index] = data;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>App.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Obj GetValue(TObj property) {
#if QUICKTUPLE
            return _Values[property.Index];
#else
            throw new JockeNotImplementedException();
#endif
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="property">The property.</param>
        /// <returns>``0.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public T GetTypedValue<T>(TObj property) where T : Obj, new() {
#if QUICKTUPLE
            return (T)(_Values[property.Index]);
#else
            throw new NotImplementedException();
#endif
        }

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="Starcounter.JockeNotImplementedException"></exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void SetValue(TObj property, Obj value) {
#if QUICKTUPLE
            _Values[property.Index] = value;
#else
            throw new JockeNotImplementedException();
#endif
            this.HasChanged(property);
        }

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="Starcounter.JockeNotImplementedException"></exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void SetValue(TObj property, IBindable value) {
#if QUICKTUPLE
            Obj app = (Obj)property.CreateInstance(this);
            app.Data = value;
            _Values[property.Index] = app;
#else
            throw new JockeNotImplementedException();
#endif
            this.HasChanged(property);
        }
    }
}
