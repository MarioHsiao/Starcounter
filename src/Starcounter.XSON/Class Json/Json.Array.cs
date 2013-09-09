// ***********************************************************************
// <copyright file="AppList.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using Starcounter;

using Starcounter.Templates;
using Starcounter.Advanced;
using System.Collections;
using Starcounter.Internal.XSON;
using System.Text;

namespace Starcounter {


    /// <summary>
    /// 
    /// </summary>
    public partial class Arr : Container, IList<Json>
#if IAPP
//, IAppArray
#endif
 {

        // private TObjArr _property;

        /// <summary>
        /// 
        /// </summary>
        public Json Current {
            get {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="templ"></param>
        public Arr(Json parent, TObjArr templ) {
            this.Template = templ;
            Parent = parent;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public int IndexOf(Json item) {
#if QUICKTUPLE
            return _Values.IndexOf(item);
#else
         throw new NotImplementedException();
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        public void Insert(int index, Json item) {
            Json otherItem;
//            TObjArr template;

#if QUICKTUPLE
            _Values.Insert(index, item);
#else
         throw new NotImplementedException();
#endif
//            template = (TObjArr)this.Template;
//            ChangeLog.AddItemInList((Puppet)this.Parent, template, index);
            _CallHasAddedElement(index,item);

            for (Int32 i = index + 1; i < _Values.Count; i++) {
                otherItem = (Json)_Values[i];
                otherItem._cacheIndexInArr = i;
            }

        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        private void _CallHasAddedElement(int index, Json item) {
            var tarr = (TObjArr)this.Template;
            if (Session != null) {
                if (ArrayAddsAndDeletes == null) {
                    ArrayAddsAndDeletes = new List<Change>();
                }
                ArrayAddsAndDeletes.Add(Change.Update((Json)this.Parent, tarr, index));
                //Dirtyfy();
            }
            Parent.HasAddedElement(tarr, index);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        private void _CallHasRemovedElement(int index) {
            var tarr = (TObjArr)this.Template;
            if (Session != null) {
                if (ArrayAddsAndDeletes == null) {
                    ArrayAddsAndDeletes = new List<Change>();
                }
                ArrayAddsAndDeletes.Remove(Change.Add((Json)this.Parent, tarr, index));
                Dirtyfy();
            }
            Parent.HasRemovedElement(tarr, index);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="property"></param>
        internal void _CallHasChanged(TObjArr property, int index) {
            if (Session != null) {
                if (!_Values._BrandNew) {
                    //                    (_Values[index] as Json)._Dirty = true;
                    this.Dirtyfy();
                }
            }
            this.Parent.HasReplacedElement(property, index);
        }





        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        public void RemoveAt(int index) {
            Json otherItem;

#if QUICKTUPLE
            _Values.RemoveAt(index);
            var tarr = (TObjArr)this.Template;
            _CallHasRemovedElement(index);
            for (Int32 i = index; i < _Values.Count; i++) {
                otherItem = (Json)_Values[i];
                otherItem._cacheIndexInArr = i;
            }
#else
         throw new NotImplementedException();
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Remove(Json item) {
            Boolean b;
            Int32 index;

#if QUICKTUPLE
            index = _Values.IndexOf(item);
            b = (index != -1);
            if (b)
                RemoveAt(index);
            return b;
#else
         throw new NotImplementedException();
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Json this[int index] {
            get {
#if QUICKTUPLE
                return (Json)_Values[index];
#else
            throw new NotImplementedException();
#endif
            }
            set {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Json Add() {
#if QUICKTUPLE
            var elementType = ((TObjArr)this.Template).ElementType;
            Json x;
            if (elementType == null) {
                x = new Json();
            }
            else {
                x = (Json)elementType.CreateInstance(this);
            }

            //            var x = new App() { Template = ((TArr)this.Template).App };
            Add(x);
            return x;
#else
         throw new NotImplementedException();
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        public virtual void Add(Json item) {
            Int32 index;
#if QUICKTUPLE
            index = _Values.Count;
            _Values.Add(item);
            item._cacheIndexInArr = index;
            item.Parent = this;
#else
         throw new NotImplementedException();
#endif
            _CallHasAddedElement(_Values.Count - 1,item);
//            Parent.HasAddedElement((TObjArr)this.Template, QuickAndDirtyArray.Count - 1);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Clear() {

#if QUICKTUPLE

            this.InternalClear();
            Json parent = (Json)this.Parent;
            parent._CallHasChanged((TContainer)this.Template);
#else
         throw new NotImplementedException();
#endif
        }

        internal void InternalClear() {
            int indexesToRemove;
            var app = this.Parent;
            TObjArr property = (TObjArr)this.Template;
            indexesToRemove = _Values.Count;
            for (int i = (indexesToRemove - 1); i >= 0; i--) {
                app.HasRemovedElement(property, i);
            }
            _Values.Clear();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(Json item) {
#if QUICKTUPLE
            return _Values.Contains(item);
#else
         throw new NotImplementedException();
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(Json[] array, int arrayIndex) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        public int Count {
            get {
#if QUICKTUPLE
                return _Values.Count;
#else
            throw new NotImplementedException();
#endif
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsReadOnly {
            get {
#if QUICKTUPLE
                return false;
#else
            throw new NotImplementedException();
#endif
            }
        }

        IEnumerator<Json> IEnumerable<Json>.GetEnumerator() {
#if QUICKTUPLE
            var x = _Values.GetJsonArray().GetEnumerator();
            return (IEnumerator<Json>)x;
#endif
            throw new NotImplementedException();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
#if QUICKTUPLE
            return _Values.GetEnumerator();
#endif
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToJson() {
			byte[] buf;
			int size = ToJsonUtf8(out buf);
			return Encoding.UTF8.GetString(buf, 0, size);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public override int ToJsonUtf8(out byte[] buffer) {
			bool expandBuffer = false;
			byte[] itemJson = null;
			int size = 512;
			byte[] buf = new byte[size];
			int itemSize = 0;
			int offset = 0;
			int lastArrayPos = 0;

			buf[offset++] = (byte)'[';
			
		restart:
			if (expandBuffer) {
				while (size < (offset + itemSize))
					size *= 2;
				byte[] buf2 = new byte[size];
				Buffer.BlockCopy(buf, 0, buf2, 0, offset);
				buf = buf2;
			}
			expandBuffer = true;

			for (int i = lastArrayPos; i < Count; i++) {
				if (itemJson == null) {
					itemSize = this[i].ToJsonUtf8(out itemJson);
					if ((buf.Length - offset - 1) < itemSize)
						goto restart;
				}
				Buffer.BlockCopy(itemJson, 0, buf, offset, itemSize);
				itemJson = null;
				offset += itemSize;
				lastArrayPos++;
				if ((i + 1) < Count)
					buf[offset++] = (byte)',';
			}
			buf[offset++] = (byte)']';

			buffer = buf;
			return offset;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override byte[] ToJsonUtf8() {
			byte[] buf;
			int size = ToJsonUtf8(out buf);

			byte[] ret = new byte[size];
			Buffer.BlockCopy(buf, 0, ret, 0, size);
			return ret;
        }

    }
}

