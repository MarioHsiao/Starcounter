
using Starcounter.Advanced;
using Starcounter.Templates;
using System;
using System.Collections;
namespace Starcounter {
    partial class Arr {
        /// <summary>
        /// 
        /// </summary>
        internal IEnumerable notEnumeratedResult = null;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="result"></param>
        protected Arr(IEnumerable result) {
            notEnumeratedResult = result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="res"></param>
        /// <returns></returns>
        public static implicit operator Arr(Rows res) {
            return new Arr(res);
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
                    if (entity is IBindable) {
                        newApp = (Obj)template.ElementType.CreateInstance(this);
                        newApp.Data = (IBindable)entity;
                        Add(newApp);
                    } else if (entity is Obj) {
                        Add((Obj)entity);
                    }
                    else {
                        throw new Exception(String.Format(
                            "Cannot add a {0} to a Json array",entity.GetType().Name));
                    }
                }
                notEnumeratedResult = null;
            }
        }
    }
}
