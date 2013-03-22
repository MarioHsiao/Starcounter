// ***********************************************************************
// <copyright file="App.GetSetValue.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Templates;
using System;
using System.ComponentModel;
using Starcounter.Advanced;
using Starcounter.XSON;
using System.Collections.Generic;
using System.Collections;

namespace Starcounter {

    public partial class Obj {
        internal TVal GetBound<TVal>(TValue<TVal> template) {
            IBindable data = this.Data;
            if (data == null)
                return default(TVal);
            return template.GetBinding(data).Get(data);
        }

        internal void SetBound<TVal>(TValue<TVal> template, TVal value) {
            IBindable data = this.Data;
            if (data == null)
                return;
            template.GetBinding(data).Set(data, value);
        }

        //internal object GetBound(TValue template) {
        //    return template.GetBoundValueAsObject(this);
        //}

        //internal void SetBound(TValue template, object value) {
        //    template.SetBoundValueAsObject(this, value);
        //}

        internal IEnumerable GetBound(TObjArr template) {
            IBindable data = this.Data;
            if (data == null)
                return default(Rows<object>);

            return template.GetBinding(data).Get(data);
        }

        internal IBindable GetBound(TObj template) {
            IBindable data = this.Data;
            if (data == null)
                return null;
            return template.GetBinding(data).Get(data);
        }

        internal void SetBound(TObj template, IBindable value) {
            IBindable data = this.Data;
            if (data == null)
                return;
            template.GetBinding(data).Set(data, value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TVal"></typeparam>
        /// <param name="property"></param>
        /// <returns></returns>
        public TVal Get<TVal>(TValue<TVal> property) {
            if (property.Bound)
                return GetBound(property);

#if QUICKTUPLE
                return _Values[property.Index];
#else
                throw new NotImplementedException();
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TVal"></typeparam>
        /// <param name="property"></param>
        /// <param name="value"></param>
        public void Set<TVal>(TValue<TVal> property, TVal value) {
            if (property.Bound) {
                SetBound(property, value);
                this.HasChanged(property);
                return;
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
        public bool Get(TBool property) { return Get<bool>(property); }

        /// <summary>
        /// Sets the value for the specified template. If the property
        /// is bound the value will be set in the underlying dataobject.
        /// </summary>
        /// <param name="property">The template to set the value to.</param>
        /// <param name="value">The value to set.</param>
        public void Set(TBool property, bool value) { Set<bool>(property, value); }

        /// <summary>
        /// Gets the value for the specified template. If the property
        /// is bound the value will be retrived from the underlying dataobject.
        /// </summary>
        /// <param name="property">The template to retrieve the value for.</param>
        /// <returns>The value.</returns>
        public decimal Get(TDecimal property) { return Get<decimal>(property); }

        /// <summary>
        /// Sets the value for the specified template. If the property
        /// is bound the value will be set in the underlying dataobject.
        /// </summary>
        /// <param name="property">The template to set the value to.</param>
        /// <param name="value">The value to set.</param>
        public void Set(TDecimal property, decimal value) { Set<decimal>(property, value); }

        /// <summary>
        /// Gets the value for the specified template. If the property
        /// is bound the value will be retrived from the underlying dataobject.
        /// </summary>
        /// <param name="property">The template to retrieve the value for.</param>
        /// <returns>The value.</returns>
        public double Get(TDouble property) { return Get<double>(property); }

        /// <summary>
        /// Sets the value for the specified template. If the property
        /// is bound the value will be set in the underlying dataobject.
        /// </summary>
        /// <param name="property">The template to set the value to.</param>
        /// <param name="value">The value to set.</param>
        public void Set(TDouble property, double value) { Set<double>(property, value); }

        /// <summary>
        /// Gets the value for the specified template. If the property
        /// is bound the value will be retrived from the underlying dataobject.
        /// </summary>
        /// <param name="property">The template to retrieve the value for.</param>
        /// <returns>The value.</returns>
        public long Get(TLong property) { return Get<long>(property); }

        /// <summary>
        /// Sets the value for the specified template. If the property
        /// is bound the value will be set in the underlying dataobject.
        /// </summary>
        /// <param name="property">The template to set the value to.</param>
        /// <param name="value">The value to set.</param>
        public void Set(TLong property, long value) { Set<long>(property, value); }

        /// <summary>
        /// Gets the value for the specified template. If the property
        /// is bound the value will be retrived from the underlying dataobject.
        /// </summary>
        /// <param name="property">The template to retrieve the value for.</param>
        /// <returns>The value.</returns>
        public string Get(TString property) { return Get<string>(property); }

        /// <summary>
        /// Sets the value for the specified template. If the property
        /// is bound the value will be set in the underlying dataobject.
        /// </summary>
        /// <param name="property">The template to set the value to.</param>
        /// <param name="value">The value to set.</param>
        public void Set(TString property, string value) { Set<string>(property, value); }

        /// <summary>
        /// Gets the value for the specified template. If the property
        /// is bound the value will be retrived from the underlying dataobject.
        /// </summary>
        /// <param name="property">The template to retrieve the value for.</param>
        /// <returns>The value.</returns>
        public ulong Get(TOid property) { return Get<ulong>(property); }

//        /// <summary>
//        /// Gets the value for a given property in this Obj. This method returns all values boxed
//        /// as a CLR object. Whenever possible, use the function specific to a type instead
//        /// (i.e. non abstract value templates such as for example this.Get(BoolTemplate property).
//        /// </summary>
//        /// <param name="property">The template representing the property to read</param>
//        /// <returns>The value of the property</returns>
//        public object Get(TValue property) {
//            if (property.Bound)
//                return GetBound(property);

//#if QUICKTUPLE
//                return _Values[property.Index];
//#else
//                throw new NotImplementedException();
//#endif
//        }

//        /// <summary>
//        /// Sets the value for a given property in this Obj. Whenever possible, use the 
//        /// function specific to a type instead (i.e. non abstract value templates such 
//        /// as for example this.Set(BoolTemplate property, bool value).
//        /// </summary>
//        /// <param name="property">The template representing the property to write</param>
//        /// <param name="value">The value of the property</param>
//        public void Set(TValue property, object value) {
//            if (property.Bound) {
//                SetBound(property, value);
//                this.HasChanged(property);
//                return;
//            }

//#if QUICKTUPLE
//            _Values[property.Index] = value;
//#else
//                    throw new NotImplementedException();
//#endif
//            this.HasChanged(property);
//        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public Obj Get(TObj property) {
            //IBindable data = null;
            //if (property.Bound)
            //    data = GetBound(property);

#if QUICKTUPLE
            Obj v = _Values[property.Index];
            //if (v.Data != data) {
            //    v.Data = data;
            //}
            return v;
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
            value.Parent = this;

            if (property.Bound)
                SetBound(property, value.Data);
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
        /// <typeparam name="T"></typeparam>
        /// <param name="property"></param>
        /// <returns></returns>
        public T Get<T>(TObj property) where T : Obj, new() {
#if QUICKTUPLE
            return (T)Get(property);
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
            if (property.Bound)
                SetBound(property, value);

#if QUICKTUPLE
            Obj app = (Obj)property.CreateInstance(this);
            app.Data = value;
            _Values[property.Index] = app;
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
        public void Set(TObjArr property, IEnumerable data) {
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

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>Action.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Action Get(TTrigger property) {
#if QUICKTUPLE
            return _Values[property.Index];
#else
            throw new NotImplementedException();
#endif
        }

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void Set(TTrigger property, Action value) {
#if QUICKTUPLE
            _Values[property.Index] = value;
#else
            throw new NotImplementedException();
#endif
        }
    }
}
