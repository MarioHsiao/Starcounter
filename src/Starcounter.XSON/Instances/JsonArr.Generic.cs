

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
            /*
            TObjArr template = (TObjArr)Template;
            Template typed = template.ElementType;
            T app;
            if (typed != null) {
                app = (T)typed.CreateInstance(this);
            }
            else {
                throw new NotImplementedException();
//                app = new T();
//                app.Parent = this;
            }
            Add(app);
            return app;
             */

            TObjArr template = (TObjArr)Template;
            T app = new T();
            //app.Parent = this;

            Template typed = template.ElementType;
            if (typed != null) {
                app.Template = (TObj)typed;
            }
            else {
                app.CreateDynamicTemplate();
//                app.Template = new TJson();
//                CreateGe
            }
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
            var typedListTemplate = ((TObjArr)Template).ElementType;
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
            var app = (T)template.ElementType.CreateInstance(this);
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
