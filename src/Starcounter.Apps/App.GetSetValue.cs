// ***********************************************************************
// <copyright file="App.GetSetValue.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Templates;
using Starcounter.Templates.Interfaces;
using System;
using System.ComponentModel;

#if CLIENT
namespace Starcounter.Client {
#else
namespace Starcounter {
#endif

    /// <summary>
    /// Class App
    /// </summary>
    public partial class App
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
        public object GetValue(IValueTemplate property) {
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
        public void SetValue(IValueTemplate property, object value) {
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
            return _Values[property.Index];
#else
            throw new JockeNotImplementedException();
#endif
        }

        /// <summary>
        /// Reads the boolean value for a given property in this App.
        /// </summary>
        /// <param name="property">The template representing the App property</param>
        /// <returns>The value of the property</returns>
        /// <exception cref="Starcounter.JockeNotImplementedException"></exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool GetValue(IBoolTemplate property) {
#if QUICKTUPLE
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
        public void SetValue(IBoolTemplate property, bool value) {
#if QUICKTUPLE
            _Values[property.Index] = value;
#else
            throw new JockeNotImplementedException();
#endif

            ChangeLog.UpdateValue(this, property);
        }

        /// <summary>
        /// Sets the value for a given boolean property in this App.
        /// </summary>
        /// <param name="property">The template representing the App property</param>
        /// <param name="value">The new value to assign to the property</param>
        /// <exception cref="Starcounter.JockeNotImplementedException"></exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void SetValue(BoolProperty property, bool value) {
#if QUICKTUPLE
            _Values[property.Index] = value;
#else
            throw new JockeNotImplementedException();
#endif

            ChangeLog.UpdateValue(this, property);
        }


//        /// <summary>
//        /// Gets an Entity or an App for a given reference property in this App.
//        /// </summary>
//        /// <param name="property">The template representing the App property</param>
//        /// <returns>The value of the property</returns>
//        [EditorBrowsable(EditorBrowsableState.Never)]
//        public Entity GetValue(ObjectProperty property) {
//#if QUICKTUPLE
//            return _Values[property.Index];
//#else
//            throw new JockeNotImplementedException();
//#endif
//        }

        /// <summary>
        /// Gets an Entity or an App for a given reference property in this App.
        /// </summary>
        /// <param name="property">The template representing the App property</param>
        /// <returns>The value of the property</returns>
        /// <exception cref="Starcounter.JockeNotImplementedException"></exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Entity GetValue(IObjectTemplate property) {
#if QUICKTUPLE
            return _Values[property.Index];
#else
            throw new JockeNotImplementedException();
#endif
        }

        /// <summary>
        /// Sets an Entity or an App for a given reference property in this App.
        /// </summary>
        /// <param name="property">The template representing the App property</param>
        /// <param name="value">The new value to assign to the property</param>
        /// <exception cref="Starcounter.JockeNotImplementedException"></exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void SetValue(IObjectTemplate property, Entity value) {
#if QUICKTUPLE
            _Values[property.Index] = value;
#else
            throw new JockeNotImplementedException();
#endif

            ChangeLog.UpdateValue(this, property);
        }

//        /// <summary>
//        /// Sets an Entity or an App for a given reference property in this App.
//        /// </summary>
//        /// <param name="property">The template representing the App property</param>
//        /// <param name="value">The new value to assign to the property</param> 
//        [EditorBrowsable(EditorBrowsableState.Never)]
//        public void SetValue(ObjectProperty property, Entity value) {
//#if QUICKTUPLE
//            _Values[property.Index] = value;
//#else
//            throw new JockeNotImplementedException();
//#endif
//        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>System.Decimal.</returns>
        /// <exception cref="Starcounter.JockeNotImplementedException"></exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public decimal GetValue(DecimalProperty property) {
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
        /// <returns>System.Decimal.</returns>
        /// <exception cref="Starcounter.JockeNotImplementedException"></exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public decimal GetValue(IDecimalTemplate property) {
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
        public void SetValue(IDecimalTemplate property, decimal value) {
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
        public void SetValue(DecimalProperty property, decimal value) {
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
        /// <returns>System.Double.</returns>
        /// <exception cref="Starcounter.JockeNotImplementedException"></exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public double GetValue(IDoubleTemplate property) {
#if QUICKTUPLE
            return _Values[property.Index];
#else
            throw new JockeNotImplementedException();
#endif
        }
//        public object SetValue(DoubleTemplate property, double value) {
//            return _Values[property.Index] = value;
//        }

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="Starcounter.JockeNotImplementedException"></exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void SetValue(IDoubleTemplate property, double value) {
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
        /// <returns>System.Int32.</returns>
        /// <exception cref="Starcounter.JockeNotImplementedException"></exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public int GetValue(IntProperty property) {
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
        /// <returns>System.Int32.</returns>
        /// <exception cref="Starcounter.JockeNotImplementedException"></exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public int GetValue(IIntTemplate property) {
#if QUICKTUPLE
            return _Values[property.Index];
#else
            throw new JockeNotImplementedException();
#endif
        }
//        public object SetValue(IntTemplate property, int value) {
//            return _Values[property.Index] = value;
//        }

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="Starcounter.JockeNotImplementedException"></exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void SetValue(IIntTemplate property, int value) {
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
        /// <returns>System.UInt64.</returns>
        /// <exception cref="Starcounter.JockeNotImplementedException"></exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ulong GetValue(OidProperty property) {
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
        public ulong GetValue(IOidTemplate property) {
#if QUICKTUPLE
            return _Values[property.Index];
#else
            throw new JockeNotImplementedException();
#endif
        }
  //      public object SetValue(OidTemplate property, UInt64 value) {
  //          return _Values[property.Index] = value;
  //      }

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="Starcounter.JockeNotImplementedException"></exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void SetValue(IOidTemplate property, UInt64 value) {
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
        /// <returns>System.String.</returns>
        /// <exception cref="Starcounter.JockeNotImplementedException"></exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public string GetValue(StringProperty property) {
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
        public string GetValue(IStringTemplate property) {
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
        public void SetValue(IStringTemplate property, string value) {
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
        public Listing GetValue(ListingProperty property) {
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
        public void SetValue(ListingProperty property, SqlResult data) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="templ">The templ.</param>
        /// <param name="data">The data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void SetValue(ListingProperty templ, Listing data) {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="property">The property.</param>
        /// <returns>Listing{``0}.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Listing<T> GetValue<T>(ListingProperty property) where T : App, new() {
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
        public void SetValue<T>(ListingProperty templ, SqlResult data) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="templ">The templ.</param>
        /// <param name="data">The data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void SetValue<T>(ListingProperty templ, Listing<T> data) where T : App, new() {
            throw new NotImplementedException();
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
        public void SetValue(IAppTemplate property, App value) {
#if QUICKTUPLE
            _Values[property.Index] = value;
#else
            throw new JockeNotImplementedException();
#endif
        }




    }
}
