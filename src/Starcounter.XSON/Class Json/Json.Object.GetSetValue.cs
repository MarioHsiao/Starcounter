//// ***********************************************************************
//// <copyright file="App.GetSetValue.cs" company="Starcounter AB">
////     Copyright (c) Starcounter AB.  All rights reserved.
//// </copyright>
//// ***********************************************************************

using System;
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

        ///// <summary>
        ///// Gets the value for the specified template. If the property
        ///// is bound the value will be retrived from the underlying dataobject.
        ///// </summary>
        ///// <param name="property">The template to retrieve the value for.</param>
        ///// <returns>The value.</returns>
        //public ulong Get(TOid property) { return Get<ulong>(property); }

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
			if (this.trackChanges) {
				if (ArrayAddsAndDeletes == null) {
					ArrayAddsAndDeletes = new List<Change>();
				}
				ArrayAddsAndDeletes.Add(Change.Add(this.Parent, tarr, index, item));
				Dirtyfy();
                item.SetBoundValuesInTuple();
			}

            if (Parent != null)
			    Parent.ChildArrayHasAddedAnElement(tarr, index);
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        internal void CallHasReplacedElement(int index, Json item) {
            var tarr = (TObjArr)this.Template;
            if (this.trackChanges) {
                if (ArrayAddsAndDeletes == null) {
                    ArrayAddsAndDeletes = new List<Change>();
                }
                ArrayAddsAndDeletes.Add(Change.Update(this.Parent, tarr, index, item));
                Dirtyfy();
                item.SetBoundValuesInTuple();
            }

            if (Parent != null)
                Parent.ChildArrayHasReplacedAnElement(tarr, index);
        }

        internal void CallHasMovedElement(int fromIndex, int toIndex, Json item) {
            var tarr = (TObjArr)this.Template;
            if (this.trackChanges) {
                if (ArrayAddsAndDeletes == null) {
                    ArrayAddsAndDeletes = new List<Change>();
                }
                ArrayAddsAndDeletes.Add(Change.Move(this.Parent, tarr, fromIndex, toIndex, item));
                Dirtyfy();
                item.SetBoundValuesInTuple();
            }

            if (Parent != null)
                Parent.ChildArrayHasReplacedAnElement(tarr, toIndex);
        }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="index"></param>
		/// <param name="item"></param>
		internal void CallHasRemovedElement(int index, Json item) {
			var tarr = (TObjArr)this.Template;
            if (this.trackChanges) {
				if (ArrayAddsAndDeletes == null) {
					ArrayAddsAndDeletes = new List<Change>();
				}
				ArrayAddsAndDeletes.Add(Change.Remove(this.Parent, tarr, index, item));
				Dirtyfy();
			}

            if (Parent != null)
			    Parent.ChildArrayHasRemovedAnElement(tarr, index);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="property"></param>
		internal void CallHasChanged(TObjArr property, int index) {
            if (this.trackChanges)
                this.Dirtyfy();

            if (Parent != null)
			    this.Parent.ChildArrayHasReplacedAnElement(property, index);
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="property"></param>
        internal void CallHasChanged(TValue property) {
            if (this.trackChanges)
                this.Dirtyfy();
            this.HasChanged(property);
        }

        public bool BoolValue {
            get {
                if (!IsBool)
                    throw new InvalidOperationException("This instance does not have a bool value.");
                return Get((TBool)Template);
            }
            set {
                if (!IsBool)
                    throw new InvalidOperationException("This instance does not have a bool value.");
                Set((TBool)Template, value);
            }
        }

        public decimal DecimalValue {
            get {
                if (!IsDecimal)
                    throw new InvalidOperationException("This instance does not have a decimal value.");
                return Get((TDecimal)Template);
            }
            set {
                if (!IsDecimal)
                    throw new InvalidOperationException("This instance does not have a decimal value.");
                Set((TDecimal)Template, value);
            }
        }

        public double DoubleValue {
            get {
                if (!IsDouble)
                    throw new InvalidOperationException("This instance does not have a double value.");
                return Get((TDouble)Template);
            }
            set {
                if (!IsDouble)
                    throw new InvalidOperationException("This instance does not have a double value.");
                Set((TDouble)Template, value);
            }
        }

        public long IntegerValue {
            get {
                if (!IsInteger)
                    throw new InvalidOperationException("This instance does not have a integer value.");
                return Get((TLong)Template);
            }
            set {
                if (!IsInteger)
                    throw new InvalidOperationException("This instance does not have a integer value.");
                Set((TLong)Template, value);
            }
        }

        public string StringValue {
            get {
                if (!IsString)
                    throw new InvalidOperationException("This instance does not have a string value.");
                return Get((TString)Template);
            }
            set {
                if (!IsString)
                    throw new InvalidOperationException("This instance does not have a string value.");
                Set((TString)Template, value);
            }
        }

        public bool IsBool { 
            get { 
                return (Template != null) && (Template.TemplateTypeId == TemplateTypeEnum.Bool); 
            } 
        }

        public bool IsDecimal { 
            get { 
                return (Template != null) && (Template.TemplateTypeId == TemplateTypeEnum.Decimal); 
            } 
        }

        public bool IsDouble { 
            get { 
                return (Template != null) && (Template.TemplateTypeId == TemplateTypeEnum.Double); 
            } 
        }

        public bool IsInteger { 
            get { 
                return (Template != null) && (Template.TemplateTypeId == TemplateTypeEnum.Long); 
            } 
        }

        public bool IsString { 
            get { 
                return (Template != null) && (Template.TemplateTypeId == TemplateTypeEnum.String); 
            } 
        }

        public bool IsObject { 
            get { 
                return (Template != null) && (Template.TemplateTypeId == TemplateTypeEnum.Object); 
            } 
        }
	}
}
