using Starcounter.Advanced;
using Starcounter.Templates;
using System;
using System.Collections;

namespace Starcounter {

    partial class Json {

        /// <summary>
        /// 
        /// </summary>
        internal IEnumerable notEnumeratedResult = null;

        /// <summary>
        /// Initializes this Arr and sets the template and parent if not already done.
        /// If the notEnumeratedResult is not null the list is filled from the sqlresult.
        /// </summary>
        /// <paramCheckpointChangeLogparent"></param>
        /// <param name="template"></param>
        /// <remarks>
        /// This method can be called several times, the initialization only occurs once.
        /// </remarks>
        internal void Array_InitializeAfterImplicitConversion(Json parent, TObjArr template) {
            Json newApp;

            if (Template == null) {
                Template = template;
                Parent = parent;
            }

            if (notEnumeratedResult != null) {
                foreach (var entity in notEnumeratedResult) {
                    if (entity is IBindable) {
                        newApp = (Json)template.ElementType.CreateInstance(this);
                        newApp.Data = (IBindable)entity;
                        Add(newApp);
                    }
                    else if (entity is Json) {
                        Add((Json)entity);
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
