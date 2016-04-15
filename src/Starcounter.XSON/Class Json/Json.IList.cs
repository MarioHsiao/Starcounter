// ***********************************************************************
// <copyright file="PropertyList.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using Starcounter;
using System.Collections;
using Starcounter.Templates;

namespace Starcounter {
    partial class Json : IList {
        private void VerifyIsArray() {
            if (!IsArray)
                throw new InvalidOperationException("Can only be called on arrays.");
        }

        bool IList.IsFixedSize {
            get {
                return false;
            }
        }

        object ICollection.SyncRoot {
            get {
                return null;
            }
        }

        bool ICollection.IsSynchronized {
            get {
                return false;
            }
        }

        internal object _GetAt(int index) {
            return list[index];
        }

        internal void _SetAt(int index, object value) {
            list[index] = value;
        }

        /// <summary>
        /// If true, this object has been flushed from the change log (usually an
        /// indication that the object has been sent to its client.
        /// </summary>
        internal bool HasBeenSent {
            get {
                if (!_trackChanges)
                    return false;

                if (_stepSiblings != null) {
                    return _stepSiblings.HasBeenSent(_stepSiblings.IndexOf(this));
                }

                if (Parent != null) {
                    return ((IndexInParent != -1) && (!Parent.WasReplacedAt(IndexInParent)));
                } else {
                    var log = ChangeLog;
                    if (log == null) {
                        return false;
                    }
                    return !log.BrandNew;
                }
            }
        }

