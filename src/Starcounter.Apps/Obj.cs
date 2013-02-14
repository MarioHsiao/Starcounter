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
using Starcounter.Internal.REST;
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
            _cacheIndexInList = -1;
        }



        /// <summary>
        /// Cache field of index if the parent of this Obj is a list.
        /// </summary>
        internal Int32 _cacheIndexInList;

        /// <summary>
        /// Fills the index path.
        /// </summary>
        /// <param name="path">The patharray to fill</param>
        /// <param name="pos">The position to fill</param>
        internal override void FillIndexPath(int[] path, int pos) {
            if (Parent != null) {
                if (Parent is Listing) {
                    if (_cacheIndexInList == -1) {
                        _cacheIndexInList = ((Listing)Parent).IndexOf(this);
                    }
                    path[pos] = _cacheIndexInList;
                } else {
                    path[pos] = Template.Index;
                }
                Parent.FillIndexPath(path, pos - 1);
            }
        }

        /// <summary>
        /// Gets or sets the underlying entity object.
        /// </summary>
        /// <value>The data.</value>
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
        /// <param name="data"></param>
        internal virtual void InternalSetData(IBindable data) {

            _Data = data;

            if (Template.Bound) {
                Template.SetBoundValue((Obj)this.Parent, data);
            }

            RefreshAllBoundValues();
            OnData();
        }

        /// <summary>
        /// Refreshes all databound values for this Obj.
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
        /// Called after the data object is set.
        /// </summary>
        protected virtual void OnData() {
        }

//        /// <summary>
//        /// Calls the init.
//        /// </summary>
//        internal void CallInit() {
//            Init();
//        }

//        public void Input( Input input ) {
//        }

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
            if (model is ObjArrProperty) {
                ObjArrProperty apa = (ObjArrProperty)model;
                this.SetValue(apa, apa.GetBoundValue(this));
            } else if (model is ObjTemplate) {
                var at = (ObjTemplate)model;

                // TODO:
                IBindable v = at.GetBoundValue(this);
                if (v != null)
                    this.SetValue(at, v);
            } else {
                Property p = model as Property;
                if (p != null) {
                    HasChanged(p);
                }
            }
        }

        /// <summary>
        /// Is overridden by Puppet to log changes.
        /// </summary>
        /// <param name="property">The property that has changed in this Obj</param>
        protected virtual void HasChanged( Property property ) {
        }

        /// <summary>
        /// The template defining the properties of this Obj.
        /// </summary>
        /// <value>The template.</value>
        public new ObjTemplate Template {
            get { return (ObjTemplate)base.Template; }
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

//        /// <summary>
//        /// Use this method to override the default communication from the client.
//        /// </summary>
//        /// <remarks>
//        /// Requests can use the WebSockets or HTTP protocol
//        /// </remarks>
//        /// <param name="request">Can be used to retrieve the data of the request</param>
//        /// <returns>The raw response</returns>
//        public virtual byte[] HandleRawRequest(HttpRequest request) {
//            return null;
//        }

        /// <summary>
        /// If the view lives in this .NET application domain, this property can be used to reference it.
        /// For Starcounter serverside App objects, this property is often a string that is used to identifify
        /// a specific view. For web applications, the string is often a reference to the .html file.
        /// </summary>
        /// <value>The media.</value>
        public Media Media { get; set; }

        /// <summary>
        /// Gets or sets the view.
        /// </summary>
        /// <value>The view.</value>
        public string View { get; set; }


        /// <summary>
        /// Deletes this instance.
        /// </summary>
        public void Delete() { }

        /// <summary>
        /// Removes this Obj from its parent.
        /// </summary>
        public void Close() { }

        /// <summary>
        /// Shows this instance.
        /// </summary>
        public void Show() {
        }
    }
}
