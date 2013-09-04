// ***********************************************************************
// <copyright file="App.GetSetValue.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Templates;
using System;
using System.ComponentModel;
using Starcounter.Advanced;
using Starcounter.Advanced.XSON;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;

namespace Starcounter {

    public partial class Json {

        public object Get(TValue property) {
			if (property.UseBinding(DataAsBindable))
                return GetBound(property);
            return Values[property.TemplateIndex];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TVal"></typeparam>
        /// <param name="property"></param>
        /// <returns></returns>
        public TVal Get<TVal>(Property<TVal> property) {
            if (property.UseBinding(DataAsBindable))
                return GetBound(property);

#if QUICKTUPLE
                return (TVal)Values[property.TemplateIndex];
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
        public void Set<TVal>(Property<TVal> property, TVal value) {
            if (property.UseBinding(DataAsBindable)) {
                SetBound(property, value);
                this._CallHasChanged(property);
                return;
            }

#if QUICKTUPLE
            Values[property.TemplateIndex] = value;
#else
                    throw new NotImplementedException();
#endif
            this._CallHasChanged(property);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="property"></param>
        internal void _CallHasChanged(TValue property) {
            if (Session != null) {
                if (!_BrandNew) {
                    _DirtyValues[property.TemplateIndex] = true;
                    this.Dirtyfy();
                }
            }
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
        public decimal Get(Property<decimal> property) { return Get<decimal>(property); }

        /// <summary>
        /// Sets the value for the specified template. If the property
        /// is bound the value will be set in the underlying dataobject.
        /// </summary>
        /// <param name="property">The template to set the value to.</param>
        /// <param name="value">The value to set.</param>
        public void Set(Property<decimal> property, decimal value) { Set<decimal>(property, value); }

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
//                return _Values[property.TemplateIndex];
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
//            _Values[property.TemplateIndex] = value;
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
        public JsonType Get<JsonType>(TObject property)
            where JsonType : Json, new() {
            //IBindable data = null;
            //if (property.Bound)
            //    data = GetBound(property);

#if QUICKTUPLE
            return (JsonType)Values[property.TemplateIndex];
#else
            throw new NotImplementedException();
#endif
        }        /// <summary>
        /// 
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public Json Get(TObject property) {
            //IBindable data = null;
            //if (property.Bound)
            //    data = GetBound(property);

#if QUICKTUPLE
            return (Json)Values[property.TemplateIndex];
#else
            throw new NotImplementedException();
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="property"></param>
        /// <param name="value"></param>
        public void Set(TObject property, Json value) {
			if (value != null) {
				value.Parent = this;

				if (property.UseBinding(DataAsBindable))
					SetBound(property, value.Data);

				value._cacheIndexInArr = property.TemplateIndex;
			}
#if QUICKTUPLE
            var vals = Values;
            var i = property.TemplateIndex;
            var oldValue = (Json)vals[i];
            if (oldValue != null) {
                oldValue.SetParent(null);
				oldValue._cacheIndexInArr = -1;
            }
            vals[i] = value;
#else
            throw new NotImplementedException();
#endif
            this._CallHasChanged(property);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="property"></param>
        /// <param name="value"></param>
        public void Set(TObject property, IBindable value) {
            if (property.UseBinding(DataAsBindable))
                SetBound(property, value);

#if QUICKTUPLE
            Json app = (Json)property.CreateInstance(this);
            app.Data = value;
            Values[property.TemplateIndex] = app;
#else
            throw new NotImplementedException();
#endif
            this._CallHasChanged(property);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public Arr<ElementType> Get<ElementType>(TArray<ElementType> property) 
            where ElementType : Json, new()
        {
#if QUICKTUPLE
            return (Arr<ElementType>)Values[property.TemplateIndex];
#else
            throw new NotImplementedException();
#endif
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public Arr Get( TObjArr property) {
#if QUICKTUPLE
            return (Arr)Values[property.TemplateIndex];
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
            if (value != null)
                value.Parent = this;
            var i = property.TemplateIndex;
            var vals = Values;
            var oldValue = (Arr)vals[i]; //this.Get(property);
            if (oldValue != null) {
                oldValue.InternalClear();
//                oldValue.Clear();
                oldValue.SetParent(null); 
            }

            value.InitializeAfterImplicitConversion(this, property);
#if QUICKTUPLE
            vals[i] = value;
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
            var current = (Arr)Values[property.TemplateIndex];
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
        public void Set<T>(TObjArr property, Rows<object> data) where T : Json, new() {
            Arr<T> newList;
            var vals = Values;
            var current = (Arr<T>)vals[property.TemplateIndex];
            if (current != null)
                current.Clear();

            newList = data;
            newList.InitializeAfterImplicitConversion(this, property);

            vals[property.TemplateIndex] = newList;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="property"></param>
        /// <returns></returns>
        public Arr<T> Get<T>(TObjArr property) where T : Json, new() {
#if QUICKTUPLE
            return (Arr<T>)(Values[property.TemplateIndex]);
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
        public void Set<T>(TObjArr templ, Arr<T> data) where T : Json, new() {
            var vals = Values;
            var current = (Arr<T>)vals[templ.TemplateIndex];
            if (current != null)
                current.Clear();

            data.InitializeAfterImplicitConversion(this, templ);
            vals[templ.TemplateIndex] = data;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>Action.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Action Get(TTrigger property) {
#if QUICKTUPLE
            return (Action)Values[property.TemplateIndex];
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
            Values[property.TemplateIndex] = value;
#else
            throw new NotImplementedException();
#endif
        }
    }
}
