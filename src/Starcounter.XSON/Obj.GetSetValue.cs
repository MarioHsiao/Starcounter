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
                return property.GetBoundValue(this);

#if QUICKTUPLE
                return _Values[property.Index];
#else
                throw new NotImplementedException();
#endif
        }
        private void Set<TTemplate, TVal>(TTemplate property, TVal value) where TTemplate : TValue<TVal> {
            if (property.Bound) {
                property.SetBoundValue(this, value);
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
        /// Gets or sets the value for the specified template. If the property
        /// is bound the value will be retrived or set in the underlying dataobject.
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public bool this[TBool property] {
            get { return Get<TBool, bool>(property); }
            set { Set<TBool, bool>(property, value); }
        }

        /// <summary>
        /// Gets or sets the value for the specified template. If the property
        /// is bound the value will be retrived or set in the underlying dataobject.
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public decimal this[TDecimal property] {
            get { return Get<TDecimal, decimal>(property); }
            set { Set<TDecimal, decimal>(property, value); }
        }

        /// <summary>
        /// Gets or sets the value for the specified template. If the property
        /// is bound the value will be retrived or set in the underlying dataobject.
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public double this[TDouble property] {
            get { return Get<TDouble, double>(property); }
            set { Set<TDouble, double>(property, value); }
        }

        /// <summary>
        /// Gets or sets the value for the specified template. If the property
        /// is bound the value will be retrived or set in the underlying dataobject.
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public long this[TLong property] {
            get { return Get<TLong, long>(property); }
            set { Set<TLong, long>(property, value); }
        }

        /// <summary>
        /// Gets or sets the value for the specified template. If the property
        /// is bound the value will be retrived or set in the underlying dataobject.
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public String this[TString property] {
            get { return Get<TString, string>(property); }
            set { Set<TString, string>(property, value); }
        }

        /// <summary>
        /// Gets the value for the specified template. If the property
        /// is bound the value will be retrived from the underlying dataobject.
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public UInt64 this[TOid property] {
            get { return Get<TOid, UInt64>(property); }
        }

        /// <summary>
        /// Gets or sets the value for a given property in this Obj. This method returns all values boxed
        /// as a CLR object. Whenever possible, use the function specific to a type instead
        /// (i.e. non abstract value templates such as for example this[BoolTemplate property].
        /// </summary>
        /// <param name="property">The template representing the property to read</param>
        /// <returns>The value of the property</returns>
        public object this[TValue property] {
            get {
                if (property.Bound)
                    return (object)property.GetBoundValue(this);

                #if QUICKTUPLE
                    return _Values[property.Index];
                #else
                    throw new NotImplementedException();
                #endif
            }
            set {
                if (property.Bound) {
                    property.SetBoundValue(this, value);
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
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public Arr this[TObjArr property] {
            get {
#if QUICKTUPLE
                return _Values[property.Index];
#else
                throw new NotImplementedException();
#endif
            }
            set {
                Arr current = this[property];
                if (current != null)
                    current.Clear();

                value.InitializeAfterImplicitConversion(this, property);
#if QUICKTUPLE
                _Values[property.Index] = value;
#else
                throw new NotImplementedException();
#endif
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public Obj this[TObj property] {
            get {
                if (property.Bound)
                    return (Obj)property.GetBoundValue(this);

#if QUICKTUPLE
                return _Values[property.Index];
#else
                throw new NotImplementedException();
#endif
            }
            set {
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
        /// <param name="templ"></param>
        /// <param name="data"></param>
        public void Set<T>(TObjArr templ, Rows<object> data) where T : Obj, new() {
            Arr<T> newList;
            Arr<T> current = _Values[templ.Index];
            if (current != null)
                current.Clear();

            newList = data;
            newList.InitializeAfterImplicitConversion(this, templ);

            _Values[templ.Index] = newList;
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
    }
}
