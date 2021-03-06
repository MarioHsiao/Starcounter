﻿using System;
using System.Collections;
using System.Collections.Generic;
using Starcounter.Advanced;
using Starcounter.Advanced.XSON;
using Starcounter.Internal;
using Starcounter.Internal.XSON;
using Starcounter.Templates;
using Starcounter.XSON;

namespace Starcounter {
    partial class Json {
        /// <summary>
        /// If true changes in TypedJSON are tracked and stored.
        /// </summary>
        internal bool IsTrackingChanges {
            get {
                return this.trackChanges;
            }
        }

		/// <summary>
		/// Marks this instance and all parents as dirty, i.e. some value
        /// in the instance have changed.
		/// </summary>
        /// <remarks>
        /// If IsTrackingChanges is false, then this method will simply exit
        /// without doing anything. Same will happen if it's already dirty.
        /// </remarks>
		internal void Dirtyfy(bool callStepSiblings = true) {
            if (!this.trackChanges || (this.dirty == true))
                return;
            
			this.dirty = true;
			if (Parent != null)
				Parent.Dirtyfy();

            if (callStepSiblings == true && this.siblings != null) {
                foreach (Json stepSibling in this.siblings) {
                    if (stepSibling == this)
                        continue;
                    stepSibling.Dirtyfy(false);
                }
            }
		}
        
        /// <summary>
        /// Returns true if any property is marked as dirty.
        /// </summary>
        /// <returns></returns>
        public bool IsDirty() {
            return trackChanges && this.dirty;
        }

        /// <summary>
        /// Returns true if the property is marked as dirty.
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public bool IsDirty(Template property) {
#if DEBUG
            this.Template.VerifyProperty(property);
#endif
            if (this.trackChanges)
                return (IsDirty(property.TemplateIndex));
            return false;
        }

        /// <summary>
        /// Returns true if the template with the specified index is marked as dirty.
        /// </summary>
        /// <param name="templateIndex"></param>
        /// <returns></returns>
        internal bool IsDirty(int templateIndex) {
            return ((stateFlags[templateIndex] & PropertyState.Dirty) == PropertyState.Dirty);
        }

        internal bool IsCached(Template template) {
#if DEBUG
            this.Template.VerifyProperty(template);
#endif
            return IsCached(template.TemplateIndex);
        }

        /// <summary>
        /// Returns true if the template with the specified index is marked as cached.
        /// </summary>
        /// <param name="templateIndex"></param>
        /// <returns></returns>
        internal bool IsCached(int templateIndex) {
            if (stateFlags != null && templateIndex != -1) 
                return ((stateFlags[templateIndex] & PropertyState.Cached) == PropertyState.Cached);
            return false;
        }
        
        /// <summary>
        /// Marks the specified property as dirty.
        /// </summary>
        /// <param name="property"></param>
        internal void MarkAsDirty(Template property) {
            this.MarkAsDirty(property.TemplateIndex);
        }

