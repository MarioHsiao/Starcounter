using System;
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
		/// 
		/// </summary>
		internal void Dirtyfy(bool callStepSiblings = true) {
            if (!this.trackChanges)
                return;
            
			dirty = true;
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
		/// 
		/// </summary>
		internal void CheckpointChangeLog(bool callStepSiblings = true) {
            if (!this.trackChanges)
                return;

			if (this.IsArray) {
				this.arrayAddsAndDeletes = null;
				if (Template != null) {
					var tjson = (TObjArr)Template;
					tjson.Checkpoint(this.Parent);
				}
			} else {
				if (Template != null) {
                    this.Scope<Json, TValue>( 
                        (parent, tjson) => {
                            if (parent.IsObject) {
                                TObject tobj = (TObject)tjson;
                                for (int i = 0; i < tobj.Properties.ExposedProperties.Count; i++) {
                                    var property = tobj.Properties.ExposedProperties[i] as TValue;
                                    if (property != null) {
                                        property.Checkpoint(parent);
                                    }
                                }
                            } else {
                                tjson.Checkpoint(parent);
                            }
                        },
                        this,
                        (TValue)Template);

                    if (callStepSiblings == true && this.siblings != null) {
                        for (int i = 0; i < this.siblings.Count; i++) {
                            var sibling = siblings[i];
                            this.siblings.MarkAsSent(i);

                            if (sibling == this)
                                continue;
                            
                            sibling.CheckpointChangeLog(false);
                            if (sibling.Parent != null) {
                                sibling.Parent.CheckpointAt(sibling.IndexInParent);
                            }
                        }
                    }
				}
			}
			dirty = false;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="prop"></param>
		/// <returns></returns>
		public bool IsDirty(Template prop) {
#if DEBUG
			this.Template.VerifyProperty(prop);
#endif
            if (this.trackChanges)
                return (WasReplacedAt(prop.TemplateIndex));
            return false;
		}

		/// <summary>
		/// Logs all property changes made to this object or its bound data object
		/// </summary>
		/// <param name="changeLog">Log of changes</param>
		internal void LogValueChangesWithDatabase(ChangeLog changeLog, bool callStepSiblings) {
            if (!this.trackChanges)
                return;

			if (this.IsArray) {
				LogArrayChangesWithDatabase(changeLog, callStepSiblings);
			} else {
				LogObjectValueChangesWithDatabase(changeLog, callStepSiblings);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="changeLog"></param>
		private void LogArrayChangesWithDatabase(ChangeLog changeLog, bool callStepSiblings = true) {
            bool logChanges;
            Json item;

            if (this.arrayAddsAndDeletes != null && this.arrayAddsAndDeletes.Count > 0) {
                for (int i = 0; i < this.arrayAddsAndDeletes.Count; i++) {
                    var change = this.arrayAddsAndDeletes[i];

                    changeLog.Add(change);
                    var index = change.Item.cacheIndexInArr;
                    if (change.ChangeType != Change.REMOVE && index >= 0 && index < list.Count) {
                        CheckpointAt(index);
                        item = change.Item;
                        item.SetBoundValuesInTuple();
                        item.dirty = false;
                    }
                }
                
                for (int i = 0; i < this.valueList.Count; i++) {
                    // Skip all items we have already added to the changelog.
                    logChanges = true;
                    foreach (Change change in this.arrayAddsAndDeletes) {
                        if (change.ChangeType != Change.REMOVE && change.Index == i) {
                            logChanges = false;
                            break;
                        }
                    }

                    if (logChanges) {
                        ((Json)this.valueList[i]).LogValueChangesWithDatabase(changeLog, callStepSiblings);
                     }
                }

                if (changeLog.Version != null) {
                    if (versionLog == null)
                        versionLog = new List<ArrayVersionLog>();
                    versionLog.Add(new ArrayVersionLog(changeLog.Version.LocalVersion, this.arrayAddsAndDeletes));
                }
                this.arrayAddsAndDeletes = null;
            } else {
                for (int t = 0; t < this.valueList.Count; t++) {
                    var arrItem = ((Json)this.valueList[t]);
                    if (this.WasReplacedAt(t)) { // A refresh of an existing row (that is not added or removed)
                        changeLog.Add(Change.Update(this.Parent, (TValue)this.Template, t, arrItem));
                        this.CheckpointAt(t);
                    } else {
                        arrItem.LogValueChangesWithDatabase(changeLog, callStepSiblings);
                    }
                }
            }
		}

		/// <summary>
		/// Used to generate change logs for all pending property changes in this object and
		/// and its children and grandchidren (recursivly) excluding changes to bound data
		/// objects. This method is much faster than the corresponding method checking
		/// th database.
		/// </summary>
		/// <param name="changeLog">The log of changes</param>
		internal void LogValueChangesWithoutDatabase(ChangeLog changeLog, bool callStepSiblings = true) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Dirty checks each value of the object and reports any changes
		/// to the changelog.
		/// </summary>
		/// <param name="changeLog">The log of changes</param>
		private void LogObjectValueChangesWithDatabase(ChangeLog changeLog, bool callStepSiblings = true) {
            this.Scope<ChangeLog, Json, bool>((clog, json, css) => {
                var template = (TValue)json.Template;
                if (template == null)
                    return;

                if (json.IsObject) {
                    var exposed = ((TObject)template).Properties.ExposedProperties;
                    if (json.dirty) {
                        for (int t = 0; t < exposed.Count; t++) {
                            if (json.WasReplacedAt(exposed[t].TemplateIndex)) {
                                if (clog != null) {
                                    if (json.IsArray) {
                                        throw new NotImplementedException();
                                    } else {
                                        var childTemplate = (TValue)exposed[t];
                                        clog.UpdateValue(json, childTemplate);

                                        TContainer container = childTemplate as TContainer;
                                        if (container != null) {
                                            var childJson = container.GetValue(json);
                                            if (childJson != null) {
                                                childJson.SetBoundValuesInTuple();
                                                childJson.CheckpointChangeLog();
                                            }
                                        }
                                    }
                                }
                                json.CheckpointAt(exposed[t].TemplateIndex);
                            } else {
                                var p = exposed[t];
                                if (p is TContainer) {
                                    var c = ((TContainer)p).GetValue(json);
                                    if (c != null)
                                        c.LogValueChangesWithDatabase(clog, true);
                                } else {
                                    if (json.IsArray)
                                        throw new NotImplementedException();
                                    else
                                        ((TValue)p).CheckAndSetBoundValue(json, true);
                                }
                            }
                        }
                        json.dirty = false;
                    } else if (this.checkBoundProperties) {
                        for (int t = 0; t < exposed.Count; t++) {
                            if (exposed[t] is TContainer) {
                                var c = ((TContainer)exposed[t]).GetValue(json);
                                if (c != null)
                                    c.LogValueChangesWithDatabase(clog, true);
                            } else {
                                if (json.IsArray) {
                                    throw new NotImplementedException();
                                } else {
                                    var p = exposed[t] as TValue;
                                    p.CheckAndSetBoundValue(json, true);
                                }
                            }

                        }
                    }
                } else {
                    if (json.dirty) {
                        if (json.WasReplacedAt(template.TemplateIndex)) {
                            if (clog != null)
                                clog.UpdateValue(json, null);
                            json.CheckpointAt(template.TemplateIndex);
                        } else {
                            template.CheckAndSetBoundValue(json, true);
                        }
                    }
                }

                if (css == true && json.siblings != null) {
                    for (int i = 0; i < json.siblings.Count; i++) {
                        var sibling = json.siblings[i];

                        if (sibling == json)
                            continue;

                        if (json.siblings.HasBeenSent(i)) {
                            sibling.LogValueChangesWithDatabase(clog, false);
                        } else {
                            clog.Add(Change.Update(sibling, null, true));
                            json.siblings.MarkAsSent(i);
                        }
                    }
                }
            },
            changeLog, 
            this,
            callStepSiblings);
		}

		internal void SetBoundValuesInTuple(bool callStepSiblings = true) {
            if (!this.checkBoundProperties)
                return;

			if (IsArray) {
				foreach (Json item in this.valueList) {
					item.SetBoundValuesInTuple();
				}
			} else {
                this.Scope<Json>((json) => {
                    TValue tval = (TValue)json.Template;
                    if (tval != null) {
                        if (json.IsObject) {
                            var tobj = (TObject)tval;
                            for (int i = 0; i < tobj.Properties.Count; i++) {
                                var t = tobj.Properties[i];

                                if (t is TContainer) {
                                    var childJson = ((TContainer)t).GetValue(json);
                                    if (childJson != null)
                                        childJson.SetBoundValuesInTuple();
                                } else {
                                    var vt = t as TValue;
                                    if (vt != null)
                                        vt.CheckAndSetBoundValue(json, false);
                                }
                            }
                        } else {
                            tval.CheckAndSetBoundValue(json, false);
                        }
                    }

                    if (callStepSiblings == true && json.siblings != null) {
                        foreach (var stepSibling in json.siblings) {
                            if (stepSibling == this)
                                continue;
                            stepSibling.SetBoundValuesInTuple(false);
                        }
                    }            
                }, 
                this);
			}
		}

        internal void CheckBoundObject(object boundValue) {
            if (!CompareDataObjects(boundValue, Data))
                AttachData(boundValue);
        }

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

            if (hasChanged)
                this.Parent.HasChanged(tArr);
        }

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
        /// Called when this object is added to a stateful viewmodel. 
        /// This method will be called on each childjson as well.
        /// </summary>
        private void OnAddedToViewmodel(bool callStepSiblings) {
            if (callStepSiblings == true && this.siblings != null) {
                foreach (var stepSibling in this.siblings) {
                    if (stepSibling == this)
                        continue;
                    stepSibling.OnAddedToViewmodel(false);
                }
            }

            if (this.isAddedToViewmodel == true)
                return;

            var changeLog = ChangeLog;
            if (changeLog != null && changeLog.Version != null)
                this.addedInVersion = changeLog.Version.LocalVersion;

            this.isAddedToViewmodel = true;

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
                    item.OnAddedToViewmodel(true);
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
                                    childJson.OnAddedToViewmodel(true);
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
        /// Called when this object have been detached from a stateful viewmodel. Will call all
        /// children as well.
        /// </summary>
        private void OnRemovedFromViewmodel(bool callStepSiblings) {
            if (isAddedToViewmodel == false)
                return;

            isAddedToViewmodel = false;
            addedInVersion = -1;
            if (this.transaction != TransactionHandle.Invalid) {
                Session.DeregisterTransaction(this.transaction);
            }

            this.trackChanges = false;

            if (this.IsArray) {
                foreach (Json item in this.valueList) {
                    item.OnRemovedFromViewmodel(true);
                }
            } else {
                if (Template != null) {
                    if (IsObject) {
                        foreach (Template tChild in ((TObject)Template).Properties) {
                            var container = tChild as TContainer;
                            if (container != null) {
                                var childJson = (Json)container.GetUnboundValueAsObject(this);
                                if (childJson != null)
                                    childJson.OnRemovedFromViewmodel(true);
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
                    if (stepSibling.session != null || (stepSibling.Parent != null && stepSibling.Parent.isAddedToViewmodel))
                        continue;
                    stepSibling.OnRemovedFromViewmodel(false);
                }
            }
        }

        internal SiblingList Siblings {
            get { return this.siblings; }
            set {
                this.siblings = value;
                if (this.Session != null) {
                    // We just call OnAdd for this sibling since the list will be set on each one.
                    // If the sibling is already added the method will just return so no need to 
                    // do additional checks here.
                    this.OnAddedToViewmodel(true);
                }
            }
        }

        public bool AutoRefreshBoundProperties {
            get { return this.checkBoundProperties; }
            set { this.checkBoundProperties = value; } 
        }
	}
}
