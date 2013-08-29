// ***********************************************************************
// <copyright file="AppParent.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;

using Starcounter.Templates;
using Starcounter.Advanced;
using System.Text;
namespace Starcounter {

    /// <summary>
    /// Base class for App and AppList instances.
    /// </summary>
    public abstract class Container : StarcounterBase
    {
        /// <summary>
        /// Json objects can be stored on the server between requests as session data.
        /// </summary>
        internal Session _Session;
         
        /// <summary>
        /// Tells if any property value has changed on this container (if it is an object) or
        /// any of its children or grandchildren (recursivly). If this flag is true, there can be
        /// no changes to the JSON tree (but there can be changes to bound data objects).
        /// </summary>
        internal bool _Dirty = false;

        /// <summary>
        /// Used by change log
        /// </summary>
        internal bool _BrandNew = true;

        /// <summary>
        /// Json objects can be stored on the server between requests as session data.
        /// </summary>
        public Session Session {
            get {
                if (_Session == null && Parent != null ) {
                    return Parent.Session;
                }
                return _Session;
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        internal void Dirtyfy() {
            _Dirty = true;
            if (Parent != null)
                Parent.Dirtyfy();
        }



        /// <summary>
        /// The _ template
        /// </summary>
        internal Template _Template;

        /// <summary>
        /// The schema element of this app instance
        /// </summary>
        /// <value>The template.</value>
        /// <exception cref="System.Exception">Template is already set for App. Cannot change template once it is set</exception>
        public Template Template {
            set {
                //if (_Template != null) {
                //    throw new Exception("Template is already set for App. Cannot change template once it is set");
                //}
                _Template = (TContainer)value;

                if (_Template is TObject && ((TObject)_Template).IsDynamic) {
                    TObject t = (TObject)_Template;
                    if (t.SingleInstance != null && t.SingleInstance != this) {
                        throw new Exception(String.Format("You cannot assign a Template ({0}) for a dynamic Json object (i.e. an Expando like object) to a new Json object ({0})",value,this));
                    }
                    ((TObject)_Template).SingleInstance = (Json)this;
                }
                else {
                    _Template.Sealed = true;
                }
#if QUICKTUPLE
                _InitializeValues();
#endif
       //         if (this is App) {
       //             ((App)this).CallInit();
       //         }
                this.Init();
            }
            get {
                return _Template;
            }
        }

        /// <summary>
        /// Inits this instance.
        /// </summary>
        protected virtual void Init() {
        }

        /// <summary>
        /// Used to generate change logs for all pending property changes in this object and
        /// and its children and grandchidren (recursivly) excluding changes to bound data
        /// objects. This method is much faster than the corresponding method checking
        /// th database.
        /// </summary>
        /// <param name="session">The session (for faster access)</param>
        internal abstract void LogValueChangesWithoutDatabase(Starcounter.Session session);

        /// <summary>
        /// Used to generate change logs for all pending property changes in this object and
        /// and its children and grandchidren (recursivly) including changes to bound data
        /// objects.
        /// </summary>
        /// <param name="session">The session (for faster access)</param>
        internal abstract void LogValueChangesWithDatabase(Session session);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="indentation"></param>
        internal abstract void WriteToDebugString(StringBuilder sb, int indentation);

        /// <summary>
        /// Called by WriteDebugToString implementations
        /// </summary>
        /// <param name="sb">The string used to write text to</param>
        internal void _WriteDebugProperty(StringBuilder sb) {
            var t = this.Template;
            if (t != null) {
                var name = this.Template.PropertyName;
                if (name != null) {
                    sb.Append('"');
                    sb.Append(name);
                    sb.Append("\":");
                }
            }
            if (this is Json && ((Json)this).Data != null) {
                sb.Append("(db)");
            }
            if (_BrandNew) {
                sb.Append("(n)");
            }
            if (_Dirty) {
                sb.Append("(d)");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        internal abstract void CheckpointChangeLog();

#if QUICKTUPLE
        /// <summary>
        /// _s the initialize values.
        /// </summary>
        protected virtual void _InitializeValues() {
        }

#endif
        ///// <summary>
        ///// Called when [set parent].
        ///// </summary>
        ///// <param name="child">The child.</param>
        //internal virtual void OnSetParent(Container child) {
        //    //child._parent = this;
        //}

        public virtual void HasAddedElement(TObjArr property, int elementIndex) {
        }

        public virtual void HasRemovedElement(TObjArr property, int elementIndex) {
        }

        public virtual void HasReplacedElement(TObjArr property, int elementIndex) {
        }

        /// <summary>
        /// The _parent
        /// </summary>
        internal Container _parent;

        /// <summary>
        /// Gets or sets the parent.
        /// </summary>
        /// <value>The parent.</value>
        /// <exception cref="System.Exception">Cannot change parent in Apps</exception>
        public Container Parent {
            get {
                return _parent;
            }
            set {
                if (_parent != null && _parent != value) {
                    throw new Exception("Cannot change parent of objects in Typed JSON trees");
                }
                SetParent(value);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        internal void SetParent(Container value) {
            if (value == null) {
                if (_parent != null) {
                    _parent.HasRemovedChild(this);
                }
            }
            _parent = value;
        }


        /// <summary>
        /// Called when a Obj or Arr property value has been removed from its parent.
        /// </summary>
        /// <param name="property">The name of the property</param>
        /// <param name="child">The old value of the property</param>
        private void HasRemovedChild( Container child ) {
            // This Obj or Arr has been removed from its parent and should be deleted from the
            // URI cache.
            //
            // TheCache.RemoveEntry( child );
            //
        }

        /// <summary>
        /// Contains the depth of this Container. Used when creating the indexpath.
        /// </summary>
        private Int32 _cachePathDepth = -1;

        /// <summary>
        /// Returns the depth of this Container.
        /// </summary>
        /// <value>The index path depth.</value>
        internal int IndexPathDepth
        {
            get
            {
                if (_cachePathDepth == -1)
                {
                    _cachePathDepth = (Parent == null) ? 0 : Parent.IndexPathDepth + 1;
                }
                return _cachePathDepth;
            }
        }

        /// <summary>
        /// Returns the depth of any child for this Container. Since all children
        /// will have the same depth, a specific childinstance is not needed.
        /// </summary>
        /// <value>The child path depth.</value>
        internal int ChildPathDepth
        {
            get { return IndexPathDepth + 1; }
        }

        /// <summary>
        /// Returns an array of indexes starting from the rootapp on how to get
        /// to this specific instance.
        /// </summary>
        /// <value>The index path.</value>
        internal Int32[] IndexPath
        {
            get
            {
                Int32[] ret = new Int32[IndexPathDepth];
                //ret[ret.Length - 1] = 
                FillIndexPath(ret, ret.Length - 2);
                return ret;
            }
        }

        /// <summary>
        /// Returns an array of indexes starting from the rootapp on how to get
        /// the instance of the specified template.
        /// </summary>
        /// <param name="template">The template.</param>
        /// <returns>Int32[][].</returns>
        public Int32[] IndexPathFor(Template template)
        {
            Int32[] path = new Int32[ChildPathDepth];
            path[path.Length - 1] = template.TemplateIndex;
            FillIndexPath(path, path.Length - 2);
            return path;
        }

        /// <summary>
        /// Fills the index path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="pos">The pos.</param>
        internal virtual void FillIndexPath(Int32[] path, Int32 pos)
        {
            path[pos] = Template.TemplateIndex;
            Parent.FillIndexPath(path, pos - 1);
        }

        /// 
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public abstract byte[] ToJsonUtf8();

        /// <summary>
        /// Serializes this object and sets the out parameter to the buffer containing 
        /// the UTF8 encoded characters. Returns the size used in the buffer.
        /// </summary>
        /// <remarks>
        /// The actual returned buffer might be larger than the amount used.
        /// </remarks>
        /// <param name="buf"></param>
        /// <returns></returns>
        public abstract int ToJsonUtf8(out byte[] buffer);


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public abstract string ToJson();
    }
}
