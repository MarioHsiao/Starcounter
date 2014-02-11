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
        
        public Json(Json parent, TObjArr templ) {
            this.Template = templ;
            Parent = parent;
        }

        /// <summary>
        /// Creates a Json array bound to a enumerable data source such as
        /// for example a SQL query result.
        /// </summary>
        /// <param name="result">The data source</param>
        protected Json(IEnumerable result) {
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
                        Add(entity);
                    } else {
                        var tobj = template.ElementType;
                        if (tobj == null) {
                            template.CreateElementTypeFromDataObject(entity);
                            tobj = template.ElementType;
                        }
                        newApp = (Json)tobj.CreateInstance(this);
                        Add(newApp);
                        newApp.Data = entity;
                    }
                }
                _PendingEnumeration = false;
            }
            parent._CallHasChanged(template);
        }

        internal void InternalClear() {
            int indexesToRemove;
            var app = this.Parent;
            TObjArr property = (TObjArr)this.Template;
            indexesToRemove = list.Count;
            for (int i = (indexesToRemove - 1); i >= 0; i--) {
                app.ChildArrayHasRemovedAnElement(property, i);
            }
            list.Clear();
        }

        public Json Add() {
            var elementType = ((TObjArr)this.Template).ElementType;
            Json x;
            if (elementType == null) {
                x = new Json();
            } else {
                x = (Json)elementType.CreateInstance(this);
            }

            //            var x = new App() { Template = ((TArr)this.Template).App };
            Add(x);
            return x;
        }
    }
}

