// ***********************************************************************
// <copyright file="Obj.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Advanced;
using Starcounter.Templates;
using System;
using Starcounter.Internal;
using Starcounter.Templates.Interfaces;


namespace Starcounter {
    /// <summary>
    /// Base class for simple data objects that are mapped to schemas (called Templates). These
    /// objects can contain named properties with simple datatypes found in common programming languages,
    /// including string, integer, boolean, decimal, floating point, null and array. The objects mimics
    /// the kind of objects inducable from Json trees, albeit with a richer set of numeric representations.
    /// 
    /// An Obj object is a basic data object inspired by Json.  
    /// Obj objects can form trees using arrays and basic
    /// value types as well as nested objects.
    /// 
    /// While Json is a text based notation format, Obj is a materialized
    /// tree of objects than can be serialized and deserialized from Json.
    /// 
    /// The difference from the Json induced object tree in Javascript is
    /// foremost that Obj supports multiple numeric types, time and higher precision numerics.
    ///
    /// Obj is the base class for Starcounter Puppets and Starcounter Messages.
    /// 
    /// Each object points to a Template that describes its schema (properties). 
    /// 
    /// The datatypes are a merge of what is available in most common high abstraction application languages such as Javascript,
    /// C#, Ruby and Java. This means that it is in part a superset and in part a subset.
    /// 
    ///
    /// The types supported are:
    ///
    /// Object			    (can contain properties of any supported type)
    /// List			    (typed array/list/vector of any supported type),
    /// null            
    /// Time 			    (datetime)
    /// Boolean
    /// String 			    (variable length Unicode string),
    /// Integer 		    (variable length up to 64 bit, signed)
    /// Unsigned Integer	(variable length up to 64 bit, unsigned)
    /// Decimal			    (base-10 floating point up to 64 bit),
    /// Float			    (base-2 floating point up to 64 bit)
    /// 
    /// 
    /// The object trees are designed to be serializable and deserializable to and from JSON and XML although there
    /// is presently no XML implementation.
    /// 
    /// When you write applications in Starcounter, you normally do not use Obj objects directly. Instead you would
    /// use the specialisations Puppet for session-bound object trees or Message for REST style data transfer objects
    /// that are sent as requests or responses to and from a Starcounter REST endpoint (handler).
    /// </summary>
    /// <remarks>
    /// Obj is the base class for popular Starcounter concepts such as Puppets (live mirrored view models) and
    /// Messages (json data objects used in REST style code).
    ///
    /// The current implementation has a few shortcommings. Currently Obj only supports arrays of objects.
    /// Also, all objects in the array must use the same template. Support for arrays of value types (primitives) will
    /// be supported in the future. Mixed type arrays are currently not planned.
    /// 
    /// In the release version of Starcounter, Obj objects trees will be optimized for storage in "blobs" rather than on
    /// the garbage collected heap. This is such that stateful sessions can employ them without causing unnecessary system
    /// stress.
    /// </remarks>
    public abstract partial class Obj : Container, IHypermedia {

        /// <summary>
        /// Static constructor to automatically initialize XSON.
        /// </summary>
        static Obj() {
            HelperFunctions.LoadNonGACDependencies();
            XSON.CodeGeneration.Initializer.InitializeXSON();
        }

        /// <summary>
        /// An Obj can be bound to a data object. This makes the Obj reflect the data in the
        /// underlying bound object. This is common in database applications where Json messages
        /// or view models (Puppets) are often associated with database objects. I.e. a person form might
        /// reflect a person database object (Entity).
        /// </summary>
        private IBindable _data;

        /// <summary>
        /// Transaction applied to this node.
        /// </summary>
        private ITransaction _transaction;

        /// <summary>
        /// Cache element index if the parent of this Obj is an array (Arr).
        /// </summary>
        internal int _cacheIndexInArr;

        /// <summary>
        /// Injection point for generating typed json from different kinds of input.
        /// </summary>
        internal static ITypedJsonFactory Factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="Obj" /> class.
        /// </summary>
        public Obj()
            : base() {
            _cacheIndexInArr = -1;
            _transaction = null;
			LogChanges = false;
        }

        /// <summary>
        /// Returns True if current Obj is within the given tree.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public Boolean HasThisRoot(Obj treeRoot) {
            Container r = this;
            while (r.Parent != null)
                r = r.Parent;
            Obj root = (Obj)r;

            if (treeRoot == root)
                return true;

            return false;
        }

