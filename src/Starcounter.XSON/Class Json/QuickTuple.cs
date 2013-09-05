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

namespace Starcounter.Internal {

    /// <summary>
    /// The collection of values in a JSON object or array.
    /// Will be replaced by the session blog storage system.
    /// </summary>
    public class QuickTuple : IList {

        public bool IsFixedSize {
            get {
                return false;
            }
        }

        public object SyncRoot {
            get {
                return null;
            }
        }

        public bool IsSynchronized {
            get {
                return false;
            }
        }

        /// <summary>
        /// Used by change log
        /// </summary>
        public bool _BrandNew {
            get {
                return __BrandNew_;
            }
            set {
                __BrandNew_ = value;
            }
        }

        private bool __BrandNew_ = true;

        /// <summary>
        /// The naive implementation keeps track of the changed values
        /// generate the JSON-Patch document
        /// </summary>
        internal List<bool> _ReplacedFlag;

        // /// <summary>
        // /// The naive implementation keeps track of the changed database objects to
        // /// generate the JSON-Patch document
        // /// </summary>
        // internal List<object> _BoundDirtyCheck;

        /// <summary>
        /// The owner of this list.
        /// </summary>
        private Container parent;

        /// <summary>
        /// </summary>
        private IList list;

        /// <summary>
        /// </summary>
        /// <param name="parent"></param>
        internal QuickTuple(Container parent,int vc) {
            this.parent = parent;
            
            if (parent.IsArray)
                list = new List<Json>();
            else
                list = new List<object>();

            _ReplacedFlag = new List<bool>(vc);
            for (int i = 0; i < vc; i++) {
                _ReplacedFlag.Add(false);
                list.Add(null);
            }
        }

        public bool WasReplacedAt(int index) {
            return _ReplacedFlag[index];
        }

        public void CheckpointAt(int index) {
            _ReplacedFlag[index] = false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public object this[int index] {
            get {
                return list[index];
            }
            set {
                if (!_BrandNew) {
                    _ReplacedFlag[index] = true;
                }

                if (parent.IsArray) {
                    (parent as Arr)._CallHasChanged(parent.Template as TObjArr, index);
                }
                else {
                    (parent as Json)._CallHasChanged(parent.Template as TValue);
                }

                list[index] = value;
            }
        }

        /// <summary>
        /// Copies all items to the specified array.
        /// </summary>
        /// <param name="array">The destination array.</param>
        /// <param name="arrayIndex">The start index in the source.</param>
        public void CopyTo(Array array, int arrayIndex) {
            list.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        /// <value>The count.</value>
        /// <returns>The number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1" />.</returns>
        public int Count {
            get { return list.Count; }
        }

        /// <summary>
        /// Gets the index in the list of the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.Int32.</returns>
        public int IndexOf(object item) {
            return list.IndexOf((object)item);
        }

        /// <summary>
        /// Inserts the item at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="item">The item.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void Insert(int index, object item) {
            list.Insert(index, item);
            _ReplacedFlag.Insert(index, false);
        }

        /// <summary>
        /// Removed the item at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void RemoveAt(int index) {
            list.RemoveAt(index);
            _ReplacedFlag.RemoveAt(index);
        }

        /// <summary>
        /// Adds the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <exception cref="System.Exception">This template is already used by an App. Cannot add more properties.</exception>
        public int Add(object item) {
            _ReplacedFlag.Add(false);
            return list.Add(item);
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        public void Clear() {
            list.Clear();
            _ReplacedFlag.Clear();
        }

        /// <summary>
        /// Checks if the specified item exists in the list.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if [contains] [the specified item]; otherwise, <c>false</c>.</returns>
        public bool Contains(object item) {
            return list.Contains((object)item);
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only.
        /// </summary>
        /// <value><c>true</c> if this instance is read only; otherwise, <c>false</c>.</value>
        /// <returns>true if the <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only; otherwise, false.</returns>
        public bool IsReadOnly {
            get { return false; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public void Remove(object item) {
            var i = IndexOf(item);
            if (i == -1)
                return;
            list.RemoveAt(i);
            _ReplacedFlag.RemoveAt(i);
            return;
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
        public IEnumerator GetEnumerator() {
            return list.GetEnumerator();
        }


        public List<Json> GetJsonArray() {
            return (List<Json>)list;
        }
    }

}
