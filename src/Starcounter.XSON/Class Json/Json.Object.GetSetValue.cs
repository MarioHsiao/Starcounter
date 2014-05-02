//// ***********************************************************************
//// <copyright file="App.GetSetValue.cs" company="Starcounter AB">
////     Copyright (c) Starcounter AB.  All rights reserved.
//// </copyright>
//// ***********************************************************************

using System.Collections;
using System.Collections.Generic;
using Starcounter.Internal.XSON;
using Starcounter.Templates;

namespace Starcounter {
	public partial class Json {
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="TVal"></typeparam>
		/// <param name="property"></param>
		/// <returns></returns>
		public TVal Get<TVal>(Property<TVal> property) {
			return property.Getter(this);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="TVal"></typeparam>
		/// <param name="property"></param>
		/// <param name="value"></param>
		public void Set<TVal>(Property<TVal> property, TVal value) {
			property.Setter(this, value);
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

		/// <summary>
		/// 
		/// </summary>
		/// <param name="property"></param>
		/// <returns></returns>
		public JsonType Get<JsonType>(TObject property)
			where JsonType : Json, new() {
				return (JsonType)property.Getter(this);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="property"></param>
		/// <returns></returns>
		public Json Get(TObject property) {
			return property.Getter(this);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="property"></param>
		/// <param name="value"></param>
		public void Set(TObject property, Json value) {
			property.Setter(this, value);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="property"></param>
		/// <param name="value"></param>
		public void Set(TObject property, object value) {
			property.SetValue(this, value);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="property"></param>
		/// <returns></returns>
		public Arr<ElementType> Get<ElementType>(TArray<ElementType> property)
			where ElementType : Json, new() {
				return property.Getter(this);
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="property"></param>
		/// <returns></returns>
		public Json Get(TObjArr property) {
			return property.Getter(this);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="property"></param>
		/// <param name="value"></param>
		public void Set(TObjArr property, Json value) {
			property.Setter(this, value);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="property"></param>
		/// <param name="data"></param>
		public void Set(TObjArr property, IEnumerable data) {
			property.SetValue(this, data);			
		}

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="property"></param>
		/// <param name="data"></param>
		public void Set<T>(TObjArr property, Rows<object> data) where T : Json, new() {
			property.SetValue(this, data);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="property"></param>
		/// <returns></returns>
		public Arr<T> Get<T>(TObjArr property) where T : Json, new() {
			return (Arr<T>)property.Getter(this);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="property"></param>
		/// <param name="data"></param>
		public void Set<T>(TObjArr property, Arr<T> data) where T : Json, new() {
			property.Setter(this, data);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="index"></param>
		/// <param name="item"></param>
		internal void CallHasAddedElement(int index, Json item) {
			var tarr = (TObjArr)this.Template;
			if (Session != null) {
				if (ArrayAddsAndDeletes == null) {
					ArrayAddsAndDeletes = new List<Change>();
				}
				ArrayAddsAndDeletes.Add(Change.Add((Json)this.Parent, tarr, index));
				Dirtyfy();
				item.SetBoundValuesInTuple();
			}
			Parent.ChildArrayHasAddedAnElement(tarr, index);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="index"></param>
		/// <param name="item"></param>
		internal void CallHasRemovedElement(int index) {
			var tarr = (TObjArr)this.Template;
			if (Session != null) {
				if (ArrayAddsAndDeletes == null) {
					ArrayAddsAndDeletes = new List<Change>();
				}
				ArrayAddsAndDeletes.Add(Change.Remove((Json)this.Parent, tarr, index));
				Dirtyfy();
			}
			Parent.ChildArrayHasRemovedAnElement(tarr, index);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="property"></param>
		internal void CallHasChanged(TObjArr property, int index) {
			if (HasBeenSent) {
                this.Dirtyfy();
			}
			this.Parent.ChildArrayHasReplacedAnElement(property, index);
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="property"></param>
        internal void CallHasChanged(TValue property) {
            if (_isStatefulObject &&  Session != null) {
                if (HasBeenSent) {
                    // _Values.SetReplacedFlagAt(property.TemplateIndex,true);
                    this.Dirtyfy();
                }
            }
            this.HasChanged(property);
        }

	}
}
