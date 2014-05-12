using System;
using System.Collections;
using System.Collections.Generic;
using Starcounter.Advanced;
using Starcounter.Internal.XSON;
using Starcounter.Templates;

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
					var tjson = (TObject)Template;

                    this.ExecuteInScope(() => {
                        for (int i = 0; i < tjson.Properties.ExposedProperties.Count; i++) {
                            var property = tjson.Properties.ExposedProperties[i] as TValue;
                            if (property != null) {
                                property.Checkpoint(this);
                            }
                        }
                    });
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
			if (ArrayAddsAndDeletes != null) {
                session.AddRangeOfChanges(ArrayAddsAndDeletes);
				ArrayAddsAndDeletes.Clear();

				for (int i = 0; i < list.Count; i++) {
					CheckpointAt(i);
				}
			}
			
			var property = Template as TObjArr;
			for (int t = 0; t < _list.Count; t++) {
				if (WasReplacedAt(t)) {
                    session.UpdateValue(this.Parent, property, t);
				}
				(_list[t] as Json).LogValueChangesWithDatabase(session);
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
			var template = (TObject)Template;
			var exposed = template.Properties.ExposedProperties;

            this.ExecuteInScope(() => {
                if (_Dirty) {
                    for (int t = 0; t < exposed.Count; t++) {
                        if (WasReplacedAt(exposed[t].TemplateIndex)) {
                            var s = Session;
                            if (s != null) {
                                if (IsArray) {
                                    throw new NotImplementedException();
                                } else {
                                    var childTemplate = (TValue)exposed[t];
                                    Session.UpdateValue(this, childTemplate);

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
                                    c.LogValueChangesWithDatabase(session);
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
                                c.LogValueChangesWithDatabase(session);
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
                            ((Json)e).LogValueChangesWithDatabase(session);
                        }
                    }
                }
            });
		}

		internal void SetBoundValuesInTuple() {
			if (IsArray) {
				foreach (Json item in _list) {
					item.SetBoundValuesInTuple();
				}
			} else {
                this.ExecuteInScope(() => {
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
                    Add(newJson);
                    newJson.Data = value;
                    hasChanged = true;
                } else {
                    oldJson = (Json)_list[index];
                    if (!CompareDataObjects(oldJson.Data, value)) {
                        oldJson.Data = value;
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
                RemoveAt(i);
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
