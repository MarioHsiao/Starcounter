// ***********************************************************************
// <copyright file="AppList.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections;
using System.Collections.Generic;
using Starcounter.Internal;
using Starcounter.Internal.XSON;
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
            _trackChanges = false;
            _checkBoundProperties = true;
            _cacheIndexInArr = -1;
            _transaction = TransactionHandle.Invalid;
            AttachCurrentTransaction();
            this.Template = templ;
            Parent = parent;
        }

        /// <summary>
        /// Creates a Json array bound to a enumerable data source such as
        /// for example a SQL query result.
        /// </summary>
        /// <param name="result">The data source</param>
        protected Json(IEnumerable result) {
            _trackChanges = false;
            _checkBoundProperties = true;
            _cacheIndexInArr = -1;
            _transaction = TransactionHandle.Invalid;
            AttachCurrentTransaction();
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

                        // Setting only the reference to the data first to allow bindings 
                        // and other stuff be handled then setting the property data after 
                        // the new item have been added to have the callback to usercode.
                        newApp._data = entity;
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

        internal int TransformIndex(long fromVersion, int orgIndex) {
            int transformedIndex;
            Session s;
            long currentVersion;
            
            if ((_Dirty == false) && (versionLog == null || versionLog.Count == 0))
                return orgIndex;

            s = Session;
            currentVersion = s.ServerVersion;
            transformedIndex = orgIndex;

            if (versionLog != null) {
                // There might be old changes logged for this array. We need to find all changes
                // from the specified version to current for the index.
                for (int i = 0; i < versionLog.Count; i++) {
                    if (versionLog[i].Version <= fromVersion)
                        continue;
                    transformedIndex = FindAndTransformIndex(versionLog[i].Changes, transformedIndex);
                    if (transformedIndex == -1)
                        break;
                }
            }

            if (transformedIndex != -1 && ArrayAddsAndDeletes != null) {
                // There are current changes made that haven't been pushed to client yet.
                transformedIndex = FindAndTransformIndex(ArrayAddsAndDeletes, transformedIndex);
            }
            return transformedIndex;
        }

        private int FindAndTransformIndex(List<Change> changes, int index) {
            Change change;
            int transformedIndex = index;

            for (int i = 0; i < changes.Count; i++) {
                change = changes[i];

                if (change.ChangeType == Change.ADD) {
                    // If the type of change is add and index on change is equal or lower than the specified index
                    // we increase it, otherwise we ignore it.
                    if (change.Index <= transformedIndex)
                        transformedIndex++;
                } else if (change.ChangeType == Change.REMOVE) {
                    // If the type of change is remove and index in change is equal to transformed index, it is invalid.
                    // If the index in change is lower than transformed index we decrease the transformed index.
                    if (change.Index < transformedIndex) {
                        transformedIndex--;
                    } else if (change.Index == transformedIndex) {
                        transformedIndex = -1;
                        break;
                    }
                } else {
                    if (change.Index == transformedIndex) {
                        transformedIndex = -1;
                        break;
                    }
                }
            }

            return transformedIndex;
        }
    }
}