        /// <summary>
        /// In order to support Json pointers (TODO REF), this method is called
        /// recursively to fill in a list of relative pointers from the root to
        /// a given node in the Json like tree (the Obj/Arr tree).
        /// </summary>
        /// <param name="path">The patharray to fill</param>
        /// <param name="pos">The position to fill</param>
        internal override void FillIndexPath(int[] path, int pos) {
            if (Parent != null) {
                if (Parent is Arr) {
                    if (_cacheIndexInArr == -1) {
                        _cacheIndexInArr = ((Arr)Parent).IndexOf(this);
                    }
                    path[pos] = _cacheIndexInArr;
                }
                else {
					// We use the cacheIndexInArr to keep track of obj that is set
					// in the parent as an untyped object since the template here is not
					// the template in the parent (which we want).
					if (_cacheIndexInArr != -1)
						path[pos] = _cacheIndexInArr;
					else 
						path[pos] = Template.TemplateIndex;
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
                return (IBindable)_data;
            }
            set {
                if (Template == null) throw ErrorCode.ToException(Error.SCERRTEMPLATENOTSPECIFIED);
                InternalSetData(value);
            }
        }

        /// <summary>
        /// Start usage of given session.
        /// </summary>
        /// <param name="jsonNode"></param>
        internal void ResumeTransaction()
        {
            // Starting using current transaction if any.
            if (Transaction != null)
                StarcounterBase._DB.SetCurrentTransaction(_transaction);
        }

        /// <summary>
        /// Sets the underlying data object and refreshes all bound values.
        /// This function does not check for a valid transaction as the 
        /// public Data-property does.
        /// </summary>
        /// <param name="data">The bound data object (usually an Entity)</param>
        protected virtual void InternalSetData(IBindable data) {
            if (Transaction == null) {
                Transaction = StarcounterBase._DB.GetCurrentTransaction();
            }

            this._data = data;
            if (Template.Bound) {
                ((Obj)this.Parent).SetBound(Template, data);
            }

            RefreshAllBoundValues();
            OnData();
        }

        /// <summary>
        /// Gets nearest transaction.
        /// </summary>
        public ITransaction Transaction {
            get {

                // Returning first available transaction climbing up the tree starting from this node.

                if (_transaction != null)
                    return _transaction;

                Obj parentWithTrans = GetNearestObjParentWithTransaction();
                if (parentWithTrans != null)
                    return parentWithTrans.Transaction;

                return null;
            }
            set {
                if (_transaction != null) {
                    throw new Exception("An transaction is already set for this object. Changing transaction_ is not allowed.");
                }
                _transaction = value;
            }
        }

        /// <summary>
        /// Returns the transaction that is set on this app. Does NOT
        /// look in parents.
        /// </summary>
        internal ITransaction TransactionOnThisNode {
            get { return _transaction; }
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

        /// <summary>
        /// Returns the nearest parent that is not an Arr (list).
        /// </summary>
        /// <returns>An Obj or null if this is the root Obj.</returns>
        Obj GetNearestObjParent() {
            Container parent = Parent;
            while ((parent != null) && (!(parent is Obj))) {
                parent = parent.Parent;
            }
            return (Obj)parent;
        }

        /// <summary>
        /// Returns the nearest parent that has a transaction.
        /// </summary>
        /// <returns>An Obj or null if this is the root Obj.</returns>
        Obj GetNearestObjParentWithTransaction()
        {
            Container parent = Parent;
            while (parent != null)
            {
                Obj objParent = parent as Obj;

                if ((null != objParent) && (null != objParent.Transaction))
                    return objParent;

                parent = parent.Parent;
            }

            return (Obj)parent;
        }

        /// <summary>
        /// Refreshes the specified property of this Obj.
        /// </summary>
        /// <param name="property">The property</param>
        public void Refresh(Template property) {
            if (property is TObjArr) {
                TObjArr apa = (TObjArr)property;
                this.Set(apa, this.GetBound(apa));
            }
            else if (property is TObj) {
                var at = (TObj)property;
                IBindable v = this.GetBound(at);
                this.Set(at, v);
            }
            else {
                TValue p = property as TValue;
                if (p != null) {
                    HasChanged(p);
                }
            }
        }

        /// <summary>
        /// Is overridden by Puppet to log changes.
        /// </summary>
        /// <remarks>
        /// The puppet needs to log all changes as they will need to be sent to the client (the client keeps a mirrored view model).
        /// See MVC/MVVM (TODO! REF!). See Puppets (TODO REF)
        /// </remarks>
        /// <param name="property">The property that has changed in this Obj</param>
        protected virtual void HasChanged(TValue property) {
            ChangeLog.UpdateValue(this, property);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="property"></param>
        /// <param name="elementIndex"></param>
        public override void HasAddedElement(TObjArr property, int elementIndex) {
            ChangeLog.AddItemInList(this, property, elementIndex);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="property"></param>
        /// <param name="elementIndex"></param>
        public override void HasRemovedElement(TObjArr property, int elementIndex) {
            ChangeLog.RemoveItemInList(this, property, elementIndex);
        }

        /// <summary>
        /// The template defining the schema (properties) of this Obj.
        /// </summary>
        /// <value>The template</value>
        public new TObj Template {
            get { return (TObj)base.Template; }
            set { base.Template = value; }
        }

        /// <summary>
        /// Implementation field used to cache the Metadata property.
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

		/// <summary>
		/// If set true and a ChangeLog is set on the current thread, all 
		/// changes done to this Obj will be logged.
		/// </summary>
		public bool LogChanges { get; set; }

        public abstract void ProcessInput<V>(TValue<V> template, V value);
    }
}
