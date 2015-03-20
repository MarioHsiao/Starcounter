using System;
using System.Collections;
using System.Collections.Generic;
using Starcounter.Advanced;
using Starcounter.Advanced.XSON;
using Starcounter.Internal;
using Starcounter.Internal.XSON;
using Starcounter.Templates;

namespace Starcounter {
    internal struct ArrayVersionLog {
        internal long Version;
        internal List<Change> Changes;

        internal ArrayVersionLog(long version, List<Change> changes) {
            Version = version;
            Changes = changes;
        }
    }

	partial class Json {
		/// <summary>
		/// 
		/// </summary>
		internal void Dirtyfy(bool callStepSiblings = true) {
            if (!_trackChanges)
                return;
            
			_Dirty = true;
			if (Parent != null)
				Parent.Dirtyfy();

            if (callStepSiblings == true && _stepSiblings != null) {
                foreach (Json stepSibling in _stepSiblings) {
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
            if (!_trackChanges)
                return;

			if (this.IsArray) {
				this.ArrayAddsAndDeletes = null;
				if (Template != null) {
					var tjson = (TObjArr)Template;
					tjson.Checkpoint(this.Parent);
				}
			} else {
				if (Template != null) {
                    this.Scope<TObject>( 
                        (tjson) => {
                            for (int i = 0; i < tjson.Properties.ExposedProperties.Count; i++) {
                                var property = tjson.Properties.ExposedProperties[i] as TValue;
                                if (property != null) {
                                    property.Checkpoint(this);
                                }
                            }
                        },
                        (TObject)Template);

                    if (callStepSiblings == true && this._stepSiblings != null) {
                        foreach (Json stepSibling in _stepSiblings) {
                            if (stepSibling == this)
                                continue;
                            stepSibling.CheckpointChangeLog(false);
                        }
                    }
				}
			}
			_Dirty = false;
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
            if (_trackChanges)
                return (WasReplacedAt(prop.TemplateIndex));
            return false;
		}

		/// <summary>
		/// Logs all property changes made to this object or its bound data object
		/// </summary>
		/// <param name="session">The session (for faster access)</param>
		internal void LogValueChangesWithDatabase(Session session, bool callStepSiblings) {
            if (!_trackChanges)
                return;

			if (this.IsArray) {
				LogArrayChangesWithDatabase(session, callStepSiblings);
			} else {
				LogObjectValueChangesWithDatabase(session, callStepSiblings);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="session"></param>
		private void LogArrayChangesWithDatabase(Session session, bool callStepSiblings = true) {
            bool logChanges;
            Json item;

            if (ArrayAddsAndDeletes != null && ArrayAddsAndDeletes.Count > 0) {
                for (int i = 0; i < ArrayAddsAndDeletes.Count; i++) {
                    var change = ArrayAddsAndDeletes[i];

                    session.AddChange(change);
                    var index = change.Item._cacheIndexInArr;
                    if (change.ChangeType != Change.REMOVE && index >= 0 && index < list.Count) {
                        CheckpointAt(index);
                        item = change.Item;
                        item.SetBoundValuesInTuple();
                        item._Dirty = false;
                    }
                }
                
                for (int i = 0; i < _list.Count; i++) {
                    // Skip all items we have already added to the changelog.
                    logChanges = true;
                    foreach (Change change in ArrayAddsAndDeletes) {
                        if (change.ChangeType != Change.REMOVE && change.Index == i) {
                            logChanges = false;
                            break;
                        }
                    }

                    if (logChanges) {
                        ((Json)_list[i]).LogValueChangesWithDatabase(session, callStepSiblings);
                     }
                }

                if (session.CheckOption(SessionOptions.PatchVersioning)) {
                    if (versionLog == null)
                        versionLog = new List<ArrayVersionLog>();
                    versionLog.Add(new ArrayVersionLog(session.ServerVersion, ArrayAddsAndDeletes));
                }
                ArrayAddsAndDeletes = null;
            } else {
                for (int t = 0; t < _list.Count; t++) {
                    ((Json)_list[t]).LogValueChangesWithDatabase(session, callStepSiblings);
                }
            }
		}

		/// <summary>
		/// Used to generate change logs for all pending property changes in this object and
		/// and its children and grandchidren (recursivly) excluding changes to bound data
		/// objects. This method is much faster than the corresponding method checking
		/// th database.
		/// </summary>
		/// <param name="session">The session (for faster access)</param>
		internal void LogValueChangesWithoutDatabase(Session s, bool callStepSiblings = true) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Dirty checks each value of the object and reports any changes
		/// to the session changelog.
		/// </summary>
		/// <param name="session">The session to report to</param>
		private void LogObjectValueChangesWithDatabase(Session session, bool callStepSiblings = true) {
            this.Scope<Session, Json, bool>((s, json, css) => {
                var template = (TObject)json.Template;
                if (template == null)
                    return;

                var exposed = template.Properties.ExposedProperties;

                if (json._Dirty) {
                    for (int t = 0; t < exposed.Count; t++) {
                        if (json.WasReplacedAt(exposed[t].TemplateIndex)) {
                            if (s != null) {
                                if (json.IsArray) {
                                    throw new NotImplementedException();
                                } else {
                                    var childTemplate = (TValue)exposed[t];
                                    s.UpdateValue(json, childTemplate);

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
                                    c.LogValueChangesWithDatabase(s, true);
                            } else {
                                if (json.IsArray)
                                    throw new NotImplementedException();
                                else
                                    ((TValue)p).CheckAndSetBoundValue(json, true);
                            }
                        }
                    }
                    json._Dirty = false;
                } else if (_checkBoundProperties && template.HasAtLeastOneBoundProperty) {
                    for (int t = 0; t < exposed.Count; t++) {
                        if (exposed[t] is TContainer) {
                            var c = ((TContainer)exposed[t]).GetValue(json);
                            if (c != null)
                                c.LogValueChangesWithDatabase(s, true);
                        } else {
                            if (json.IsArray) {
                                throw new NotImplementedException();
                            } else {
                                var p = exposed[t] as TValue;
                                p.CheckAndSetBoundValue(json, true);
                            }
                        }
                    }
                } else {
                    foreach (var e in json.list) {
                        if (e is Json) {
                            ((Json)e).LogValueChangesWithDatabase(s, true);
                        }
                    }
                }

                if (css == true && json._stepSiblings != null) {
                    foreach (var stepSibling in json._stepSiblings) {
                        if (stepSibling == json)
                            continue;
                        stepSibling.LogValueChangesWithDatabase(s, false);
                    }
                }
            },
            session, 
            this,
            callStepSiblings);
		}

		internal void SetBoundValuesInTuple(bool callStepSiblings = true) {
            if (!_checkBoundProperties)
                return;

			if (IsArray) {
				foreach (Json item in _list) {
					item.SetBoundValuesInTuple();
				}
			} else {
                this.Scope<Json>((json) => {
                    TObject tobj = (TObject)json.Template;
                    if (tobj != null) {
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
                    }

                    if (callStepSiblings == true && json._stepSiblings != null) {
                        foreach (var stepSibling in json._stepSiblings) {
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

        internal void CheckBoundArray(IEnumerable boundValue) {
            Json oldJson;
            Json newJson;
            int index = 0;
            TObjArr tArr = Template as TObjArr;
            bool hasChanged = false;

            foreach (object value in boundValue) {
                if (_list.Count <= index) {
                    newJson = (Json)tArr.ElementType.CreateInstance();
                    newJson.Data = value;
                    ((IList)this).Add(newJson);
                    hasChanged = true;
                } else {
                    oldJson = (Json)_list[index];
                    if (!CompareDataObjects(oldJson.Data, value)) {
                        newJson = (Json)tArr.ElementType.CreateInstance();
                        newJson.Data = value;
                        ((IList)this)[index] = newJson;
                        oldJson.SetParent(null);
                        hasChanged = true;
                    }
                }
                index++;
            }

            for (int i = _list.Count - 1; i >= index; i--) {
                ((IList)this).RemoveAt(i);
                hasChanged = true;
            }

            if (hasChanged)
                this.Parent.HasChanged(tArr);
        }

        private bool CompareDataObjects(object obj1, object obj2) {
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
        /// Checks if this object is accessible in an earlier version of the viewmodel.
        /// </summary>
        /// <param name="serverVersion">The version of the viewmodel to check</param>
        /// <returns>true if this object existed in the specified version, false otherwise.</returns>
        /// <remarks>
        /// This method is used when versioning is enabled in Session and this json belongs to a viewmodeltree.
        /// </remarks>
        internal bool IsValidForVersion(long serverVersion) {
            return (serverVersion >= addedInVersion);
        }

        internal void CleanupOldVersionLogs(long toVersion, bool callStepSiblings = true) {
            if (versionLog != null) {
                Session session = Session;
                for (int i = 0; i < versionLog.Count; i++) {
                    if (versionLog[i].Version <= session.ClientServerVersion) {
                        versionLog.RemoveAt(i);
                        i--;
                    }
                }
            }

            if (IsArray) {
                foreach (Json child in _list) {
                    child.CleanupOldVersionLogs(toVersion);
                }
            } else {
                var tobj = (TObject)Template;
                foreach (Template t in tobj.Properties) {
                    var tcontainer = t as TContainer;
                    if (tcontainer != null) {
                        var json = (Json)tcontainer.GetUnboundValueAsObject(this);
                        if (json != null)
                            json.CleanupOldVersionLogs(toVersion);
                    }
                }
            }

            if (callStepSiblings && _stepSiblings != null) {
                foreach (Json stepSibling in _stepSiblings) {
                    if (stepSibling == this)
                        continue;
                    CleanupOldVersionLogs(toVersion, false);
                }
            }
        }

        /// <summary>
        /// Called when this object is added to a stateful viewmodel. 
        /// This method will be called on each childjson as well.
        /// </summary>
        private void OnAddedToViewmodel(bool callStepSiblings) {
            if (this.isAddedToViewmodel == true)
                return;

            this.addedInVersion = this.Session.ServerVersion;
            this.isAddedToViewmodel = true;

            // If the transaction attached to this json is the same transaction set higher 
            // up in the tree we set it back to invalid. This will be useful later when
            // json will be stored in blobs.
            
            if (_parent != null && this._transaction == _parent.GetTransactionHandle(true))
                this._transaction = TransactionHandle.Invalid;
            
            if (this._transaction != TransactionHandle.Invalid) {
                // We have a transaction attached on this json. We register the transaction 
                // on the session to keep track of it. This will also mean that the session
                // is responsible for releasing it when noone uses it anymore.
                Session.RegisterTransaction(_transaction);
            }

            if (callStepSiblings == true && this._stepSiblings != null) {
                foreach (var stepSibling in this._stepSiblings) {
                    if (stepSibling == this)
                        continue;
                    stepSibling.OnAddedToViewmodel(false);
                }
            }
            
            _trackChanges = true;

            if (this.IsArray) {
                _SetFlag = new List<bool>(_list.Count);
                foreach (Json item in _list) {
                    _SetFlag.Add(false);
                    item.OnAddedToViewmodel(true);
                }
            } else {
                if (Template != null) {
                    var tobj = (TObject)Template;
                    _SetFlag = new List<bool>(tobj.Properties.Count);
                    foreach (Template tChild in tobj.Properties) {
                        _SetFlag.Add(false);
                        var container = tChild as TContainer;
                        if (container != null) {
                            var childJson = (Json)container.GetUnboundValueAsObject(this);
                            if (childJson != null)
                                childJson.OnAddedToViewmodel(true);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Called when this object have been detached from a stateful viewmodel. Will call all
        /// children as well.
        /// </summary>
        private void OnRemovedFromViewmodel(bool callStepSiblings) {
            isAddedToViewmodel = false;
            addedInVersion = -1;
            if (_transaction != TransactionHandle.Invalid) {
                Session.DeregisterTransaction(_transaction);
            }

            _trackChanges = false;

            if (this.IsArray) {
                foreach (Json item in _list) {
                    item.OnRemovedFromViewmodel(true);
                }
            } else {
                if (Template != null) {
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

            if (callStepSiblings == true && this._stepSiblings != null) {
                foreach (var stepSibling in _stepSiblings) {
                    if (stepSibling == this)
                        continue;

                    // Check for stepsiblings that might be a part of a stateful viewmodel,
                    // and still be a sibling to another. In that case we don't do the call.
                    if (stepSibling._Session != null || (stepSibling.Parent != null && stepSibling.Parent.isAddedToViewmodel))
                        continue;
                    stepSibling.OnRemovedFromViewmodel(false);
                }
            }
        }

        internal List<Json> StepSiblings {
            get { return _stepSiblings; }
            set {
                _stepSiblings = value;
                if (this.Session != null) {
                    // We just call OnAdd for this sibling since the list will be set on each one.
                    // If the sibling is already added the method will just return so no need to 
                    // do additional checks here.
                    this.OnAddedToViewmodel(false);
                }
            }
        }
	}
}
