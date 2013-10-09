
using Starcounter.Internal.XSON;
using Starcounter.Templates;
using System;
using System.Collections.Generic;

namespace Starcounter {

    partial class Json {

        /// <summary>
        /// 
        /// </summary>
        internal void Dirtyfy() {
            _Dirty = true;
            if (Parent != null)
                Parent.Dirtyfy();
        }

        /// <summary>
        /// 
        /// </summary>
        internal void CheckpointChangeLog() {
            this.ArrayAddsAndDeletes = null;
            var values = list;
            if (this.IsArray) {
                for (int t = 0; t < values.Count; t++) {
                    CheckpointAt(t);
                    var value = values[t];
                    if (value is Json) {
                        ((Json)value).CheckpointChangeLog();
                    }
                }
            }
            else {
                var tjson = (TObject)Template;
                var json = (Json)this;
                for (int t = 0; t < values.Count; t++) {
                    var property = tjson.Properties[t];
                    if (property is TValue) {
                        var tval = property as TValue;
                        if (!tval.IsArray && tval.UseBinding(json.DataAsBindable)) {
                            values[t] = json.GetBound(tval);
                        }
                    }
                    if (this[t] is Json) {
                        (this[t] as Json).CheckpointChangeLog();
                    }
                    CheckpointAt(t);
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
            return (WasReplacedAt(prop.TemplateIndex));
        }

        /// <summary>
        /// Logs all property changes made to this object or its bound data object
        /// </summary>
        /// <param name="session">The session (for faster access)</param>
        internal void LogValueChangesWithDatabase(Session session) {
            if (this.IsArray) {
                LogArrayChangesWithDatabase(session);
            }
            else {
                LogObjectValueChangesWithDatabase(session);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        private void LogArrayChangesWithDatabase(Session session) {
            if (ArrayAddsAndDeletes != null) {
                Session._Changes.AddRange(ArrayAddsAndDeletes);
				ArrayAddsAndDeletes.Clear();

				for (int i = 0; i < list.Count; i++) {
					CheckpointAt(i);
				}
            }
//            foreach (var e in _Values) {
//                (e as Json).LogValueChangesWithDatabase(session);
//            }
 //           if (_Dirty) {


            var property = Template as TValue;
                for (int t = 0; t < _list.Count; t++) {
                    if (WasReplacedAt(t)) {
                        session._Changes.Add( Change.Update( this.Parent as Json,property,t) );
                    }
                    (_list[t] as Json).LogValueChangesWithDatabase(session);
                }
//            }
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
        private void LogObjectValueChangesWithDatabase(Session session ) {
            var template = (TObject)Template;
			var exposed = template.Properties.ExposedProperties;

            if (_Dirty) {
                for (int t = 0; t < exposed.Count; t++) {
                    if (WasReplacedAt(exposed[t].TemplateIndex)) {
                        var s = Session;
                        if (s != null) {
                            if (IsArray) {
                                throw new NotImplementedException();
                            }
                            else {
								var childTemplate = (TValue)exposed[t];
                                Session.UpdateValue((this as Json), childTemplate);

								// TODO:
								// Added this code to make current implementation work.
								// Probably not the correct place to do it though, both
								// for readability and speed.
								if (childTemplate is TContainer) {
									var childJson = (Json)this.Get(childTemplate);
									if (childJson != null){
										childJson.SetBoundValuesInTuple();
										childJson.CheckpointChangeLog();
									}
								}
                            }
                        }
                        CheckpointAt(exposed[t].TemplateIndex);
                    }
                    else {
                        var p = exposed[t];
                        if (p is TContainer) {
                            var c = ((Json)this[p.TemplateIndex]);
                            if (c != null) {
                                c.LogValueChangesWithDatabase(session);
                            }
                        }
                        else {
                            if (IsArray) {
                                throw new NotImplementedException();
                            }
                            else {
                                var j = this as Json;
                                if (((TValue)p).UseBinding(j.DataAsBindable)) {
                                    var val = j.GetBound((TValue)p);
                                    if ( val != list[p.TemplateIndex] ) {
                                        list[p.TemplateIndex] = val;
                                        Session.UpdateValue(j, (TValue)exposed[t]);
                                    }   
                                }
                            }
                        }
                    }
                }
                _Dirty = false;
            }
            else if (template.HasAtLeastOneBoundProperty) {
                for (int t = 0; t < exposed.Count; t++) {
					var value = list[exposed[t].TemplateIndex];
                    if (value is Json) {
                        ((Json)value).LogValueChangesWithDatabase(session);
                    }
                    else {
                        if (IsArray) {
                            throw new NotImplementedException();
                        }
                        else {
                            var j = this as Json;
                            var templ = j.Template as TObject;
                            var p = exposed[t] as TValue;
                            if (p != null && p.UseBinding(j.DataAsBindable)) {
                                var val = j.GetBound(p);

								// TODO:
								// When comparing for example two boxed integers, the != comparison returns
								// false when it should be true so we need to make a call to equals here.
//                                if (val != list[t]) {
								if ((val == null && list[p.TemplateIndex] != null) || (val != null && !val.Equals(list[p.TemplateIndex]))) {
									list[p.TemplateIndex] = val;
									Session.UpdateValue(j, (TValue)exposed[t]);
								}
                            }
                        }
                    }
                }
            }
            else {
                foreach (var e in list) {
                    if (e is Json) {
                        ((Json)e).LogValueChangesWithDatabase(session);
                    }
                }
            }
        }

		private void SetBoundValuesInTuple() {
			if (IsArray) {
				foreach (Json item in _list) {
					item.SetBoundValuesInTuple();
				}
			} else {
				var dataObj = DataAsBindable;
				if (dataObj != null) {
					var valueList = list;
					TObject tobj = (TObject)Template;
					for (int i = 0; i < tobj.Properties.Count; i++) {
						var vt = tobj.Properties[i] as TValue;
						if (vt != null) {
							if (vt is TContainer) {
								var childJson = (Json)Get(vt);
								childJson.SetBoundValuesInTuple();
							} else if (vt != null && vt.UseBinding(dataObj)) {
								valueList[i] = GetBound(vt);
							}
						}
					}
				}
			}
		}
    }
}
