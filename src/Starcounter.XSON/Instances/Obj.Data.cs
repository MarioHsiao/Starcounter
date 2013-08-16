// ***********************************************************************
// <copyright file="Obj.IBindable.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using Starcounter.Templates;
using Starcounter.Advanced;
using System.Collections;

namespace Starcounter {

    partial class Obj {

        /// <summary>
        /// An Obj can be bound to a data object. This makes the Obj reflect the data in the
        /// underlying bound object. This is common in database applications where Json messages
        /// or view models (Puppets) are often associated with database objects. I.e. a person form might
        /// reflect a person database object (Entity).
        /// </summary>
        private IBindable _data;



        /// <summary>
        /// Gets or sets the bound (underlying) data object (often a database Entity object). This enables
        /// the Obj to reflect the values of the bound object. The values are matched by property names by default.
        /// When you declare an Obj using generics, be sure to specify the type of the bound object in the class declaration.
        /// </summary>
        /// <value>The bound data object (often a database Entity)</value>
        public IBindable Data {
            get {
                return (IBindable)_data;
            }
            set {
                if (Template == null) {
                    this.CreateDynamicTemplate(); // If there is no template, we'll create a template
                }
                InternalSetData(value);
            }
        }

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
        /// Sets the underlying data object and refreshes all bound values.
        /// This function does not check for a valid transaction as the 
        /// public Data-property does.
        /// </summary>
        /// <param name="data">The bound data object (usually an Entity)</param>
        protected virtual void InternalSetData(IBindable data) {
            this._data = data;

            if (Template.Bound == Bound.Yes) {
                ((Obj)this.Parent).SetBound(Template, data);
            }

            RefreshAllBoundValues();
            OnData();
        }


        /// <summary>
        /// Refreshes all bound values of this Obj. Retrieves data from the Data
        /// property.
        /// </summary>
        private void RefreshAllBoundValues() {
            /*            TValue child;
                        for (Int32 i = 0; i < this.Template.Properties.Count; i++) {
                            child = Template.Properties[i] as TValue;
                            if (child != null && child.Bound) {
                                Refresh(child);
                            }
                        }
             */
        }

        /// <summary>
        /// Called after the Data property is set.
        /// </summary>
        protected virtual void OnData() {
        }


    }
}