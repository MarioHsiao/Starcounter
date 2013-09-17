// ***********************************************************************
// <copyright file="AppParent.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;

using Starcounter.Templates;
using Starcounter.Advanced;
using System.Text;
using System.Collections;
namespace Starcounter {

    /// <summary>
    /// Base class for App and AppList instances.
    /// </summary>
    public partial class Json : StarcounterBase
    {

        public object this[int index] {
            get {
                if (this.IsArray) {
                    return _GetAt(index);
                }
                else {
                    var json = this as Json;
                    var property = (TValue)((TObject)Template).Properties[index];
                    if (property.UseBinding(json.DataAsBindable)) {
                        object ret;
                        ret = json.GetBound(property);
                        if (property is TObject) {
                            var newJson = (Json)property.CreateInstance(this);
                            newJson.AttachData((IBindable)ret);
                            _SetAt(property.TemplateIndex, newJson);
                            return newJson;
                        }
                        else if (property is TObjArr) {
                            var newJson = (Json)property.CreateInstance(this);
                            newJson.Data = (IEnumerable)ret;
                            _SetAt(property.TemplateIndex, newJson);
                            return newJson;
                        }
                        return ret;
                    }
                    else {
                        return _GetAt(property.TemplateIndex);
                    }
                }
            }
            set {




                if (IsArray) {
                    // We need to update the cached index array
                    var thisj = this as Json;
                    var j = value as Json;
                    if (j != null) {
                        j.Parent = this;
                        j._cacheIndexInArr = index;
                    }
                    var oldValue = (Json)_list[index];
                    if (oldValue != null) {
                        oldValue.SetParent(null);
                        oldValue._cacheIndexInArr = -1;
                    }
                }
                else {
                    var property = (TValue)((TObject)Template).Properties[index];
                    this._OnSetProperty(property,value);
                }
                list[index] = value;

                if (!_BrandNew) {
                    MarkAsReplaced(index);
                }

                if (IsArray) {
                    (this as Json)._CallHasChanged(this.Template as TObjArr, index);
                }
                else {
                    (this as Json)._CallHasChanged(this.Template as TValue);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="property"></param>
        /// <param name="value"></param>
        private void _OnSetProperty(TValue property, object value) {
            var thisj = this as Json;


            if (property.UseBinding(thisj.DataAsBindable)) {
                if (property is TObject) {
                    thisj.SetBound(property, (value as Json).Data);
                }
                else {
                    thisj.SetBound(property, value);
                }
            }

            if (property is TObjArr) {
                var valuearr = (Json)value;
                var oldValue = (Json)_list[property.TemplateIndex];
                if (oldValue != null) {
                    oldValue.InternalClear();
                    //                oldValue.Clear();
                    oldValue.SetParent(null);
                }

                valuearr.Array_InitializeAfterImplicitConversion(thisj, (TObjArr)property);
            }

            if (property is TObject) {
                var j = (Json)value;
                // We need to update the cached index array
                if (j != null) {
                    j.Parent = this;


                    j._cacheIndexInArr = property.TemplateIndex;
                }
                var vals = list;
                var i = property.TemplateIndex;
                var oldValue = (Json)vals[i];
                if (oldValue != null) {
                    oldValue.SetParent(null);
                    oldValue._cacheIndexInArr = -1;
                }
            }
        }
         

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
                else if (_Template == null) {
                    return;
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
//              this.Init();
            }
            get {
                return _Template;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsArray {
            get {
                if (Template == null) {
                    return false;
                }
                return Template is TObjArr;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsPrimitive {
            get {
                if (Template == null) {
                    return false;
                }
                return Template.IsPrimitive;
            }
        }

        /// <summary>
        /// Inits this instance.
        /// </summary>
		//protected virtual void Init() {
		//}



        /// <summary>
        /// Used to generate change logs for all pending property changes in this object and
        /// and its children and grandchidren (recursivly) including changes to bound data
        /// objects.
        /// </summary>
        /// <param name="session">The session (for faster access)</param>


        ///// <summary>
        ///// Called when [set parent].
        ///// </summary>
        ///// <param name="child">The child.</param>
        //internal virtual void OnSetParent(Container child) {
        //    //child._parent = this;
        //}

        public virtual void ChildArrayHasAddedAnElement(TObjArr property, int elementIndex) {
        }

        public virtual void ChildArrayHasRemovedAnElement(TObjArr property, int elementIndex) {
        }

        public virtual void ChildArrayHasReplacedAnElement(TObjArr property, int elementIndex) {
        }


        /// <summary>
        /// Gets or sets the parent.
        /// </summary>
        /// <value>The parent.</value>
        /// <exception cref="System.Exception">Cannot change parent in Apps</exception>
        public Json Parent {
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
        internal void SetParent(Json value) {
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
        private void HasRemovedChild( Json child ) {
            // This Obj or Arr has been removed from its parent and should be deleted from the
            // URI cache.
            //
            // TheCache.RemoveEntry( child );
            //
        }


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
        /// In order to support Json pointers (TODO REF), this method is called
        /// recursively to fill in a list of relative pointers from the root to
        /// a given node in the Json like tree (the Obj/Arr tree).
        /// </summary>
        /// <param name="path">The patharray to fill</param>
        /// <param name="pos">The position to fill</param>
        internal void FillIndexPath(Int32[] path, Int32 pos) {
            if (IsArray) {
                path[pos] = Template.TemplateIndex;
                Parent.FillIndexPath(path, pos - 1);
            }
            else {
                if (Parent != null) {
                    if (Parent.IsArray) {
                        if (_cacheIndexInArr == -1) {
                            _cacheIndexInArr = Parent.IndexOf(this);
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
        }

    }
}

