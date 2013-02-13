// ***********************************************************************
// <copyright file="App.GetSetValue.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Templates;
using Starcounter.Templates.Interfaces;
using System;
using System.ComponentModel;
using Starcounter.Apps;
using Starcounter.Advanced;

#if CLIENT
namespace Starcounter.Client {
#else
namespace Starcounter {
#endif

    /// <summary>
    /// Class App
    /// </summary>
    public partial class Obj
    {
        /// <summary>
        /// Reads the value for a given property in this App. This method returns all values boxed
        /// as a CLR object. Whenever possible, use the GetValue function specific to a type instead
        /// (i.e. non abstract value templates such as for example GetValue( BoolTemplate property ).
        /// </summary>
        /// <param name="property">The template representing the App property</param>
        /// <returns>The value of the property</returns>
        /// <exception cref="Starcounter.JockeNotImplementedException"></exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public object GetValue(Property property) {
            if (property.Bound)
                return property.GetBoundValueAsObject(this);

#if QUICKTUPLE
            return _Values[property.Index];
#else
            throw new JockeNotImplementedException();
#endif
        }

        /// <summary>
        /// Sets the value for a given property in this App. This method takes the new value
        /// as a boxed CLR object. Whenever possible, use the SetValue function specific to a type instead
        /// (i.e. non abstract value templates such as for example SetValue( BoolTemplate property, bool value ).
        /// </summary>
        /// <param name="property">The template representing the App property</param>
        /// <param name="value">The new value to assign to the property</param>
        /// <exception cref="Starcounter.JockeNotImplementedException"></exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void SetValue(Property property, object value) {
            if (property.Bound) {
                property.SetBoundValueAsObject(this, value);
                ChangeLog.UpdateValue(this, property);
                return;
            }

#if QUICKTUPLE
            _Values[property.Index] = value;
#else
            throw new JockeNotImplementedException();
#endif
            ChangeLog.UpdateValue(this, property);
        }

        /// <summary>
        /// Reads the boolean value for a given property in this App.
        /// </summary>
        /// <param name="property">The template representing the App property</param>
        /// <returns>The value of the property</returns>
        /// <exception cref="Starcounter.JockeNotImplementedException"></exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool GetValue(BoolProperty property) {
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
        public void SetValue(BoolProperty property, bool value) {
            if (property.Bound) {
                property.SetBoundValue(this, value);
                ChangeLog.UpdateValue(this, property);
                return;
            }

#if QUICKTUPLE
            _Values[property.Index] = value;
#else
            throw new JockeNotImplementedException();
#endif

            ChangeLog.UpdateValue(this, property);
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>System.Decimal.</returns>
        /// <exception cref="Starcounter.JockeNotImplementedException"></exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public decimal GetValue(DecimalProperty property) {
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
        public void SetValue(DecimalProperty property, decimal value) {
            if (property.Bound) {
                property.SetBoundValue(this, value);
                ChangeLog.UpdateValue(this, property);
                return;
            }

#if QUICKTUPLE
            _Values[property.Index] = value;
#else
            throw new JockeNotImplementedException();
#endif

            ChangeLog.UpdateValue(this, property);
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>System.Double.</returns>
        /// <exception cref="Starcounter.JockeNotImplementedException"></exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public double GetValue(DoubleProperty property) {
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
        public long GetValue(IntProperty property) {
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
        public ulong GetValue(OidProperty property) {
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
        public string GetValue(StringProperty property) {
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
        public void SetValue(StringProperty property, string value) {
            if (property.Bound) {
                property.SetBoundValue(this, value);
                ChangeLog.UpdateValue(this, property);
                return;
            }

#if QUICKTUPLE
            _Values[property.Index] = value;
#else
            throw new JockeNotImplementedException();
#endif

            ChangeLog.UpdateValue(this, property);
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>Action.</returns>
        /// <exception cref="Starcounter.JockeNotImplementedException"></exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Action GetValue(ActionProperty property) {
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
        public void SetValue(ActionProperty property, Action value) {
#if QUICKTUPLE
            _Values[property.Index] = value;
#else
            throw new JockeNotImplementedException();
#endif
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>Listing.</returns>
        /// <exception cref="Starcounter.JockeNotImplementedException"></exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Listing GetValue(ObjArrProperty property) {
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
        public void SetValue(ObjArrProperty property, SqlResult data) {
            Listing current = _Values[property.Index];
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
        public void SetValue(ObjArrProperty templ, Listing data) {
            Listing current = _Values[templ.Index];
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
        /// <returns>Listing{``0}.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Listing<T> GetTypedValue<T>(ObjArrProperty property) where T : Obj, new() {
#if QUICKTUPLE
            return (Listing<T>)(_Values[property.Index]);
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
        public void SetValue<T>(ObjArrProperty templ, SqlResult data) where T : Obj, new() {
            Listing<T> newList;
            Listing<T> current = _Values[templ.Index];
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
        /// <exception cref="System.NotImplementedException"></exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void SetValue<T>(ObjArrProperty templ, Listing<T> data) where T : App, new() {
            Listing<T> current = _Values[templ.Index];
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
        /// <exception cref="Starcounter.JockeNotImplementedException"></exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public App GetValue(AppTemplate property) {
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
        public T GetValue<T>(AppTemplate property) where T : App, new() {
#if QUICKTUPLE
            return (T)(_Values[property.Index]);
#else
            throw new NotImplementedException();
#endif
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>App.</returns>
        /// <exception cref="Starcounter.JockeNotImplementedException"></exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public App GetValue(IAppTemplate property) {
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
        public void SetValue(AppTemplate property, App value) {
#if QUICKTUPLE
            _Values[property.Index] = value;
#else
            throw new JockeNotImplementedException();
#endif

            ChangeLog.UpdateValue(this, property);
        }

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="Starcounter.JockeNotImplementedException"></exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void SetValue(AppTemplate property, IBindable value) {
#if QUICKTUPLE
            Obj app = (Obj)property.CreateInstance(this);
            app.Data = value;
            _Values[property.Index] = app;
#else
            throw new JockeNotImplementedException();
#endif

            ChangeLog.UpdateValue(this, property);
        }

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="Starcounter.JockeNotImplementedException"></exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void SetValue(IAppTemplate property, App value) {
#if QUICKTUPLE
            _Values[property.Index] = value;
#else
            throw new JockeNotImplementedException();
#endif
        }
    }
}
