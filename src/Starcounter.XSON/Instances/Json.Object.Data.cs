// ***********************************************************************
// <copyright file="Obj.IBindable.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using Starcounter.Templates;
using Starcounter.Advanced;
using System.Collections;
using Starcounter.Internal.XSON;

namespace Starcounter {
    partial class Json {
        /// <summary>
        /// An Obj can be bound to a data object. This makes the Obj reflect the data in the
        /// underlying bound object. This is common in database applications where Json messages
        /// or view models (Puppets) are often associated with database objects. I.e. a person form might
        /// reflect a person database object (Entity).
        /// </summary>
        private IBindable _data;


        /// <summary>
        /// 
        /// </summary>
        private IBindable DataAsBindable {
            get {
                return (IBindable)_data;
            }
        }

        /// <summary>
        /// Gets or sets the bound (underlying) data object (often a database Entity object). This enables
        /// the Obj to reflect the values of the bound object. The values are matched by property names by default.
        /// When you declare an Obj using generics, be sure to specify the type of the bound object in the class declaration.
        /// </summary>
        /// <value>The bound data object (often a database Entity)</value>
        public IBindable Data {
            get {
                return _data;
            }
            set {
                if (IsArray) {
                    throw new NotImplementedException();
                }
                else {
                    if (Template == null) {
                        this.CreateDynamicTemplate(); // If there is no template, we'll create a template
                    }
                    InternalSetData((IBindable)value, (Schema)Template);
                }
            }
        }


		/// <summary>
		/// Gets the bound value from the dataobject.
		/// </summary>
		/// <remarks>
		/// This method assumes that the cached binding on the template 
		/// is correct and will not verify it.
		/// </remarks>
		/// <typeparam name="TVal"></typeparam>
		/// <param name="template"></param>
		/// <returns></returns>
        internal TVal GetBound<TVal>(Property<TVal> template) {
            IBindable data = DataAsBindable;
            if (data == null)
                return default(TVal);
            return ((DataValueBinding<TVal>)template.dataBinding).Get(data);
        }

		/// <summary>
		/// Sets the value to the dataobject.
		/// </summary>
		/// <remarks>
		/// This method assumes that the cached binding on the template 
		/// is correct and will not verify it.
		/// </remarks>
		/// <param name="template"></param>
		/// <param name="value"></param>
        internal void SetBound<TVal>(Property<TVal> template, TVal value) {
            IBindable data = DataAsBindable;
            if (data == null)
                return;
            ((DataValueBinding<TVal>)template.dataBinding).Set(data, value);
        }

		internal object GetBound(TValue template) {
            IBindable data = DataAsBindable;
			if (data == null)
				return null;
			
			return template.GetBoundValueAsObject(this);
		}

		internal void SetBound(TValue template, object value) {
            IBindable data = DataAsBindable;
			if (data == null)
				return;

			template.SetBoundValueAsObject(this, value);
		}

		/// <summary>
		/// Gets the bound value from the dataobject.
		/// </summary>
		/// <remarks>
		/// This method assumes that the cached binding on the template 
		/// is correct and will not verify it.
		/// </remarks>
		/// <param name="template"></param>
		/// <returns></returns>
        internal IEnumerable GetBound(TObjArr template) {
            IBindable data = DataAsBindable;
            if (data == null)
                return default(Rows<object>);

            return ((DataValueBinding<IEnumerable>)template.dataBinding).Get(data);
        }

		/// <summary>
		/// Gets the bound value from the dataobject.
		/// </summary>
		/// <remarks>
		/// This method assumes that the cached binding on the template 
		/// is correct and will not verify it.
		/// </remarks>
		/// <param name="template"></param>
		/// <returns></returns>
        internal IBindable GetBound(Schema template) {
            IBindable data = DataAsBindable;
            if (data == null)
                return null;
            return ((DataValueBinding<IBindable>)template.dataBinding).Get(data);
        }

		/// <summary>
		/// Sets the value to the dataobject.
		/// </summary>
		/// <remarks>
		/// This method assumes that the cached binding on the template 
		/// is correct and will not verify it.
		/// </remarks>
		/// <param name="template"></param>
		/// <param name="value"></param>
        internal void SetBound(Schema template, IBindable value) {
            IBindable data = DataAsBindable;
            if (data == null)
                return;
			((DataValueBinding<IBindable>)template.dataBinding).Set(data, value);
        }

        /// <summary>
        /// Sets the underlying data object and refreshes all bound values.
        /// This function does not check for a valid transaction as the 
        /// public Data-property does.
        /// </summary>
        /// <param name="data">The bound data object (usually an Entity)</param>
        protected virtual void InternalSetData(IBindable data, Schema template ) {
            this._data = data;

			if (template.Bound != Bound.No) {
				var parent = ((Json)this.Parent);
				if (parent != null && template.UseBinding(parent.DataAsBindable)) {
					((DataValueBinding<IBindable>)template.dataBinding).Set(parent.DataAsBindable, data);
				}
			}

			InitBoundArrays(template);
            OnData();
        }

        /// <summary>
        /// Initializes bound arrays when a new dataobject is set.
        /// </summary>
        private void InitBoundArrays(Schema template) {
            TObjArr child;
            for (Int32 i = 0; i < template.Properties.Count; i++) {
                child = template.Properties[i] as TObjArr;
                if (child != null && child.Bound != Bound.No) {
					if (_data != null) {
						child.UseBinding(DataAsBindable);
						Refresh(child);
					} else {
						var arr = (Arr<Json>)Get(child);
						arr.Clear();
					}
                }
            }
        }

        /// <summary>
        /// Called after the Data property is set.
        /// </summary>
        protected virtual void OnData() {
        }
    }
}