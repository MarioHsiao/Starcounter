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
using Starcounter.XSON;

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
#if JSONINSTANCECOUNTER
            AssignInstanceNumber();
#endif
            trackChanges = false;
            checkBoundProperties = true;
            cacheIndexInArr = -1;
            transaction = TransactionHandle.Invalid;
            appName = StarcounterEnvironment.AppName;
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
#if JSONINSTANCECOUNTER
            AssignInstanceNumber();
#endif
            trackChanges = false;
            checkBoundProperties = true;
            cacheIndexInArr = -1;
            transaction = TransactionHandle.Invalid;
            AttachCurrentTransaction();
            data = result;
            pendingEnumeration = true;
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
            TValue itemTemplate;

            if (Template == null) {
                Template = template;
                Parent = parent;
            }

            if (this.pendingEnumeration) {
                var notEnumeratedResult = (IEnumerable)this.data;
                var elementType = template.ElementType;

                if (notEnumeratedResult != null) {
                    foreach (var entity in notEnumeratedResult) {
                        if (entity is Json) {
                            ((IList)this).Add(entity);
                        } else {
                            if (elementType == null) {
                                // no type specified on the array. Create one for this item depending on type
                                itemTemplate = DynamicFunctions.GetTemplateFromType(entity.GetType(), true);
                                newApp = (Json)itemTemplate.CreateInstance(this);
                            } else {
                                newApp = (Json)elementType.CreateInstance(this);
                            }

                            // Setting only the reference to the data first to allow bindings 
                            // and other stuff be handled then setting the property data after 
                            // the new item have been added to have the callback to usercode.
                            newApp.data = entity;
                            ((IList)this).Add(newApp);
                            newApp.Data = entity;
                        }
                    }
                }
                this.pendingEnumeration = false;
            }
            parent?.CallHasChanged(template);
        }

        internal void UpdateCachedIndex() {
            cacheIndexInArr = ((IList)Parent).IndexOf(this);
        }

        private void CheckAndAddArrayVersionLog(ChangeLog clog) {
            if (clog.Version != null && this.IsArray) {
                if (this.versionLog == null)
                    this.versionLog = new List<ArrayVersionLog>();
                this.versionLog.Add(
                    new ArrayVersionLog(clog.Version.LocalVersion,
                                        this.arrayAddsAndDeletes)
                );
            }
        }

        internal int TransformIndex(ViewModelVersion version, long fromVersion, int orgIndex) {
            int transformedIndex;
            long currentVersion;
            
            if ((dirty == false) && (versionLog == null || versionLog.Count == 0))
                return orgIndex;
            
            currentVersion = version.LocalVersion;
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
            
            if (transformedIndex != -1 && this.arrayAddsAndDeletes != null) {
                // There are current changes made that haven't been pushed to client yet.
                transformedIndex = FindAndTransformIndex(this.arrayAddsAndDeletes, transformedIndex);
            }
            
            return transformedIndex;
        }

        private int FindAndTransformIndex(List<Change> changes, int index) {
            Change change;
            int transformedIndex = index;

            if (changes == null)
                return -1;

            for (int i = 0; i < changes.Count; i++) {
                change = changes[i];

                if (change.ChangeType == Change.ADD) {
                    // If the type of change is add and index on change is equal or lower than the specified index
                    // we increase it, otherwise we ignore it.
                    if (change.Index <= transformedIndex)
                        transformedIndex++;
                } else if (change.ChangeType == Change.REMOVE) {
                    // If index in change is set to int.MaxValue, the whole list have been cleared.
                    // If index in change is equal to transformed index, it is invalid.
                    // If the index in change is lower than transformed index we decrease the transformed index.

                    if (change.Index == int.MaxValue) { // All items removed.
                        if (change.FromIndex >= transformedIndex) { // FromIndex = highest index of the removed items.
                            transformedIndex = -1;
                            break;
                        }
                    } else if (change.Index < transformedIndex) {
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

