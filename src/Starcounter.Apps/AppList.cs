// ***********************************************************************
// <copyright file="AppList.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using Starcounter.Templates.Interfaces;
using Starcounter;

#if CLIENT
using Starcounter.Client.Template;
namespace Starcounter.Client {
#else
using Starcounter.Templates;
using Starcounter.Apps;
namespace Starcounter {
#endif
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Listing<T> : Listing where T : App, new() {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="res"></param>
        /// <returns></returns>
        public static implicit operator Listing<T>(SqlResult res) {
            return new Listing<T>(res);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="result"></param>
        protected Listing(SqlResult result) : base(result) {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="templ"></param>
        public Listing(App parent, ListingProperty templ)
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
            var app = new T();
            Add(app);
            return app;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public T Add(Entity data) {
            ListingProperty template = (ListingProperty)Template;
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
    public class Listing : AppNode, IList<App>
#if IAPP
, IAppArray
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
        public static implicit operator Listing(SqlResult res) {
            return new Listing(res);
        }

#if QUICKTUPLE
        /// <summary>
        /// Temporary. Should be replaced by TupleProxy functionality
        /// </summary>
        internal List<App> QuickAndDirtyArray = new List<App>();
#endif
        // private AppListTemplate _property;

        /// <summary>
        /// 
        /// </summary>
        public App Current {
            get {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="result"></param>
        protected Listing(SqlResult result) {
            notEnumeratedResult = result;
        }

        /// <summary>
        /// Initializes this Listing and sets the template and parent if not already done.
        /// If the notEnumeratedResult is not null the list is filled from the sqlresult.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="template"></param>
        /// <remarks>
        /// This method can be called several times, the initialization only occurs once.
        /// </remarks>
        internal void InitializeAfterImplicitConversion(App parent, ListingProperty template) {
            App newApp;

            if (Template == null) {
                Template = template;
                Parent = parent;
            }

            if (notEnumeratedResult != null) {
                foreach (var entity in notEnumeratedResult) {
                    newApp = (App)template.App.CreateInstance(this);
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
        public Listing(App parent, ListingProperty templ) {
            this.Template = templ;
            Parent = parent;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public int IndexOf(App item) {
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
        public void Insert(int index, App item) {
            App otherItem;
            ListingProperty template;

#if QUICKTUPLE
            QuickAndDirtyArray.Insert(index, item);
#else
         throw new JockeNotImplementedException();
#endif
            template = (ListingProperty)this.Template;
            Session.Current.changeLog.AddItemInList((App)this.Parent, template, index);

            for (Int32 i = index + 1; i < QuickAndDirtyArray.Count; i++) {
                otherItem = QuickAndDirtyArray[i];
                otherItem._cacheIndexInList = i;
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        public void RemoveAt(int index) {
            App otherItem;
            ListingProperty template;

#if QUICKTUPLE

            template = (ListingProperty)this.Template;
            QuickAndDirtyArray.RemoveAt(index);
            Session.Current.changeLog.RemoveItemInList((App)this.Parent, template, index);

            for (Int32 i = index; i < QuickAndDirtyArray.Count; i++) {
                otherItem = QuickAndDirtyArray[i];
                otherItem._cacheIndexInList = i;
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
        public bool Remove(App item) {
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
        public App this[int index] {
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
        public App Add() {
#if QUICKTUPLE
            App x = (App)((ListingProperty)this.Template).App.CreateInstance(this);

            //            var x = new App() { Template = ((ListingProperty)this.Template).App };
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
        internal override void OnSetParent(AppNode item) {
            base.OnSetParent(item);
            //            QuickAndDirtyArray.Add((App)item);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        public void Add(App item) {
            Int32 index;

#if QUICKTUPLE
            index = QuickAndDirtyArray.Count;
            QuickAndDirtyArray.Add(item);
            item._cacheIndexInList = index;
            item.Parent = this;
#else
         throw new JockeNotImplementedException();
#endif

            Session.Current.changeLog.AddItemInList((App)this.Parent, (ListingProperty)this.Template, QuickAndDirtyArray.Count - 1);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Clear() {
            int indexesToRemove;
            App app = (App)this.Parent;
            ListingProperty property = (ListingProperty)this.Template;

#if QUICKTUPLE

            indexesToRemove = QuickAndDirtyArray.Count;
            for (int i = (indexesToRemove - 1); i >= 0; i--) {
                Session.Current.changeLog.RemoveItemInList(app, property, i);
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
        public bool Contains(App item) {
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
        public void CopyTo(App[] array, int arrayIndex) {
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

        IEnumerator<App> IEnumerable<App>.GetEnumerator() {
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

