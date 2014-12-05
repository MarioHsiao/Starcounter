using System;
using System.Collections;
using System.Collections.Generic;
using Starcounter.Advanced;
using Starcounter.Internal.XSON;
using Starcounter.Templates;
using Starcounter.Advanced.XSON;

namespace Starcounter {
	partial class Json {
		/// <summary>
		/// 
		/// </summary>
		internal void Dirtyfy() {
            if (!_dirtyCheckEnabled)
                return;

			_Dirty = true;
			if (Parent != null)
				Parent.Dirtyfy();

            if (_stepParent != null)
                _stepParent.Dirtyfy();
		}

		/// <summary>
		/// 
		/// </summary>
		internal void CheckpointChangeLog() {
            if (!_dirtyCheckEnabled)
                return;

			if (this.IsArray) {
				this.ArrayAddsAndDeletes = null;
				if (Template != null) {
					var tjson = (TObjArr)Template;
					tjson.Checkpoint(this.Parent);
				}
			} else {
				if (Template != null) {
                    this.AddInScope<TObject>( 
                        (tjson) => {
                            for (int i = 0; i < tjson.Properties.ExposedProperties.Count; i++) {
                                var property = tjson.Properties.ExposedProperties[i] as TValue;
                                if (property != null) {
                                    property.Checkpoint(this);
                                }
                            }
                        },
                        (TObject)Template);

                    if (this._stepSiblings != null) {
                        foreach (Json stepSibling in _stepSiblings) {
                            stepSibling.CheckpointChangeLog();
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
            if (_dirtyCheckEnabled)
                return (WasReplacedAt(prop.TemplateIndex));
            return false;
		}

		/// <summary>
		/// Logs all property changes made to this object or its bound data object
		/// </summary>
		/// <param name="session">The session (for faster access)</param>
		internal void LogValueChangesWithDatabase(Session session) {
            if (!_dirtyCheckEnabled)
                return;

			if (this.IsArray) {
				LogArrayChangesWithDatabase(session);
			} else {
				LogObjectValueChangesWithDatabase(session);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="session"></param>
		private void LogArrayChangesWithDatabase(Session session) {
            bool logChanges;
            Json item;

            if (ArrayAddsAndDeletes != null && ArrayAddsAndDeletes.Count > 0) {
                for (int i = 0; i < ArrayAddsAndDeletes.Count; i++) {
                    var change = ArrayAddsAndDeletes[i];
                    session.AddChange(change);

                    var index = change.Index;
                    if (index < list.Count) {
                        CheckpointAt(index);

                        item = (Json)list[index];
                        item.SetBoundValuesInTuple();
                        item._Dirty = false;
                    }
                }
                
                for (int i = 0; i < _list.Count; i++) {
                    // Skip all items we have already added to the changelog.
                    logChanges = true;
                    foreach (Change change in ArrayAddsAndDeletes) {
                        if (change.Index == i) {
                            logChanges = false;
                            break;
                        }
                    }

                    if (logChanges) {
                        ((Json)_list[i]).LogValueChangesWithDatabase(session);
                    }
                }

                if (session.CheckOption(SessionOptions.EnableProtocolVersioning)) {
                    if (versionLog == null)
                        versionLog = new List<List<Change>>();
                    versionLog.Add(ArrayAddsAndDeletes);
                }
                ArrayAddsAndDeletes = null;
            } else {
                for (int t = 0; t < _list.Count; t++) {
                    ((Json)_list[t]).LogValueChangesWithDatabase(session);
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
		internal void LogValueChangesWithoutDatabase(Session s) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Dirty checks each value of the object and reports any changes
		/// to the session changelog.
		/// </summary>
		/// <param name="session">The session to report to</param>
		private void LogObjectValueChangesWithDatabase(Session session) {
            this.AddInScope<Session>((s) => {
                var template = (TObject)Template;
                if (template == null)
                    return;

                var exposed = template.Properties.ExposedProperties;

                if (_Dirty) {
                    for (int t = 0; t < exposed.Count; t++) {
                        if (WasReplacedAt(exposed[t].TemplateIndex)) {
                            if (s != null) {
                                if (IsArray) {
                                    throw new NotImplementedException();
                                } else {
                                    var childTemplate = (TValue)exposed[t];
                                    s.UpdateValue(this, childTemplate);

                                    // TODO:
                                    // Added this code to make current implementation work.
                                    // Probably not the correct place to do it though, both
                                    // for readability and speed.
                                    Json childJson = null;
                                    if (childTemplate is TObjArr)
                                        childJson = this.Get((TObjArr)childTemplate);
                                    else if (childTemplate is TObject)
                                        childJson = this.Get((TObject)childTemplate);

                                    if (childJson != null) {
                                        childJson.SetBoundValuesInTuple();
                                        childJson.CheckpointChangeLog();
                                    }
                                }
                            }
                            CheckpointAt(exposed[t].TemplateIndex);
                        } else {
                            var p = exposed[t];
                            if (p is TContainer) {
                                var c = ((TContainer)p).GetValue(this);
                                if (c != null)
                                    c.LogValueChangesWithDatabase(s);
                            } else {
                                if (IsArray)
                                    throw new NotImplementedException();
                                else
                                    ((TValue)p).CheckAndSetBoundValue(this, true);
                            }
                        }
                    }
                    _Dirty = false;
                } else if (template.HasAtLeastOneBoundProperty) {
                    for (int t = 0; t < exposed.Count; t++) {
                        if (exposed[t] is TContainer) {
                            var c = ((TContainer)exposed[t]).GetValue(this);
                            if (c != null)
                                c.LogValueChangesWithDatabase(s);
                        } else {
                            if (IsArray) {
                                throw new NotImplementedException();
                            } else {
                                var p = exposed[t] as TValue;
                                p.CheckAndSetBoundValue(this, true);
                            }
                        }
                    }
                } else {
                    foreach (var e in list) {
                        if (e is Json) {
                            ((Json)e).LogValueChangesWithDatabase(s);
                        }
                    }
                }

                if (_stepSiblings != null) {
                    foreach (var stepSibling in _stepSiblings) {
                        stepSibling.LogValueChangesWithDatabase(session);
                    }
                }
            },
            session);
		}

		internal void SetBoundValuesInTuple() {
			if (IsArray) {
				foreach (Json item in _list) {
					item.SetBoundValuesInTuple();
				}
			} else {
                this.AddInScope(() => {
                    TObject tobj = (TObject)Template;
                    if (tobj != null) {
                        for (int i = 0; i < tobj.Properties.Count; i++) {
                            var t = tobj.Properties[i];

                            if (t is TContainer) {
                                var childJson = ((TContainer)t).GetValue(this);
                                if (childJson != null)
                                    childJson.SetBoundValuesInTuple();
                            } else {
                                var vt = t as TValue;
                                if (vt != null)
                                    vt.CheckAndSetBoundValue(this, false);
                            }
                        }
                    }

                    if (_stepSiblings != null) {
                        foreach (var stepSibling in _stepSiblings) {
                            stepSibling.SetBoundValuesInTuple();
                        }
                    }
                });
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
                    ((IList)this).Add(newJson);
                    newJson.Data = value;
                    hasChanged = true;
                } else {
                    oldJson = (Json)_list[index];
                    if (!CompareDataObjects(oldJson.Data, value)) {
                        newJson = (Json)tArr.ElementType.CreateInstance();
                        ((IList)this)[index] = newJson;
                        newJson.Data = value;
                        oldJson.SetParent(null);
                        if (_dirtyCheckEnabled) {
                            if (ArrayAddsAndDeletes == null)
                                ArrayAddsAndDeletes = new List<Change>();
                            ArrayAddsAndDeletes.Add(Change.Update((Json)this.Parent, tArr, index));
                        }
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
	}
}
