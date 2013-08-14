

using Starcounter.Advanced;
using Starcounter.Templates;
using System;
using System.Collections;
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
            TObjArr template = (TObjArr)Template;
            var app = (T)template.App.CreateInstance(this);
            Add(app);
            return app;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        public void Add(T item) {
            base.Add(item);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        public override void Add(Obj item) {
            var typedListTemplate = ((TObjArr)Template).App;
            if (typedListTemplate != null) {
                //                var t = allowedTemplate.GetType();
                if (item.Template != typedListTemplate) {
                    throw new Exception(
                        String.Format("Cannot add item with template {0} as the array is expecting another template of type {1}",
                                item.Template.GetType().Name,
                                typedListTemplate.GetType().Name));
                }
            }
            base.Add(item);
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
            throw new NotImplementedException();
#endif
            }
            set {
                throw new NotImplementedException();
            }
        }

    }

}
