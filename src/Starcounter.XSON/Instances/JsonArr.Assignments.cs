
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

        public override void HasAddedElement(TObjArr property, int elementIndex) {
        }

        public override void HasRemovedElement(TObjArr property, int elementIndex) {
        }



        /// <summary>
        /// Initializes this Arr and sets the template and parent if not already done.
        /// If the notEnumeratedResult is not null the list is filled from the sqlresult.
        /// </summary>
        /// <paramCheckpointChangeLogparent"></param>
        /// <param name="template"></param>
        /// <remarks>
        /// This method can be called several times, the initialization only occurs once.
        /// </remarks>
        internal void InitializeAfterImplicitConversion(Json<object> parent, TObjArr template) {
            Json<object> newApp;

            if (Template == null) {
                Template = template;
                Parent = parent;
            }

            if (notEnumeratedResult != null) {
                foreach (var entity in notEnumeratedResult) {
                    if (entity is IBindable) {
                        newApp = (Json<object>)template.ElementType.CreateInstance(this);
                        newApp.Data = (IBindable)entity;
                        Add(newApp);
                    }
                    else if (entity is Json<object>) {
                        Add((Json<object>)entity);
                    }
                    else {
                        throw new Exception(String.Format(
                            "Cannot add a {0} to a Json array",entity.GetType().Name));
                    }
                }
                notEnumeratedResult = null;
            }

            parent._CallHasChanged(template);

        }
    }
}
