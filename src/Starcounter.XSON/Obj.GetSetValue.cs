// ***********************************************************************
// <copyright file="App.GetSetValue.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Templates;
using System;
using System.ComponentModel;
using Starcounter.Advanced;

namespace Starcounter {

    public partial class Obj {
        private TVal Get<TTemplate, TVal>(TTemplate property) where TTemplate : TValue<TVal> {
            if (property.Bound)
                throw new NotImplementedException();
//                return property.GetBoundValue(this);

#if QUICKTUPLE
                return _Values[property.Index];
#else
                throw new NotImplementedException();
#endif
        }
        private void Set<TTemplate, TVal>(TTemplate property, TVal value) where TTemplate : TValue<TVal> {
            if (property.Bound) {
                throw new NotImplementedException();
//                property.SetBoundValue(this, value);
                //this.HasChanged(property);
                //return;
            }

#if QUICKTUPLE
            _Values[property.Index] = value;
#else
                    throw new NotImplementedException();
#endif
            this.HasChanged(property);
        }

        /// <summary>
        /// Gets the value for the specified template. If the property
        /// is bound the value will be retrived from the underlying dataobject.
        /// </summary>
        /// <param name="property">The template to retrieve the value for.</param>
        /// <returns>The value.</returns>
        public bool Get(TBool property) { return Get<TBool, bool>(property); }

        /// <summary>
        /// Sets the value for the specified template. If the property
        /// is bound the value will be set in the underlying dataobject.
        /// </summary>
        /// <param name="property">The template to set the value to.</param>
        /// <param name="value">The value to set.</param>
        public void Set(TBool property, bool value) { Set<TBool, bool>(property, value); }

        /// <summary>
        /// Gets the value for the specified template. If the property
        /// is bound the value will be retrived from the underlying dataobject.
        /// </summary>
        /// <param name="property">The template to retrieve the value for.</param>
        /// <returns>The value.</returns>
        public decimal Get(TDecimal property) { return Get<TDecimal, decimal>(property); }

        /// <summary>
        /// Sets the value for the specified template. If the property
        /// is bound the value will be set in the underlying dataobject.
        /// </summary>
        /// <param name="property">The template to set the value to.</param>
        /// <param name="value">The value to set.</param>
        public void Set(TDecimal property, decimal value) { Set<TDecimal, decimal>(property, value); }

        /// <summary>
        /// Gets the value for the specified template. If the property
        /// is bound the value will be retrived from the underlying dataobject.
        /// </summary>
        /// <param name="property">The template to retrieve the value for.</param>
        /// <returns>The value.</returns>
        public double Get(TDouble property) { return Get<TDouble, double>(property); }

        /// <summary>
        /// Sets the value for the specified template. If the property
        /// is bound the value will be set in the underlying dataobject.
        /// </summary>
        /// <param name="property">The template to set the value to.</param>
        /// <param name="value">The value to set.</param>
        public void Set(TDouble property, double value) { Set<TDouble, double>(property, value); }

        /// <summary>
        /// Gets the value for the specified template. If the property
        /// is bound the value will be retrived from the underlying dataobject.
        /// </summary>
        /// <param name="property">The template to retrieve the value for.</param>
        /// <returns>The value.</returns>
        public long Get(TLong property) { return Get<TLong, long>(property); }

        /// <summary>
        /// Sets the value for the specified template. If the property
        /// is bound the value will be set in the underlying dataobject.
        /// </summary>
        /// <param name="property">The template to set the value to.</param>
        /// <param name="value">The value to set.</param>
        public void Set(TLong property, long value) { Set<TLong, long>(property, value); }

        /// <summary>
        /// Gets the value for the specified template. If the property
        /// is bound the value will be retrived from the underlying dataobject.
        /// </summary>
        /// <param name="property">The template to retrieve the value for.</param>
        /// <returns>The value.</returns>
        public string Get(TString property) { return Get<TString, string>(property); }

        /// <summary>
        /// Sets the value for the specified template. If the property
        /// is bound the value will be set in the underlying dataobject.
        /// </summary>
        /// <param name="property">The template to set the value to.</param>
        /// <param name="value">The value to set.</param>
        public void Set(TString property, string value) { Set<TString, string>(property, value); }

        /// <summary>
        /// Gets the value for the specified template. If the property
        /// is bound the value will be retrived from the underlying dataobject.
        /// </summary>
        /// <param name="property">The template to retrieve the value for.</param>
        /// <returns>The value.</returns>
        public ulong Get(TOid property) { return Get<TOid, ulong>(property); }

