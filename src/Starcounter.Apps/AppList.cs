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
namespace Starcounter
{
#endif
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Listing<T> : Listing where T : App, new()
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="res"></param>
        /// <returns></returns>
        public static implicit operator Listing<T>(SqlResult res)
        {
            throw new NotImplementedException();
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="templ"></param>
        public Listing(App parent, ListingProperty templ)
            : base(parent, templ)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public new T Current
        {
            get
            {
                return (T)base.Current;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public new T Add()
        {
            var app = new T();
            Add(app);
            return app;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public T Add(Entity data)
        {
            var app = new T() { Data = data };
            Add(app);
            return app;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public new T this[int index]
        {
            get
            {
#if QUICKTUPLE
                return (T)QuickAndDirtyArray[index];
#else
            throw new JockeNotImplementedException();
#endif
            }
            set
            {
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
        /// <param name="res"></param>
        /// <returns></returns>
        public static implicit operator Listing(SqlResult res)
        {
            throw new NotImplementedException();
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
        public App Current
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="templ"></param>
        public Listing(App parent, ListingProperty templ)
        {
            //  _property = templ;
            this.Template = templ;
            Parent = parent;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public int IndexOf(App item)
        {
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
        public void Insert(int index, App item)
        {
            App otherItem;
            ListingProperty template;

#if QUICKTUPLE
            QuickAndDirtyArray.Insert(index, item);
#else
         throw new JockeNotImplementedException();
#endif
            template = (ListingProperty)this.Template;
            ChangeLog.AddItemInList((App)this.Parent, template, index);

            for (Int32 i = index + 1; i < QuickAndDirtyArray.Count; i++)
            {
                otherItem = QuickAndDirtyArray[i];
                otherItem._cacheIndexInList = i;
            }
            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        public void RemoveAt(int index)
        {
            App otherItem;
            ListingProperty template;

#if QUICKTUPLE

            template = (ListingProperty)this.Template;
            QuickAndDirtyArray.RemoveAt(index);
            ChangeLog.RemoveItemInList((App)this.Parent, template, index);

            for (Int32 i = index; i < QuickAndDirtyArray.Count; i++)
            {
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
        public bool Remove(App item)
        {
            Boolean b;
            Int32 index;

#if QUICKTUPLE
            index = QuickAndDirtyArray.IndexOf(item);
            b = (index != -1);
            if (b) RemoveAt(index);
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
        public App this[int index]
        {
            get
            {
#if QUICKTUPLE
                return QuickAndDirtyArray[index];
#else
            throw new JockeNotImplementedException();
#endif
            }
            set
            {
                throw new JockeNotImplementedException();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public App Add()
        {
#if QUICKTUPLE
            var x = new App() { Template = ((ListingProperty)this.Template).App };
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
        internal override void OnSetParent(AppNode item)
        {
            base.OnSetParent(item);
//            QuickAndDirtyArray.Add((App)item);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        public void Add(App item)
        {
            Int32 index;

#if QUICKTUPLE
            index = QuickAndDirtyArray.Count;
            QuickAndDirtyArray.Add(item);
            item._cacheIndexInList = index;
            item.Parent = this;
#else
         throw new JockeNotImplementedException();
#endif

            ChangeLog.AddItemInList((App)this.Parent, (ListingProperty)this.Template, QuickAndDirtyArray.Count - 1);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Clear()
        {
#if QUICKTUPLE
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
        public bool Contains(App item)
        {
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
        public void CopyTo(App[] array, int arrayIndex)
        {
            throw new JockeNotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        public int Count
        {
            get
            {
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
        public bool IsReadOnly
        {
            get
            {
#if QUICKTUPLE
                return false;
#else
            throw new JockeNotImplementedException();
#endif
            }
        }

        IEnumerator<App> IEnumerable<App>.GetEnumerator()
        {
#if QUICKTUPLE
            return QuickAndDirtyArray.GetEnumerator();
#endif
            throw new JockeNotImplementedException();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
#if QUICKTUPLE
            return QuickAndDirtyArray.GetEnumerator();
#endif
            throw new JockeNotImplementedException();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class JockeNotImplementedException : NotImplementedException
    {

    }
}