        /// <summary>
        /// Marks the property with the specified index as dirty.
        /// </summary>
        /// <param name="index"></param>
        internal void MarkAsDirty(int templateIndex) {
            stateFlags[templateIndex] |= PropertyState.Dirty;
            this.Dirtyfy();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="templateIndex"></param>
        private void MarkAsNonDirty(int templateIndex) {
            stateFlags[templateIndex] &= ~PropertyState.Dirty;
        }

        /// <summary>
        /// Marks the property with the specified index as cached.
        /// </summary>
        /// <param name="templateIndex"></param>
        internal void MarkAsCached(int templateIndex) {
            stateFlags[templateIndex] |= PropertyState.Cached;
        }
        
        /// <summary>
        /// Resets the stateflags for the property with the specified index.
        /// </summary>
        /// <param name="templateIndex"></param>
        internal void CheckpointAt(int templateIndex) {
            stateFlags[templateIndex] = PropertyState.Default;
        }

        /// <summary>
        /// Checkpoints this instance and all children and resets state and dirtyflags.
        /// </summary>
        internal void ScopeAndCheckpoint(bool callStepSiblings = true) {
            if (!this.trackChanges)
                return;

            this.Scope(() => {
                this.Checkpoint();

                if (callStepSiblings == true && this.siblings != null) {
                    for (int i = 0; i < this.siblings.Count; i++) {
                        var sibling = siblings[i];
                        this.siblings.MarkAsSent(i);

                        if (sibling == this)
                            continue;

                        sibling.ScopeAndCheckpoint(false);
                        if (sibling.Parent != null && sibling.Parent.IsTrackingChanges) {
                            sibling.Parent.CheckpointAt(sibling.IndexInParent);
                        }
                    }
                }
            });
			dirty = false;
		}

        /// <summary>
        /// Checkpoints this instance and all children. Possible to override to
        /// add custom behaviour.
        /// </summary>
        public virtual void Checkpoint() {
            if (!trackChanges)
                return;

            if (this.IsArray) {
                this.arrayAddsAndDeletes = null;
                for (int i = 0; i < ((IList)this).Count; i++) {
                    var row = (Json)this._GetAt(i);
                    row.ScopeAndCheckpoint();
                    this.CheckpointAt(i);
                }
            } else {
                if (Template != null) {
                    if (this.IsObject) {
                        TObject tobj = (TObject)Template;
                        for (int i = 0; i < tobj.Properties.ExposedProperties.Count; i++) {
                            var property = tobj.Properties.ExposedProperties[i] as TValue;
                            if (property != null) {
                                property.Checkpoint(this);
                            }
                        }
                    } else {
                        ((TValue)Template).Checkpoint(this);
                    }
                }
            }
        }

        /// <summary>
        /// Dirty checks each value of the object and adds any changes
        /// to the changelog.
        /// </summary>
        /// <param name="changeLog">Log of changes</param>
        internal void ScopeAndCollectChanges(ChangeLog changeLog, bool callStepSiblings) {
            if (!this.trackChanges)
                return;

            this.Scope(() => {
                this.CollectChanges(changeLog);

                if (callStepSiblings == true && this.siblings != null) {
                    for (int i = 0; i < this.siblings.Count; i++) {
                        var sibling = this.siblings[i];

                        if (sibling == this)
                            continue;

                        if (this.siblings.HasBeenSent(i)) {
                            sibling.ScopeAndCollectChanges(changeLog, false);
                        } else {
                            changeLog.Add(Change.Update(sibling, null, true));

                            // TODO:
                            // Same questions as above. Why do we reset these flags here and not in checkpoint?
                            this.siblings.MarkAsSent(i);
                            sibling.dirty = false;
                        }
                    }
                }
            });
		}
        
        /// <summary>
        /// Collects the changes for this json. Possible to override to add custom
        /// logic for handling the checks.
        /// </summary>
        /// <remarks>This method assumes that the correct long-running transaction is already scoped.</remarks>
        /// <param name="changeLog">The log of changes</param>
        public virtual void CollectChanges(ChangeLog changeLog) {
            if (!trackChanges)
                return;

            var template = (TValue)Template;
            if (template != null) {
                if (template.TemplateTypeId == TemplateTypeEnum.Object) {
                    var exposed = ((TObject)template).Properties.ExposedProperties;
                    for (int t = 0; t < exposed.Count; t++) {
                        CheckOneTemplate(this, (TValue)exposed[t], changeLog);
                    }
                } else if (template.TemplateTypeId != TemplateTypeEnum.Array) {
                    CheckOneTemplate(this, template, changeLog);
                } else {
                    CollectChangesForArray(this, changeLog);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="changeLog"></param>
        private static void CollectChangesForArray(Json arr, ChangeLog changeLog) {
            bool logChanges;
            Json item;

            if (arr.arrayAddsAndDeletes != null && arr.arrayAddsAndDeletes.Count > 0) {
                for (int i = 0; i < arr.arrayAddsAndDeletes.Count; i++) {
                    var change = arr.arrayAddsAndDeletes[i];

                    if (change.ChangeType == Change.REMOVE && change.Index == int.MaxValue) {
                        // TBH I'm not sure we can ever get here when having a remove all (it should refresh 
                        // the whole array), but if we do we treat it as adding a remove change for each item 
                        // removed (i.e count in change).
                        for (int k = 0; k < change.FromIndex; k++) {
                            changeLog.Add(Change.Remove(change.Parent, (TObjArr)change.Property, k, null));
                        }
                        continue;
                    } 

                    changeLog.Add(change);
                    var index = change.Item.cacheIndexInArr;
                    
                    if (change.ChangeType != Change.REMOVE && index >= 0 && index < arr.valueList.Count) {
                        arr.MarkAsNonDirty(index);
                        item = change.Item;
                        item.SetBoundValuesInTuple();
                        item.dirty = false;
                    }
                }

                for (int i = 0; i < arr.valueList.Count; i++) {
                    // Skip all items we have already added to the changelog.
                    logChanges = true;
                    foreach (Change change in arr.arrayAddsAndDeletes) {
                        if (change.ChangeType != Change.REMOVE && change.Index == i) {
                            logChanges = false;
                            break;
                        }
                    }

                    if (logChanges) {
                        ((Json)arr.valueList[i]).ScopeAndCollectChanges(changeLog, true);
                    }
                }

                arr.CheckAndAddArrayVersionLog(changeLog);
                arr.arrayAddsAndDeletes = null;
            } else {
                for (int t = 0; t < arr.valueList.Count; t++) {
                    var arrItem = ((Json)arr.valueList[t]);
                    if (arr.IsDirty(t)) { // A refresh of an existing row (that is not added or removed)
                        changeLog.Add(Change.Update(arr.Parent, (TValue)arr.Template, t, arrItem));
                        arr.MarkAsNonDirty(t);
                    } else {
                        arrItem.ScopeAndCollectChanges(changeLog, true);
                    }
                }
            }
            arr.dirty = false;
		}
        
        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// This method assumes that the correct transaction is set before calling.
        /// </remarks>
        /// <param name="parent"></param>
        /// <param name="template"></param>
        private static void CheckOneTemplate(Json parent, TValue template, ChangeLog changeLog) {
            if (parent.IsDirty(template.TemplateIndex)) {
                changeLog.UpdateValue(parent, template);
                template.CheckAndSetBoundValue(parent, false);

                // TODO:
                // Would like to avoid this check and call here, but the method 
                // CheckAndSetBoundValue for array is called from places where we dont want
                // to add a new versionlog.
                if (template.TemplateTypeId == TemplateTypeEnum.Array) {
                    ((TObjArr)template).UnboundGetter(parent).CheckAndAddArrayVersionLog(changeLog);
                }
                
            } else if (parent.checkBoundProperties) {
                if (template is TContainer) {
                    var c = ((TContainer)template).GetValue(parent);
                    if (c != null)
                        c.ScopeAndCollectChanges(changeLog, true);
                } else {
                    template.CheckAndSetBoundValue(parent, true);

                    // TODO:
                    // Check these lines. Why do we need them. We add it in the method above if needed.
                    if (parent.IsDirty(template.TemplateIndex))
                        changeLog.UpdateValue(parent, template);
                }
            }

            // TODO: 
            // Why do we reset the dirtyflag here? It will be done when checkpointing later.
            // Is it because we dont want to handle it twice? In that case there should be another 
            // flag set to mark it as processed (that will also be resetted during checkpointing).
            parent.MarkAsNonDirty(template.TemplateIndex);
        }
        
        /// <summary>
        /// The automatic dirtycheck for bound properties works by setting the current 
        /// value from the dataobject as the unbound value. This method will set all 
        /// current values starting from this instance and mark these properties as cached,
        /// which means that the unbound value will be used until all changes have been 
        /// sent and viewmodels checkpointed.
        /// </summary>
        /// <param name="callStepSiblings"></param>
        internal void SetBoundValuesInTuple(bool callStepSiblings = true) {
            if (!IsTrackingChanges)
                return;

            this.Scope(() => {
                if (this.IsArray) {
                    foreach (Json row in ((IList)this)) {
                        row.SetBoundValuesInTuple();
                    }
                } else {
                    TValue tval = (TValue)this.Template;
                    if (tval != null) {
                        if (this.IsObject) {
                            var tobj = (TObject)tval;
                            for (int i = 0; i < tobj.Properties.Count; i++) {
                                var t = tobj.Properties[i] as TValue;
                                if (t != null)
                                    t.CheckAndSetBoundValue(this, false);
                            }
                        } else {
                            tval.CheckAndSetBoundValue(this, false);
                        }
                    }
                }

                if (callStepSiblings == true && this.siblings != null) {
                    foreach (var stepSibling in this.siblings) {
                        if (stepSibling == this)
                            continue;
                        stepSibling.SetBoundValuesInTuple(false);
                    }
                }
            });
        }

        /// <summary>
        /// Compares the existing dataobject with the one submitted as a parameter. 
        /// If they differ the new will be set as data.
        /// </summary>
        /// <remarks>
        /// If both dataobjects implements interface IBindable, the property IBindable.Identity 
        /// will be used for comparison, otherwise a reference equality will be performed.
        /// </remarks>
        /// <param name="boundValue"></param>
        internal void CheckBoundObject(object boundValue) {
            if (!CompareDataObjects(boundValue, Data))
                AttachData(boundValue, false);
        }

        /// <summary>
        /// Returns the index, starting from offset, of the specifed object
        /// using either IBindable.Identity (if available) or reference equality
        /// for comparison.
        /// </summary>
        /// <param name="list">The list to find the index in.</param>
        /// <param name="offset">The starting point in the list.</param>
        /// <param name="value">The value to find.</param>
        /// <returns></returns>
        private static int IndexOf(IList list, int offset, object value) {
            int index = -1;
            Json current;

            for (int i = offset; i < list.Count; i++) {
                current = (Json)list[i];
                if (CompareDataObjects(current.Data, value)) {
                    index = i;
                    break;
                }
            }
            return index;
        }

        /// <summary>
        /// Old method, not currently used, but kept to be able to run tests and check performance and differences.
        /// </summary>
        /// <param name="boundValue"></param>
        internal void CheckBoundArray_OLD(IEnumerable boundValue) {
            Json oldJson;
            Json newJson;
            int index = 0;
            TObjArr tArr = Template as TObjArr;
            bool hasChanged = false;

            foreach (object value in boundValue) {
                if (this.valueList.Count <= index) {
                    newJson = (Json)tArr.ElementType.CreateInstance();
                    newJson.data = value;
                    ((IList)this).Add(newJson);
                    newJson.Data = value;
                    hasChanged = true;
                } else {
                    oldJson = (Json)this.valueList[index];
                    if (!CompareDataObjects(oldJson.Data, value)) {
                        newJson = (Json)tArr.ElementType.CreateInstance();
                        newJson.data = value;
                        ((IList)this)[index] = newJson;
                        newJson.Data = value;
                        oldJson.SetParent(null);
                        hasChanged = true;
                    }
                }
                index++;
            }

            for (int i = this.valueList.Count - 1; i >= index; i--) {
                ((IList)this).RemoveAt(i);
                hasChanged = true;
            }

            if (hasChanged)
                this.Parent.HasChanged(tArr);
        }

        /// <summary>
        /// Compares all items in an array that is bound with an enumeration of values.
        /// Will remove, add and replaces items that have changed.
        /// </summary>
        /// <param name="boundValue"></param>
        internal void CheckBoundArray(IEnumerable boundValue) {
            Json oldJson;
            Json newJson;
            int index = 0;
            int itemIndex;
            TObjArr tArr = Template as TObjArr;
            bool hasChanged = false;
            IList jsonList = (IList)this;
            int offset = (this.arrayAddsAndDeletes != null) ? this.arrayAddsAndDeletes.Count : 0;

            if (boundValue != null) {
                foreach (object value in boundValue) {
                    if (jsonList.Count <= index) {
                        newJson = (Json)tArr.ElementType.CreateInstance();
                        newJson.data = value;
                        jsonList.Add(newJson);
                        newJson.Data = value;
                        hasChanged = true;
                    } else {
                        oldJson = (Json)jsonList[index];
                        if (!CompareDataObjects(oldJson.Data, value)) {
                            itemIndex = IndexOf(jsonList, index + 1, value);
                            if (itemIndex == -1) {
                                newJson = (Json)tArr.ElementType.CreateInstance();
                                newJson.data = value;
                                jsonList.Insert(index, newJson);
                                newJson.Data = value;
                            } else {
                                this.Move(itemIndex, index);
                            }
                            hasChanged = true;
                        }
                    }
                    index++;
                }
            }

            int deleteCount = 0;
            
            for (int i = this.valueList.Count - 1; i >= index; i--) {
                jsonList.RemoveAt(i);
                hasChanged = true;
                deleteCount++;
            }

            ReduceArrayChanges(this.arrayAddsAndDeletes, offset, deleteCount);
            // TODO:
            // Temorary workaround until jsonpatch generation supports move operation.
            SplitMoves(this.arrayAddsAndDeletes);

            if (hasChanged)
                this.Parent.HasChanged(tArr);
        }

        /// <summary>
        /// Compares the two objects. If both implements the IBindable interface, the
        /// identity will be used, otherwise a reference equality will be performed.
        /// </summary>
        /// <param name="obj1"></param>
        /// <param name="obj2"></param>
        /// <returns></returns>
        private static bool CompareDataObjects(object obj1, object obj2) {
            if (obj1 == null && obj2 == null)
                return true;

            if (obj1 == null && obj2 != null)
                return false;

            if (obj1 != null && obj2 == null)
                return false;

            var bind1 = obj1 as IBindable;
            var bind2 = obj2 as IBindable;

            if (bind1 == null || bind2 == null)
                return obj1.Equals(obj2);

            return (bind1.Identity == bind2.Identity);
        }

        /// <summary>
        /// Try to reduce a list of modifications for an array. Some assumptions are made to make the implementation easier:
        /// 1) All deletes are in the end of the list, where deleteCount is the number of deletes.
        /// 2) We always move deletes to the beginning and transforming modification as we move the delete.
        /// 
        /// Modifications can be removed if:
        /// a MOVE is moving FROM and TO the same index.
        /// an INSERT has the same index as the DELETE were moving. Both will be canceled out.
        /// a REPLACE has the same index as the DELETE were moving. The REPLACE will be skipped.
        /// </summary>
        /// <param name="arrayChanges"></param>
        /// <param name="offset"></param>
        /// <param name="deleteCount"></param>
        private static void ReduceArrayChanges(List<Change> arrayChanges, int offset, int deleteCount) {
            Change delete;
            Change candidate;
            int lastIndex;

            if (deleteCount < 1 || arrayChanges == null || (arrayChanges.Count - offset) < 2)
                return;

            for (int i = 0; i < deleteCount; i++) {
                lastIndex = arrayChanges.Count - 1;
                delete = arrayChanges[lastIndex];

                for (int k = lastIndex - 1; k >= offset; k--) {
                    candidate = arrayChanges[k];

                    if (candidate.ChangeType == Change.MOVE) {
                        if (candidate.Index == delete.Index) {
                            delete.Index = candidate.FromIndex;
                            arrayChanges[lastIndex] = delete;
                            arrayChanges.RemoveAt(k);
                            lastIndex = arrayChanges.Count - 1;
                        } else {
                            // Checking FROM index (i.e the delete)
                            if (candidate.FromIndex == delete.Index) {
                                delete.Index--;
                                arrayChanges[lastIndex] = delete;
                                candidate.FromIndex--;
                            } else if (candidate.FromIndex > delete.Index) {
                                candidate.FromIndex--;
                            } else { // candidate.FromIndex < delete.Index
                                delete.Index++;
                            }

                            // Checking TO index (i.e. the insert)
                            if (candidate.Index > delete.Index)
                                candidate.Index--;
                            else if (candidate.Index < delete.Index) {
                                delete.Index--;
                                arrayChanges[lastIndex] = delete;
                            }

                            // If after moving the delete the indexes in the move are
                            // equal we can just remove it since it is unnecessary
                            if (candidate.Index == candidate.FromIndex) {
                                arrayChanges.RemoveAt(k);
                                lastIndex = arrayChanges.Count - 1;
                            } else {
                                arrayChanges[k] = candidate;
                            }
                        }
                    } else if (candidate.ChangeType == Change.ADD) {
                        if (candidate.Index == delete.Index) {
                            // Since we first insert and then delete the inserted index
                            // we can just remove both operations.
                            arrayChanges.RemoveAt(lastIndex);
                            arrayChanges.RemoveAt(k);
                            lastIndex = -1;
                            break;
                        } else if (candidate.Index > delete.Index) {
                            candidate.Index--;
                            arrayChanges[k] = candidate;
                        } else { // candidate.Index < delete.Index
                            delete.Index--;
                            arrayChanges[lastIndex] = delete;
                        }
                    } else if (candidate.ChangeType == Change.REMOVE) {
                        if (candidate.Index == delete.Index) {
                            // Do nothing
                        } else if (candidate.Index > delete.Index) {
                            candidate.Index--;
                            arrayChanges[k] = candidate;
                        } else { // candidate.Index < delete.Index
                            delete.Index++;
                            arrayChanges[lastIndex] = delete;
                        }
                    } else if (candidate.ChangeType == Change.REPLACE) {
                        if (candidate.Index == delete.Index) {
                            // We first overwrite the value at index and then remove it
                            // so in this case we can just skip the replace operation.
                            arrayChanges.RemoveAt(k);
                            lastIndex = arrayChanges.Count - 1;
                        } else if (candidate.Index > delete.Index) {
                            candidate.Index--;
                            arrayChanges[k] = candidate;
                        } else { // candidate.Index < delete.Index
                            // Do nothing
                        }
                    }
                }

                if (arrayChanges.Count > 1 && lastIndex != -1) {
                    arrayChanges.RemoveAt(lastIndex);
                    arrayChanges.Insert(offset, delete);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="changes"></param>
        private static void SplitMoves(List<Change> changes) {
            Change current;
            Change toSplit;

            if (changes == null)
                return;

            for (int i = 0; i < changes.Count; i++) {
                current = changes[i];
                if (current.ChangeType == Change.MOVE) {
                    toSplit = Change.Remove(current.Parent, (TObjArr)current.Property, current.FromIndex, current.Item);
                    changes[i] = toSplit;
                    toSplit = Change.Add(current.Parent, (TObjArr)current.Property, current.Index, current.Item);
                    changes.Insert(i + 1, toSplit);
                    i++;

                    if (current.Item != null)
                        current.Item.SetBoundValuesInTuple();
                }
            }
        }

        /// <summary>
        /// Checks if this object is accessible in an earlier version of the viewmodel.
        /// </summary>
        /// <param name="serverVersion">The version of the viewmodel to check</param>
        /// <returns>true if this object existed in the specified version, false otherwise.</returns>
        /// <remarks>
        /// This method is used when versioning is enabled and this json belongs to a viewmodeltree.
        /// </remarks>
        internal bool IsValidForVersion(long serverVersion) {
            return (serverVersion >= addedInVersion);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="version"></param>
        /// <param name="toVersion"></param>
        /// <param name="callStepSiblings"></param>
        internal void CleanupOldVersionLogs(ViewModelVersion version, long toVersion, bool callStepSiblings = true) {
            if (versionLog != null) {
                for (int i = 0; i < versionLog.Count; i++) {
                    if (versionLog[i].Version <= version.RemoteLocalVersion) {
                        versionLog.RemoveAt(i);
                        i--;
                    }
                }
            }

            if (IsArray) {
                foreach (Json child in this.valueList) {
                    child.CleanupOldVersionLogs(version, toVersion);
                }
            } else {
                if (IsObject) {
                    var tobj = (TObject)Template;
                    foreach (Template t in tobj.Properties) {
                        var tcontainer = t as TContainer;
                        if (tcontainer != null) {
                            var json = (Json)tcontainer.GetUnboundValueAsObject(this);
                            if (json != null)
                                json.CleanupOldVersionLogs(version, toVersion);
                        }
                    }
                }
            }

            if (callStepSiblings && this.siblings != null) {
                foreach (var stepSibling in this.siblings) {
                    if (stepSibling == this)
                        continue;
                    stepSibling.CleanupOldVersionLogs(version, toVersion, false);
                }
            }
        }

        /// <summary>
        /// Initializes needed state to be able to keep track of changes and optionally
        /// keep track of versions.
        /// </summary>
        /// <remarks>
        /// If this method is called on an already stateful instance, it will silently return.
        /// All children of this json will be called as well, so it's enough to just call 
        /// this method for the instance in question.
        /// </remarks>
        private void UpgradeToStateful(bool callStepSiblings) {
            if (callStepSiblings == true && this.siblings != null) {
                foreach (var stepSibling in this.siblings) {
                    if (stepSibling == this)
                        continue;
                    stepSibling.UpgradeToStateful(false);
                }
            }

            if (this.isStateful == true)
                return;

            var changeLog = ChangeLog;
            if (changeLog != null && changeLog.Version != null)
                this.addedInVersion = changeLog.Version.LocalVersion;

            this.isStateful = true;

            // If the transaction attached to this json is the same transaction set higher 
            // up in the tree we set it back to invalid. This will be useful later when
            // json will be stored in blobs.
            
            if (this.parent != null && this.transaction == this.parent.GetTransactionHandle(true))
                this.transaction = TransactionHandle.Invalid;
            
            if (this.transaction != TransactionHandle.Invalid) {
                // We have a transaction attached on this json. We register the transaction 
                // on the session to keep track of it. This will also mean that the session
                // is responsible for releasing it when noone uses it anymore.
                this.transaction = Session.RegisterTransaction(transaction);
            }
            
            this.trackChanges = true;

            if (this.IsArray) {
                stateFlags = new List<PropertyState>(this.valueList.Count);
                foreach (Json item in this.valueList) {
                    stateFlags.Add(PropertyState.Default);
                    item.UpgradeToStateful(true);
                }
            } else {
                if (Template != null) {
                    if (IsObject) {
                        var tobj = (TObject)Template;
                        stateFlags = new List<PropertyState>(tobj.Properties.Count);
                        foreach (Template tChild in tobj.Properties) {
                            stateFlags.Add(PropertyState.Default);
                            var container = tChild as TContainer;
                            if (container != null) {
                                var childJson = (Json)container.GetUnboundValueAsObject(this);
                                if (childJson != null)
                                    childJson.UpgradeToStateful(true);
                            }
                        }
                    } else {
                        stateFlags = new List<PropertyState>(1);
                        stateFlags.Add(PropertyState.Default);
                    }
                }
            }
        }

        /// <summary>
        /// Downgrades the instance from a stateful viewmodel to a non-stateful meaning that 
        /// changes will not be tracked and versioninformation is discarded.
        /// </summary>
        /// <remarks>
        /// If this method is called on a non-stateful instance, it will silently return.
        /// All children of this json will be called as well, so it's enough to just call 
        /// this method for the instance in question.
        /// </remarks>
        private void DowngradeFromStateFul(bool callStepSiblings) {
            if (isStateful == false)
                return;

            isStateful = false;
            addedInVersion = -1;
            if (this.transaction != TransactionHandle.Invalid) {
                Session.DeregisterTransaction(this.transaction);
                this.transaction = TransactionHandle.Invalid;
            }

            this.trackChanges = false;

            if (this.IsArray) {
                foreach (Json item in this.valueList) {
                    item.DowngradeFromStateFul(true);
                }
            } else {
                if (Template != null) {
                    if (IsObject) {
                        foreach (Template tChild in ((TObject)Template).Properties) {
                            var container = tChild as TContainer;
                            if (container != null) {
                                var childJson = (Json)container.GetUnboundValueAsObject(this);
                                if (childJson != null)
                                    childJson.DowngradeFromStateFul(true);
                            }
                        }
                    }
                }
            }

            if (callStepSiblings == true && this.siblings != null) {
                foreach (var stepSibling in this.siblings) {
                    if (stepSibling == this)
                        continue;

                    // Check for stepsiblings that might be a part of a stateful viewmodel,
                    // and still be a sibling to another. In that case we don't do the call.
                    if (stepSibling.session != null || (stepSibling.Parent != null && stepSibling.Parent.isStateful))
                        continue;
                    stepSibling.DowngradeFromStateFul(false);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        internal SiblingList Siblings {
            get { return this.siblings; }
            set {
                this.siblings = value;
                if (this.Session != null) {
                    // We just call OnAdd for this sibling since the list will be set on each one.
                    // If the sibling is already added the method will just return so no need to 
                    // do additional checks here.
                    this.UpgradeToStateful(true);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool AutoRefreshBoundProperties {
            get { return this.checkBoundProperties; }
            set {
                this.checkBoundProperties = value;
                if (this.Siblings != null) {
                    foreach (var sibling in this.Siblings) {
                        if (sibling == this)
                            continue;
                        sibling.checkBoundProperties = value;
                    }
                }
            } 
        }
        
        /// <summary>
        /// If true, this object has been flushed from the change log (usually an
        /// indication that the object has been sent to its client.
        /// </summary>
        internal bool HasBeenSent {
            get {
                if (!this.trackChanges)
                    return false;

                if (this.siblings != null) {
                    return this.siblings.HasBeenSent(this.siblings.IndexOf(this));
                }

                if (Parent != null) {
                    return ((IndexInParent != -1) && (!Parent.IsDirty(IndexInParent)));
                } else {
                    var log = ChangeLog;
                    if (log == null) {
                        return false;
                    }
                    return !log.BrandNew;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="verifySiblings"></param>
        internal void VerifyDirtyFlags(bool verifySiblings = true) {
            if (!this.trackChanges)
                return;

            switch (this.Template.TemplateTypeId) {
                case TemplateTypeEnum.Object:
                    VerifyDirtyFlagsForObject();
                    break;
                case TemplateTypeEnum.Array:
                    VerifyDirtyFlagsForArray();
                    break;
                default: // Single value
                    VerifyDirtyFlagsForSingleValue();
                    break;
            }

            if (verifySiblings && this.siblings != null) {
                foreach (var sibling in this.siblings) {
                    if (this.Equals(sibling))
                        continue;
                    sibling.VerifyDirtyFlags(false);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void VerifyDirtyFlagsForSingleValue() {
            AssertOrThrow((this.stateFlags.Count == 1), this.Template);
            AssertOrThrow((this.stateFlags[0] == PropertyState.Default), this.Template);
            AssertOrThrow((this.dirty == false), this.Template);
        }

        /// <summary>
        /// 
        /// </summary>
        private void VerifyDirtyFlagsForArray() {
            Json row;
            var tArr = (TObjArr)this.Template;

            AssertOrThrow((this.dirty == false), tArr);
            AssertOrThrow((this.stateFlags.Count == this.valueList.Count), tArr);
            for (int i = 0; i < this.stateFlags.Count; i++) {
                AssertOrThrow((this.stateFlags[i] == PropertyState.Default), tArr);

                row = (Json)this.valueList[i];
                if (row != null)
                    row.VerifyDirtyFlags();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void VerifyDirtyFlagsForObject() {
            Json child;
            TContainer tCon;
            var tObj = (TObject)this.Template;

            AssertOrThrow((this.stateFlags.Count == tObj.Properties.Count), tObj);
            for (int i = 0; i < this.stateFlags.Count; i++) {
                AssertOrThrow((this.stateFlags[i] == PropertyState.Default), tObj.Properties[i]);

                tCon = tObj.Properties[i] as TContainer;
                if (tCon != null) {
                    child = (Json)tCon.GetUnboundValueAsObject(this);
                    if (child != null)
                        child.VerifyDirtyFlags();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="template"></param>
        private void AssertOrThrow(bool expression, Template template) {
            if (!expression) {
                //                Json.logSource.LogWarning("Verification of dirtyflags failed for " + GetTemplateName(template) + "\n" + (new StackTrace(true)).ToString());
                throw new System.Exception("Verification of dirtyflags failed for " + JsonDebugHelper.GetFullName(this, template));
            }
        }
    }
}