        /// <summary>
        /// Gets the value for a given property in this Obj. This method returns all values boxed
        /// as a CLR object. Whenever possible, use the function specific to a type instead
        /// (i.e. non abstract value templates such as for example this.Get(BoolTemplate property).
        /// </summary>
        /// <param name="property">The template representing the property to read</param>
        /// <returns>The value of the property</returns>
        public object Get(TValue property) {
            if (property.Bound)
                throw new NotImplementedException();
//                    return (object)property.GetBoundValue(this);

#if QUICKTUPLE
                return _Values[property.Index];
#else
                throw new NotImplementedException();
#endif
        }

        /// <summary>
        /// Sets the value for a given property in this Obj. Whenever possible, use the 
        /// function specific to a type instead (i.e. non abstract value templates such 
        /// as for example this.Set(BoolTemplate property, bool value).
        /// </summary>
        /// <param name="property">The template representing the property to write</param>
        /// <param name="value">The value of the property</param>
        public void Set(TValue property, object value) {
            if (property.Bound) {
                throw new NotImplementedException();
//                    property.SetBoundValue(this, value);
                //this.HasChanged(property);
                //return;
            }

#if QUICKTUPLE
            _Values[property.Index] = value;
#else
                    throw new NotImplementedException();
#endif
            this.HasChanged(property);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public Obj Get(TObj property) {
            if (property.Bound)
                throw new NotImplementedException();
//                    return (Obj)property.GetBoundValue(this);

#if QUICKTUPLE
            return _Values[property.Index];
#else
            throw new NotImplementedException();
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="property"></param>
        /// <param name="value"></param>
        public void Set(TObj property, Obj value) {
            if (property.Bound) {
                throw new NotImplementedException();
                //                    property.SetBoundValue(this, value);
                //this.HasChanged(property);
                //return;
            }

#if QUICKTUPLE
            _Values[property.Index] = value;
#else
            throw new JockeNotImplementedException();
#endif
            this.HasChanged(property);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="property"></param>
        /// <returns></returns>
        public T Get<T>(TObj property) where T : Obj, new() {
#if QUICKTUPLE
            return (T)(_Values[property.Index]);
#else
            throw new NotImplementedException();
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="property"></param>
        /// <param name="value"></param>
        public void Set(TObj property, IBindable value) {
#if QUICKTUPLE
            Obj app = (Obj)property.CreateInstance(this);
            app.Data = value;
            _Values[property.Index] = app;
#else
            throw new JockeNotImplementedException();
#endif
            this.HasChanged(property);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public Arr Get(TObjArr property) {
#if QUICKTUPLE
            return _Values[property.Index];
#else
            throw new NotImplementedException();
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="property"></param>
        /// <param name="value"></param>
        public void Set(TObjArr property, Arr value) {
            Arr current = this.Get(property);
            if (current != null)
                current.Clear();

            value.InitializeAfterImplicitConversion(this, property);
#if QUICKTUPLE
            _Values[property.Index] = value;
#else
            throw new NotImplementedException();
#endif
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="property"></param>
        /// <param name="data"></param>
        public void Set(TObjArr property, Rows<object> data) {
            Arr current = _Values[property.Index];
            if (current != null) {
                current.Clear();
                current.notEnumeratedResult = data;
                current.InitializeAfterImplicitConversion(this, property);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="property"></param>
        /// <param name="data"></param>
        public void Set<T>(TObjArr property, Rows<object> data) where T : Obj, new() {
            Arr<T> newList;
            Arr<T> current = _Values[property.Index];
            if (current != null)
                current.Clear();

            newList = data;
            newList.InitializeAfterImplicitConversion(this, property);

            _Values[property.Index] = newList;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="property"></param>
        /// <returns></returns>
        public Arr<T> Get<T>(TObjArr property) where T : Obj, new() {
#if QUICKTUPLE
            return (Arr<T>)(_Values[property.Index]);
#else
            throw new NotImplementedException();
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="templ"></param>
        /// <param name="data"></param>
        public void Set<T>(TObjArr templ, Arr<T> data) where T : Obj, new() {
            Arr<T> current = _Values[templ.Index];
            if (current != null)
                current.Clear();

            data.InitializeAfterImplicitConversion(this, templ);
            _Values[templ.Index] = data;
        }
    }
}
