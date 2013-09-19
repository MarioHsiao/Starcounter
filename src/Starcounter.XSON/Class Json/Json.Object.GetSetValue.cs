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
using Starcounter.Internal.XSON;

namespace Starcounter {

    public partial class Json {


        public object this[string key] {
            get {
                var template = (TObject)this.Template;
                var prop = template.Properties[key];
                if (prop == null) {
                    return null;
                }
                return this[prop.TemplateIndex];
            }
            set {
                var template = (TObject)this.Template;
                var prop = template.Properties[key];
                if (prop == null) {
                    Type type;
                    if (value == null) {
                        type = typeof(Json);
                    }
                    else {
                        type = value.GetType();
                    }
                    template.OnSetUndefinedProperty(key, type);
                    this[key] = value;
                    return;
                }
                this[prop.TemplateIndex] = value;
            }
        }

        public object Get(TValue property) {
            return this[property.TemplateIndex];
        }

        internal object Wrap(object ret, TValue property) {
            return ret;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TVal"></typeparam>
        /// <param name="property"></param>
        /// <returns></returns>
        public TVal Get<TVal>(Property<TVal> property) {
            return (TVal)Get((TValue)property);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TVal"></typeparam>
        /// <param name="property"></param>
        /// <param name="value"></param>
        public void Set<TVal>(Property<TVal> property, TVal value) {
            this[property.TemplateIndex] = value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="property"></param>
        internal void _CallHasChanged(TValue property) {
            if (Session != null) {
                if (!_BrandNew) {
                   // _Values.SetReplacedFlagAt(property.TemplateIndex,true);
                    this.Dirtyfy();
                }
            }
            //this.HasChanged(property);
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
                return (JsonType)this[property.TemplateIndex];
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public Json Get(TObject property) {
            return Get<Json>(property);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="property"></param>
        /// <param name="value"></param>
        public void Set(TObject property, Json value) {
            this[property.TemplateIndex] = value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="property"></param>
        /// <param name="value"></param>
        public void Set(TObject property, IBindable value) {
            this[property.TemplateIndex] = value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public Arr<ElementType> Get<ElementType>(TArray<ElementType> property) 
            where ElementType : Json, new()
        {
            return (Arr<ElementType>)this[property.TemplateIndex];
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public Json Get( TObjArr property) {
            return (Json)this[property.TemplateIndex];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="property"></param>
        /// <param name="value"></param>
        public void Set(TObjArr property, Json value) {
            this[property.TemplateIndex] = value;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="property"></param>
        /// <param name="data"></param>
        public void Set(TObjArr property, IEnumerable data) {
            var current = (Json)list[property.TemplateIndex];
            if (current != null) {
                current.Clear();
                current._PendingEnumeration = true;
                current._data = data;
                current.Array_InitializeAfterImplicitConversion(this, property);
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
            var vals = list;
            var current = (Arr<T>)vals[property.TemplateIndex];
            if (current != null)
                current.Clear();

            newList = data;
            newList.Array_InitializeAfterImplicitConversion(this, property);

            vals[property.TemplateIndex] = newList;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="property"></param>
        /// <returns></returns>
        public Arr<T> Get<T>(TObjArr property) where T : Json, new() {
            return (Arr<T>)(this[property.TemplateIndex]);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="templ"></param>
        /// <param name="data"></param>
        public void Set<T>(TObjArr templ, Arr<T> data) where T : Json, new() {
            this[templ.TemplateIndex] = data;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>Action.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Action Get(TTrigger property) {
#if QUICKTUPLE
            return (Action)this[property.TemplateIndex];
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
            this[property.TemplateIndex] = value;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        internal void _CallHasAddedElement(int index, Json item) {
            var tarr = (TObjArr)this.Template;
            if (Session != null) {
                if (ArrayAddsAndDeletes == null) {
                    ArrayAddsAndDeletes = new List<Change>();
                }
                ArrayAddsAndDeletes.Add(Change.Update((Json)this.Parent, tarr, index));
                Dirtyfy();
            }
            Parent.ChildArrayHasAddedAnElement(tarr, index);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        internal void _CallHasRemovedElement(int index) {
            var tarr = (TObjArr)this.Template;
            if (Session != null) {
                if (ArrayAddsAndDeletes == null) {
                    ArrayAddsAndDeletes = new List<Change>();
                }
                ArrayAddsAndDeletes.Remove(Change.Add((Json)this.Parent, tarr, index));
                Dirtyfy();
            }
            Parent.ChildArrayHasRemovedAnElement(tarr, index);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="property"></param>
        internal void _CallHasChanged(TObjArr property, int index) {
            if (Session != null) {
                if (!_BrandNew) {
                    //                    (_Values[index] as Json)._Dirty = true;
                    this.Dirtyfy();
                }
            }
            this.Parent.ChildArrayHasReplacedAnElement(property, index);
        }
    }
}
