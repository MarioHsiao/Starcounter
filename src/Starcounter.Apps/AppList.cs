
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
    public class Listing<T> : Listing where T : App, new()
    {
        public static implicit operator Listing<T>(SqlResult res)
        {
            throw new NotImplementedException();
        }

        public Listing(App parent, ListingProperty templ)
            : base(parent, templ)
        {
        }

        public new T Current
        {
            get
            {
                return (T)base.Current;
            }
        }
        public new T Add()
        {
            var app = new T();
            Add(app);
            return app;
        }
        public T Add(Entity data)
        {
            var app = new T() { Data = data };
            Add(app);
            return app;
        }

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


    public class Listing : AppNode, IList<App>
#if IAPP
, IAppArray
#endif
    {

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

        public App Current
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public Listing(App parent, ListingProperty templ)
        {
            //  _property = templ;
            this.Template = templ;
            Parent = parent;
        }

        public int IndexOf(App item)
        {
#if QUICKTUPLE
            return QuickAndDirtyArray.IndexOf(item);
#else
         throw new JockeNotImplementedException();
#endif
        }

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

        internal override void OnSetParent(AppNode item)
        {
            base.OnSetParent(item);
//            QuickAndDirtyArray.Add((App)item);
        }

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

        public void Clear()
        {
#if QUICKTUPLE
            QuickAndDirtyArray.Clear();
#else
         throw new JockeNotImplementedException();
#endif
        }

        public bool Contains(App item)
        {
#if QUICKTUPLE
            return QuickAndDirtyArray.Contains(item);
#else
         throw new JockeNotImplementedException();
#endif
        }

        public void CopyTo(App[] array, int arrayIndex)
        {
            throw new JockeNotImplementedException();
        }

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

    public class JockeNotImplementedException : NotImplementedException
    {

    }
}

