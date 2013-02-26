// ***********************************************************************
// <copyright file="AppList.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using Starcounter.Templates.Interfaces;
using Starcounter;

using Starcounter.Templates;
using Starcounter.Apps;
using Starcounter.Advanced;

namespace Starcounter {

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Arr<T> : Arr where T : Obj, new() {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="res"></param>
        /// <returns></returns>
        public static implicit operator Arr<T>(SqlResult res) {
            return new Arr<T>(res);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="result"></param>
        protected Arr(SqlResult result) : base(result) {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="templ"></param>
        public Arr(Obj parent, TObjArr templ)
            : base(parent, templ) {
        }

        /// <summary>
        /// 
        /// </summary>
        public new T Current {
            get {
                return (T)base.Current;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public new T Add() {
        //    var app = new T();
        //    Add(app);
        //    return app;
            TObjArr template = (TObjArr)Template;
            var app = (T)template.App.CreateInstance(this);

          //  app.Data = data;
            Add(app);
            return app;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public T Add(IBindable data) {
            TObjArr template = (TObjArr)Template;
            var app = (T)template.App.CreateInstance(this);
            
            app.Data = data;
            Add(app);
            return app;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public new T this[int index] {
            get {
#if QUICKTUPLE
                return (T)QuickAndDirtyArray[index];
#else
            throw new JockeNotImplementedException();
#endif
            }
            set {
                throw new JockeNotImplementedException();
            }
        }

    }

    /// <summary>
    /// 
    /// </summary>
    public class Arr : Container, IList<Obj>
#if IAPP
//, IAppArray
#endif
 {
        /// <summary>
        /// 
        /// </summary>
        internal SqlResult notEnumeratedResult = null;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="res"></param>
        /// <returns></returns>
        public static implicit operator Arr(SqlResult res) {
            return new Arr(res);
        }

#if QUICKTUPLE
        /// <summary>
        /// Temporary. Should be replaced by TupleProxy functionality
        /// </summary>
        internal List<Obj> QuickAndDirtyArray = new List<Obj>();
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
        /// <param name="result"></param>
        protected Arr(SqlResult result) {
            notEnumeratedResult = result;
        }

        /// <summary>
        /// Initializes this Arr and sets the template and parent if not already done.
        /// If the notEnumeratedResult is not null the list is filled from the sqlresult.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="template"></param>
        /// <remarks>
        /// This method can be called several times, the initialization only occurs once.
        /// </remarks>
        internal void InitializeAfterImplicitConversion(Obj parent, TObjArr template) {
            Obj newApp;

            if (Template == null) {
                Template = template;
                Parent = parent;
            }

            if (notEnumeratedResult != null) {
                foreach (var entity in notEnumeratedResult) {
                    newApp = (Obj)template.App.CreateInstance(this);
                    newApp.Data = entity;
                    Add(newApp);
                }
                notEnumeratedResult = null;
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
         throw new JockeNotImplementedException();
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
         throw new JockeNotImplementedException();
#endif
//            template = (TObjArr)this.Template;
//            ChangeLog.AddItemInList((Puppet)this.Parent, template, index);
            Parent.HasAddedElement((TObjArr)this.Template, index);

            for (Int32 i = index + 1; i < QuickAndDirtyArray.Count; i++) {
                otherItem = QuickAndDirtyArray[i];
                otherItem._cacheIndexInArr = i;
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        public void RemoveAt(int index) {
            Obj otherItem;
//            TObjArr template;

#if QUICKTUPLE

//            template = (TObjArr)this.Template;
            QuickAndDirtyArray.RemoveAt(index);
//            ChangeLog.RemoveItemInList((Puppet)this.Parent, template, index);
            this.HasRemovedElement((TObjArr)this.Template, index);

            for (Int32 i = index; i < QuickAndDirtyArray.Count; i++) {
                otherItem = QuickAndDirtyArray[i];
                otherItem._cacheIndexInArr = i;
            }
#else
         throw new JockeNotImplementedException();
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
         throw new JockeNotImplementedException();
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
            throw new JockeNotImplementedException();
#endif
            }
            set {
                throw new JockeNotImplementedException();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Obj Add() {
#if QUICKTUPLE
            Obj x = (Obj)((TObjArr)this.Template).App.CreateInstance(this);

            //            var x = new App() { Template = ((TArr)this.Template).App };
            Add(x);
            return x;
#else
         throw new JockeNotImplementedException();
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        internal override void OnSetParent(Container item) {
            base.OnSetParent(item);
            //            QuickAndDirtyArray.Add((App)item);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        public void Add(Obj item) {
            Int32 index;

#if QUICKTUPLE
            index = QuickAndDirtyArray.Count;
            QuickAndDirtyArray.Add(item);
            item._cacheIndexInArr = index;
            item.Parent = this;
#else
         throw new JockeNotImplementedException();
#endif
            Parent.HasAddedElement((TObjArr)this.Template, QuickAndDirtyArray.Count - 1);
        }



        /// <summary>
        /// 
        /// </summary>
        public void Clear() {
            int indexesToRemove;
            var app = this.Parent;
            TObjArr property = (TObjArr)this.Template;

#if QUICKTUPLE

            indexesToRemove = QuickAndDirtyArray.Count;
            for (int i = (indexesToRemove - 1); i >= 0; i--) {
                app.HasAddedElement(property, i );
            }
            QuickAndDirtyArray.Clear();
#else
         throw new JockeNotImplementedException();
#endif
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
         throw new JockeNotImplementedException();
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(Obj[] array, int arrayIndex) {
            throw new JockeNotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        public int Count {
            get {
#if QUICKTUPLE
                return QuickAndDirtyArray.Count;
#else
            throw new JockeNotImplementedException();
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
            throw new JockeNotImplementedException();
#endif
            }
        }

        IEnumerator<Obj> IEnumerable<Obj>.GetEnumerator() {
#if QUICKTUPLE
            return QuickAndDirtyArray.GetEnumerator();
#endif
            throw new JockeNotImplementedException();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
#if QUICKTUPLE
            return QuickAndDirtyArray.GetEnumerator();
#endif
            throw new JockeNotImplementedException();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class JockeNotImplementedException : NotImplementedException {

    }
}

