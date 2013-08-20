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

namespace Starcounter {


    /// <summary>
    /// 
    /// </summary>
    public partial class Arr : Container, IList<Obj>
#if IAPP
//, IAppArray
#endif
 {

#if QUICKTUPLE
        /// <summary>
        /// Temporary. Should be replaced by TupleProxy functionality
        /// </summary>
        internal List<Obj> QuickAndDirtyArray = new List<Obj>();
        internal List<Change> Changes = null;
#endif
        // private TObjArr _property;

        /// <summary>
        /// 
        /// </summary>
        public Obj Current {
            get {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="templ"></param>
        public Arr(Obj parent, TObjArr templ) {
            this.Template = templ;
            Parent = parent;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public int IndexOf(Obj item) {
#if QUICKTUPLE
            return QuickAndDirtyArray.IndexOf(item);
#else
         throw new NotImplementedException();
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        public void Insert(int index, Obj item) {
            Obj otherItem;
//            TObjArr template;

#if QUICKTUPLE
            QuickAndDirtyArray.Insert(index, item);
#else
         throw new NotImplementedException();
#endif
//            template = (TObjArr)this.Template;
//            ChangeLog.AddItemInList((Puppet)this.Parent, template, index);
            _CallHasAddedElement(index,item);

            for (Int32 i = index + 1; i < QuickAndDirtyArray.Count; i++) {
                otherItem = QuickAndDirtyArray[i];
                otherItem._cacheIndexInArr = i;
            }

        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        private void _CallHasAddedElement(int index, Obj item) {
            var tarr = (TObjArr)this.Template;
            if (Session != null) {
                if (Changes == null) {
                    Changes = new List<Change>();
                }
                Changes.Add(Change.Update((Obj)this.Parent, tarr, index));
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
                if (Changes == null) {
                    Changes = new List<Change>();
                }
                Changes.Remove(Change.Add((Obj)this.Parent, tarr, index));
                Dirtyfy();
            }
            Parent.HasRemovedElement(tarr, index);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        public void RemoveAt(int index) {
            Obj otherItem;

#if QUICKTUPLE
            QuickAndDirtyArray.RemoveAt(index);
            var tarr = (TObjArr)this.Template;
            _CallHasRemovedElement(index);
            for (Int32 i = index; i < QuickAndDirtyArray.Count; i++) {
                otherItem = QuickAndDirtyArray[i];
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
        public bool Remove(Obj item) {
            Boolean b;
            Int32 index;

#if QUICKTUPLE
            index = QuickAndDirtyArray.IndexOf(item);
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
        public Obj this[int index] {
            get {
#if QUICKTUPLE
                return QuickAndDirtyArray[index];
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
        public Obj Add() {
#if QUICKTUPLE
            Obj x = (Obj)((TObjArr)this.Template).ElementType.CreateInstance(this);

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
        public virtual void Add(Obj item) {
            Int32 index;
#if QUICKTUPLE
            index = QuickAndDirtyArray.Count;
            QuickAndDirtyArray.Add(item);
            item._cacheIndexInArr = index;
            item.Parent = this;
#else
         throw new NotImplementedException();
#endif
            _CallHasAddedElement(QuickAndDirtyArray.Count - 1,item);
//            Parent.HasAddedElement((TObjArr)this.Template, QuickAndDirtyArray.Count - 1);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Clear() {

#if QUICKTUPLE

            this.InternalClear();
            Obj parent = (Obj)this.Parent;
            parent._CallHasChanged(this.Template);
#else
         throw new NotImplementedException();
#endif
        }

        internal void InternalClear() {
            int indexesToRemove;
            var app = this.Parent;
            TObjArr property = (TObjArr)this.Template;
            indexesToRemove = QuickAndDirtyArray.Count;
            for (int i = (indexesToRemove - 1); i >= 0; i--) {
                app.HasRemovedElement(property, i);
            }
            QuickAndDirtyArray.Clear();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(Obj item) {
#if QUICKTUPLE
            return QuickAndDirtyArray.Contains(item);
#else
         throw new NotImplementedException();
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(Obj[] array, int arrayIndex) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        public int Count {
            get {
#if QUICKTUPLE
                return QuickAndDirtyArray.Count;
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

        IEnumerator<Obj> IEnumerable<Obj>.GetEnumerator() {
#if QUICKTUPLE
            return QuickAndDirtyArray.GetEnumerator();
#endif
            throw new NotImplementedException();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
#if QUICKTUPLE
            return QuickAndDirtyArray.GetEnumerator();
#endif
            throw new NotImplementedException();
        }



        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToJson() {
            return "[TODO]";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public override int ToJsonUtf8(out byte[] buffer) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override byte[] ToJsonUtf8() {
            throw new NotImplementedException();
        }

    }
}

