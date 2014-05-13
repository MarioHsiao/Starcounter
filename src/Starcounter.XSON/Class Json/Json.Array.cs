// ***********************************************************************
// <copyright file="AppList.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Collections;
using Starcounter.Templates;

namespace Starcounter {
    public partial class Json {
        /// <summary>
        /// You can assign a result set from a SQL query operation directly to 
        /// a JSON array property.
        /// <example>
        /// myJson.Items = Db.SQL("SELECT i FROM Items i");
        /// </example>
        /// </summary>
        /// <param name="res">The SQL result set</param>
        /// <returns></returns>
        public static implicit operator Json(Rows res) {
            return new Json(res);
        }
        
        internal Json(Json parent, TObjArr templ) {
            _dirtyCheckEnabled = DirtyCheckEnabled;
            this.Template = templ;
            Parent = parent;
        }

        /// <summary>
        /// Creates a Json array bound to a enumerable data source such as
        /// for example a SQL query result.
        /// </summary>
        /// <param name="result">The data source</param>
        protected Json(IEnumerable result) {
            _dirtyCheckEnabled = DirtyCheckEnabled;
            _data = result;
            _PendingEnumeration = true;
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
        internal void Array_InitializeAfterImplicitConversion(Json parent, TObjArr template) {
            Json newApp;

            if (Template == null) {
                Template = template;
                Parent = parent;
            }

            if (_PendingEnumeration) {
                var notEnumeratedResult = (IEnumerable)_data;
                foreach (var entity in notEnumeratedResult) {
                    if (entity is Json) {
                        ((IList)this).Add(entity);
                    } else {
                        var tobj = template.ElementType;
                        if (tobj == null) {
                            template.CreateElementTypeFromDataObject(entity);
                            tobj = template.ElementType;
                        }
                        newApp = (Json)tobj.CreateInstance(this);
                        ((IList)this).Add(newApp);
                        newApp.Data = entity;
                    }
                }
                _PendingEnumeration = false;
            }
            parent.CallHasChanged(template);
        }

        internal void UpdateCachedIndex() {
            _cacheIndexInArr = ((IList)Parent).IndexOf(this);
        }
    }
}

