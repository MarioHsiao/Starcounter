
using System.Collections;
using System.Collections.Generic;
using Starcounter.Templates;

namespace Starcounter {
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Arr<T> : Json, IEnumerable<T> where T : Json, new() {
        internal class ArrEnumeratorWrapper<WT> : IEnumerator<WT> {
            private IEnumerator real;
            internal ArrEnumeratorWrapper(IEnumerator real) { this.real = real; }
            public WT Current { get { return (WT)real.Current; } }
            public void Dispose() { }
            object IEnumerator.Current { get { return real.Current; } }
            public bool MoveNext() { return real.MoveNext(); }
            public void Reset() { real.Reset(); }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="res"></param>
        /// <returns></returns>
        public static implicit operator Arr<T>(Rows res) {
            return new Arr<T>(res);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="result"></param>
        protected Arr(IEnumerable result)
            : base(result) {
        }

        /// <summary>
        /// 
        /// </summary>
        public Arr()
            : base() {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="templ"></param>
        public Arr(Json parent, TObjArr templ)
            : base(parent, templ) {
        }

        public T Add() {
            var template = ((TObjArr)this.Template).ElementType;
            var item = (template != null) ? (T)template.CreateInstance() : new T();
            _Add(item);
            return item;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public int IndexOf(T item) {
            return ((IList)this).IndexOf(item);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        public void Insert(int index, Json item) {
            ((IList)this).Insert(index, (object)item);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        public void Add(T item) {
            ((IList)this).Add(item);
        }

        public int Count {
            get {
                return ((IList)this).Count;
            }
        }

        public void RemoveAt(int index) {
            ((IList)this).RemoveAt(index);
        }

        public void Clear() {
            ((IList)this).Clear();
        }

        public bool Remove(T item) {
            return base.Remove(item);
        }

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="data"></param>
        ///// <returns></returns>
        //public T Add(object data) {
        //    T app;
        //    TObjArr template = (TObjArr)Template;

        //    if (template.ElementType == null) {
        //        app = new T();
        //        app.CreateDynamicTemplate();
        //    } else {
        //        app = (T)template.ElementType.CreateInstance(this);
        //    }
        //    app.Data = data;
        //    Add(app);
        //    return app;
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public new T this[int index] {
            get {
                return (T)base[index];
            }
            set {
                base[index] = value;
            }
        }

        public IEnumerator<T> GetEnumerator() {
            return new ArrEnumeratorWrapper<T>(((IList)this).GetEnumerator());
        }

    }
}
