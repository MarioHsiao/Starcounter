// ***********************************************************************
// <copyright file="Obj.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

// TODO!
// - Get rid of references to Transaction, SQL and Entity



using System;
using Starcounter.Templates.Interfaces;
using System.ComponentModel;
using Starcounter.Templates;
using Starcounter.Internal;
using Starcounter.Advanced;

namespace Starcounter {
    /// <summary>
    /// Base class for simple data objects that are mapped to schemas (called Templates). These
    /// objects can contain named properties with simple datatypes found in common programming languages,
    /// including string, integer, boolean, decimal, floating point, null and array. The objects mimics
    /// the kind of objects inducable from Json trees, albeit with a richer set of numeric representations.
    /// </summary>
    /// <remarks>
    /// Obj is the base class for popular Starcounter concepts such as Puppets and Messages.
    /// </remarks>
    public abstract partial class Obj : Container {
        /// <summary>
        /// An Obj can be bound to a data object. This makes the Obj reflect the data in the
        /// underlying bound object. This is common in database applications where Json messages
        /// or view models (Puppets) are often associated with database objects. I.e. a person form might
        /// reflect a person database object (Entity).
        /// </summary>
        private IBindable _Data;

        /// <summary>
        /// Initializes a new instance of the <see cref="Obj" /> class.
        /// </summary>
        public Obj() : base() {
            _cacheIndexInArr = -1;
        }

        /// <summary>
        /// Cache element index if the parent of this Obj is an array (Arr).
        /// </summary>
        internal Int32 _cacheIndexInArr;

        /// <summary>
        /// In order to support Json pointers (TODO REF), this method is called
        /// recursivly to fill in a list of relative pointers from the root to
        /// a given node in the Json like tree (the Obj/Arr tree).
        /// </summary>
        /// <param name="path">The patharray to fill</param>
        /// <param name="pos">The position to fill</param>
        internal override void FillIndexPath(int[] path, int pos) {
            if (Parent != null) {
                if (Parent is Listing) {
                    if (_cacheIndexInArr == -1) {
                        _cacheIndexInArr = ((Listing)Parent).IndexOf(this);
                    }
                    path[pos] = _cacheIndexInArr;
                } else {
                    path[pos] = Template.Index;
                }
                Parent.FillIndexPath(path, pos - 1);
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
                return (IBindable)_Data;
            }
            set {
                InternalSetData(value);
            }
        }

        /// <summary>
        /// Sets the underlying dataobject and refreshes all bound values.
        /// This function does not check for a valid transaction as the 
        /// public Data-property does.
        /// </summary>
        /// <param name="data">The bound data object (usually an Entity)</param>
        internal virtual void InternalSetData(IBindable data) {

            _Data = data;

            if (Template.Bound) {
                Template.SetBoundValue((Obj)this.Parent, data);
            }

            RefreshAllBoundValues();
            OnData();
        }

        /// <summary>
        /// Refreshes all bound values of this Obj. Retrieves data from the Data
        /// property.
        /// </summary>
        private void RefreshAllBoundValues() {
            Template child;
            for (Int32 i = 0; i < this.Template.Properties.Count; i++) {
                child = Template.Properties[i];
                if (child.Bound) {
                    Refresh(child);
                }
            }
        }

        /// <summary>
        /// Called after the Data property is set.
        /// </summary>
        protected virtual void OnData() {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        internal Obj GetNearestObjParent() {
            Container parent = Parent;
            while ((parent != null) && (!(parent is Obj))) {
                parent = parent.Parent;
            }
            return (Obj)parent;
        }

        /// <summary>
        /// Refreshes the specified template.
        /// </summary>
        /// <param name="model">The model.</param>
        public void Refresh(Template model) {
            if (model is TObjArr) {
                TObjArr apa = (TObjArr)model;
                this.SetValue(apa, apa.GetBoundValue(this));
            } else if (model is TObj) {
                var at = (TObj)model;

                // TODO:
                IBindable v = at.GetBoundValue(this);
                if (v != null)
                    this.SetValue(at, v);
            } else {
                TValue p = model as TValue;
                if (p != null) {
                    HasChanged(p);
                }
            }
        }

        /// <summary>
        /// Is overridden by Puppet to log changes.
        /// </summary>
        /// <param name="property">The property that has changed in this Obj</param>
        protected virtual void HasChanged( TValue property ) {
        }

        /// <summary>
        /// The template defining the properties of this Obj.
        /// </summary>
        /// <value>The template.</value>
        public new TObj Template {
            get { return (TObj)base.Template; }
            set { base.Template = value; }
        }

        /// <summary>
        /// Implementation field used to cache the Properties property.
        /// </summary>
        private ObjMetadata _Metadata = null;

        /// <summary>
        /// Here you can set properties for each property in this Obj (such as Editable, Visible and Enabled).
        /// The changes only affect this instance.
        /// If you which to change properties for the template, use the Template property instead.
        /// </summary>
        /// <value>The metadata.</value>
        /// <remarks>It is much less expensive to set this kind of metadata for the
        /// entire template (for example to mark a property for all Obj instances as Editable).</remarks>
        public ObjMetadata Metadata {
            get {
                return _Metadata;
            }
        }

    }
}
