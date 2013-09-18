
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
            _BrandNew = false;
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
                Session._Changes.AddRange(this.ArrayAddsAndDeletes);
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
            if (_Dirty) {
                for (int t = 0; t < _list.Count; t++) {
                    if (WasReplacedAt(t)) {
                        var s = Session;
                        if (s != null) {
                            if (IsArray) {
                                throw new NotImplementedException();
                            }
                            else {
                                Session.UpdateValue((this as Json), (TValue)template.Properties[t]);
                            }
                        }
                        CheckpointAt(t);
                    }
                    else {
                        var p = template.Properties[t];
                        if (p is TContainer) {
                            var c = ((Json)this[t]);
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
                                    if ( val != list[t] ) {
                                        list[t] = val;
                                        Session.UpdateValue(j, (TValue)template.Properties[t]);
                                    }   
                                }
                            }
                        }
                    }
                }
                _Dirty = false;
            }
            else if (template.HasAtLeastOneBoundProperty) {
                for (int t = 0; t < list.Count; t++) {
                    var value = list[t];
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
                            var p = templ.Properties[t] as TValue;
                            if (p != null && p.UseBinding(j.DataAsBindable)) {
                                var val = j.GetBound(p);
                                if (val != list[t]) {
                                    list[t] = val;
                                    Session.UpdateValue(j, (TValue)template.Properties[t]);
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
    }
}
