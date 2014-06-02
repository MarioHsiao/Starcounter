// ***********************************************************************
// <copyright file="Obj.IBindable.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using Starcounter.Templates;
using Starcounter.Advanced;
using Starcounter.Advanced.XSON;
using System.Collections;
using Starcounter.Internal.XSON;

namespace Starcounter {
    partial class Json {
        /// <summary>
        /// 
        /// </summary>
        internal IBindable DataAsBindable {
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
        public object Data {
            get {
                return _data;
            }
            set {
                this.AddInScope<Json, object>((j, v) => {
                    if (j.IsArray) {
                        j._PendingEnumeration = true;
                        j._data = (IEnumerable)v;
                        j.Array_InitializeAfterImplicitConversion((Json)j.Parent, (TObjArr)j.Template);
                    } else {
                        if (j.Template == null) {
                            j.CreateDynamicTemplate(); // If there is no template, we'll create a template
                        }
                        j.InternalSetData(v, (TObject)j.Template, false);
                    }
                },
                this, value);
            }
        }

        internal void AttachData(object data) {
            InternalSetData(data, (TObject)Template, true);
        }

        /// <summary>
        /// For public API functions that does not operate on templates, this
        /// method should be used instead of a simple type cast to provide a 
        /// better error message for the developer.
        /// </summary>
        /// <returns>This as a Json object</returns>
        private Json AssertIsObject() {
            if (IsArray) {
                throw new Exception("You cannot use named properties on array Json objects");
            }
            return this as Json;
        }

        /// <summary>
        /// Sets the underlying data object and refreshes all bound values.
        /// This function does not check for a valid transaction as the 
        /// public Data-property does.
        /// </summary>
        /// <param name="data">The bound data object (usually an Entity)</param>
        protected virtual void InternalSetData(object data, TObject template, bool readOperation ) {
            this._data = data;

			if (template != null) {
				if (template.BindingStrategy != BindingStrategy.Unbound) {
					var parent = ((Json)this.Parent);
					if (!readOperation && parent != null 
						&& template.UseBinding(parent) 
						&& template.BoundSetter != null) {
						template.BoundSetter(parent, data);
					}
				}

                if (_data == null)
                    ClearBoundValues(template);

				InitBoundArrays(template);
			}
            OnData();
        }

        /// <summary>
        /// If a dataobject is set to null we need to clear out all already bound values since
        /// we treat a dataobject that is null as unbound json. So we loop through all properties
        /// and if they have an existing binding we invalidate it and add a change to the session
        /// if it exists.
        /// </summary>
        /// <param name="template"></param>
        private void ClearBoundValues(TObject template) {
            TValue child;

            for (Int32 i = 0; i < template.Properties.Count; i++) {
                child = template.Properties[i] as TValue;

                if (child.BindingStrategy != BindingStrategy.Unbound && !child.isVerifiedUnbound) {
                    child.InvalidateBoundGetterAndSetter();
                    child.SetDefaultValue(this);
                    if (Session.Current != null)
                        Session.Current.UpdateValue(this, child);
                }
            }
        }

        /// <summary>
        /// Initializes bound arrays when a new dataobject is set.
        /// </summary>
        private void InitBoundArrays(TObject template) {
            TObjArr child;
            for (Int32 i = 0; i < template.Properties.Count; i++) {
                child = template.Properties[i] as TObjArr;
                if (child != null && child.BindingStrategy != BindingStrategy.Unbound) {
					if (_data != null) {
						if (child.UseBinding(this))
							Refresh(child);
					} else {
                        var thisj = AssertIsObject();
						var arr = (Json)child.Getter(thisj);
						((IList)arr).Clear();
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