        /// <summary>
        /// Use this property to access the values internally
        /// </summary>
        protected IList list {
            get {
                if (_list == null) {
                    return null;
                }
                if (IsArray) {
                    return _list;
                } else {
                    int childIndex;
                    if (!Template.IsPrimitive) {
                        var template = (TObject)Template;
                        while (_list.Count < template.Properties.Count) {
                            // We allow adding new properties to dynamic templates
                            // even after instances have been created.
                            // For this reason, we need to allow the expansion of the 
                            // values.
                            if (_trackChanges)
                                stateFlags.Add(PropertyState.Default);
                            childIndex = _list.Count;
                            _list.Add(null);
                            ((TValue)template.Properties[childIndex]).SetDefaultValue(this);
                        }
                    }
                    return _list;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vc"></param>
        internal void InitializeCache() {
            if (IsArray) {
                _list = new List<Json>();
                if (_trackChanges)
                    stateFlags = new List<PropertyState>();
            } else {
                SetDefaultValues();
            }
        }

        private void SetDefaultValues() {
            TObject tobj;

            if (!IsCodegenerated)
                _list = new List<object>();

            dirty = false;
            if (_trackChanges)
                stateFlags = new List<PropertyState>();

            if (_Template.IsPrimitive) {
                SetDefaultValue((TValue)_Template);
            } else {
                tobj = (TObject)_Template;
                dirty = false;
                for (int t = 0; t < tobj.Properties.Count; t++) {
                    SetDefaultValue((TValue)tobj.Properties[t]);
                }
            }
        }

        private void SetDefaultValue(TValue value) {
            if (_list != null)
                _list.Add(null);

            if (_trackChanges)
                stateFlags.Add(PropertyState.Default);

            value.SetDefaultValue(this);
        }

        internal bool WasReplacedAt(int index) {
            return ((stateFlags[index] & PropertyState.Dirty) == PropertyState.Dirty);
        }

        internal void CheckpointAt(int index) {
            stateFlags[index] = PropertyState.Default;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="template"></param>
        internal void MarkAsReplaced(Templates.Template template) {
            this.MarkAsReplaced(template.TemplateIndex);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        internal void MarkAsReplaced(int index) {
            stateFlags[index] |= PropertyState.Dirty;
            this.Dirtyfy();
        }

        internal Json NewItem() {
            var template = ((TObjArr)this.Template).ElementType;
            var item = (template != null) ? (Json)template.CreateInstance() : new Json();
            _Add(item);
            return item;
        }

        /// <summary>
        /// Copies all items to the specified array.
        /// </summary>
        /// <param name="array">The destination array.</param>
        /// <param name="arrayIndex">The start index in the source.</param>
        void ICollection.CopyTo(Array array, int arrayIndex) {
            list.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        /// <value>The count.</value>
        /// <returns>The number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1" />.</returns>
        int ICollection.Count {
            get {
                VerifyIsArray();
                return list.Count;
            }
        }

        /// <summary>
        /// Gets the index in the list of the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.Int32.</returns>
        int IList.IndexOf(object item) {
            VerifyIsArray();
            return list.IndexOf((object)item);
        }

        /// <summary>
        /// Inserts the item at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="item">The item.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        void IList.Insert(int index, object item) {
            Json j = VerifyJsonForInserting(item);

            list.Insert(index, j);
            j._cacheIndexInArr = index;
            j.Parent = this;

            if (_trackChanges) {
                stateFlags.Insert(index, PropertyState.Default);
                MarkAsReplaced(index);
            }
            
            Json otherItem;
            for (Int32 i = index + 1; i < list.Count; i++) {
                otherItem = (Json)list[i];
                otherItem._cacheIndexInArr = i;
            }
            CallHasAddedElement(index, j);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private Json VerifyJsonForInserting(object item) {
            VerifyIsArray();
            if (item == null) {
                throw new Exception("Typed object arrays cannot contain null elements");
            }
            if (!(item is Json)) {
                throw new Exception("You are only allowed to insert/add elements of type Json to a type Json array");
            }
            return (Json)item;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        private Json VerifyJsonForRemoving(object item) {
            Json ret;

            VerifyIsArray();
            if (item == null) {
                throw new Exception("Type object arrays cannot contain null elements");
            }
            ret = item as Json;
            if (ret == null) {
                throw new Exception("You are only allowed to remove elements of type Json from a type Json array");
            }
            return ret;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        int IList.Add(object item) {
            return _Add(item);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        internal void Replace(object item, int index) {
            var j = VerifyJsonForInserting(item);

            var typedListTemplate = ((TObjArr)Template).ElementType;
            if (typedListTemplate != null) {
                if (j.Template != typedListTemplate) {
                    throw new Exception(
                        String.Format("Cannot add item with template {0} as the array is expecting another template of type {1}",
                                j.Template.DebugString,
                                typedListTemplate.DebugString));
                }
            }

            var oldJson = (Json)list[index];
            if (oldJson != null) {
                oldJson._cacheIndexInArr = -1;
                oldJson.SetParent(null);
            }

            j._cacheIndexInArr = index;
            j.Parent = this;
            list[index] = j;

            if (_trackChanges) {
                MarkAsReplaced(index);
                Dirtyfy();
            }
            CallHasReplacedElement(index, j);
        }

        /// <summary>
        /// Adds the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <exception cref="System.Exception">This template is already used by an App. Cannot add more properties.</exception>
        internal int _Add(object item) {
            var j = VerifyJsonForInserting(item);

            var typedListTemplate = ((TObjArr)Template).ElementType;
            if (typedListTemplate != null) {
                if (j.Template != typedListTemplate) {
                    throw new Exception(
                        String.Format("Cannot add item with template {0} as the array is expecting another template of type {1}",
                                j.Template.DebugString,
                                typedListTemplate.DebugString));
                }
            }

            var index = list.Add(j);
            j._cacheIndexInArr = index;
            j.Parent = this;

            if (_trackChanges) {
                stateFlags.Add(PropertyState.Dirty);
                Dirtyfy();    
            }
            CallHasAddedElement(list.Count - 1, j);
            return index;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        void IList.Remove(object item) {
            Remove(item as Json);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        protected bool Remove(Json item) {
            bool b;
            int index;

            item = VerifyJsonForRemoving(item);
            index = list.IndexOf(item);
            b = (index != -1);
            if (b) InternalRemove(item, index);
            return b;
        }

        private void Move(int fromIndex, int toIndex) {
            Json item = (Json)list[fromIndex];
            list.RemoveAt(fromIndex);
            list.Insert(toIndex, item);
            
            int start;
            int stop;

            if (fromIndex < toIndex) {
                start = fromIndex;
                stop = toIndex;
            } else {
                start = toIndex;
                stop = fromIndex;
            }

            for (Int32 i = start; i <= stop; i++) {
                ((Json)list[i])._cacheIndexInArr = i;
            }
            CallHasMovedElement(fromIndex, toIndex, item);
        }

        /// <summary>
        /// Removed the item at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        void IList.RemoveAt(int index) {
            Json item = VerifyJsonForRemoving(list[index]);
            InternalRemove(item, index);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="index"></param>
        private void InternalRemove(Json item, int index) {
            list.RemoveAt(index);
            item.SetParent(null);
            item._cacheIndexInArr = -1;

            if (_trackChanges)
                stateFlags.RemoveAt(index);
            
            if (IsArray) {
                Json otherItem;
                var tarr = (TObjArr)this.Template;
                CallHasRemovedElement(index, item);
                for (Int32 i = index; i < list.Count; i++) {
                    otherItem = (Json)_list[i];
                    otherItem._cacheIndexInArr = i;
                }
            }
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        void IList.Clear() {
            VerifyIsArray();

            if (_trackChanges) {
                Parent.MarkAsReplaced(Template);
                stateFlags.Clear();
            }

            InternalClear();
            Parent.CallHasChanged((TContainer)this.Template);
        }

        /// <summary>
        /// 
        /// </summary>
        internal void InternalClear() {
            int indexesToRemove;
            var app = this.Parent;
            TObjArr property = (TObjArr)this.Template;
            indexesToRemove = list.Count;
            for (int i = (indexesToRemove - 1); i >= 0; i--) {
                ((Json)list[i]).SetParent(null);
                app.ChildArrayHasRemovedAnElement(property, i);
            }
            list.Clear();
        }

        /// <summary>
        /// Checks if the specified item exists in the list.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if [contains] [the specified item]; otherwise, <c>false</c>.</returns>
        bool IList.Contains(object item) {
            VerifyIsArray();
            return list.Contains(item);
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only.
        /// </summary>
        /// <value><c>true</c> if this instance is read only; otherwise, <c>false</c>.</value>
        /// <returns>true if the <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only; otherwise, false.</returns>
        bool IList.IsReadOnly {
            get { return false; }
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator() {
            VerifyIsArray();
            if (list != null)
                return list.GetEnumerator();
            return System.Linq.Enumerable.Empty<Json>().GetEnumerator();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        internal List<Json> GetJsonArray() {
            return (List<Json>)list;
        }

        /// <summary>
        /// Return the position of this Json object or array within its parent
        /// object or array. For arrays, this means the index of the element and
        /// for objects it means the index of the property.
        /// </summary>
        public int IndexInParent {
            get {
                if (_cacheIndexInArr != -1) {
                    return _cacheIndexInArr;
                } else {
                    return Template.TemplateIndex;
                }
            }
        }
    }
}
