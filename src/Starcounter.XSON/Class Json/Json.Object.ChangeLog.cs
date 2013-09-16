
using Starcounter.Internal.XSON;
using Starcounter.Templates;
using System;
using System.Collections.Generic;

namespace Starcounter {

    partial class Container {

        /// <summary>
        /// Keeps track on when we added/inserted or removed elements
        /// </summary>
        internal List<Change> ArrayAddsAndDeletes = null;

        /// <summary>
        /// 
        /// </summary>
        internal void CheckpointChangeLog() {
            _Values._BrandNew = false;
            this.ArrayAddsAndDeletes = null;
            if (this.IsArray) {
                for (int t = 0; t < _Values.Count; t++) {
                    _Values.CheckpointAt(t);
                    var value = _Values[t];
                    if (value is Container) {
                        ((Container)value).CheckpointChangeLog();
                    }
                }
            }
            else {
                var tjson = (TObject)Template;
                var json = (Json)this;
                for (int t = 0; t < _Values.Count; t++) {
                    var property = tjson.Properties[t];
                    if (property is TValue) {
                        var tval = property as TValue;
                        if (!tval.IsArray && tval.UseBinding(json.DataAsBindable)) {
                            _Values[t] = json.GetBound(tval);
                        }
                    }
                    if (_Values[t] is Container) {
                        (_Values[t] as Container).CheckpointChangeLog();
                    }
                    else {
                        if (property is TObject) {
                            (property.Wrap(_Values[t]) as Json).CheckpointChangeLog();
                        }
                    }
                    _Values.CheckpointAt(t);
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
            return (_Values.WasReplacedAt(prop.TemplateIndex));
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
                for (int t = 0; t < _Values.Count; t++) {
                    if (_Values.WasReplacedAt(t)) {
                        session._Changes.Add( Change.Update( this.Parent as Json,property,t) );
                    }
                    (_Values[t] as Json).LogValueChangesWithDatabase(session);
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
                for (int t = 0; t < _Values.Count; t++) {
                    if (_Values.WasReplacedAt(t)) {
                        var s = Session;
                        if (s != null) {
                            if (IsArray) {
                                throw new NotImplementedException();
                            }
                            else {
                                Session.UpdateValue((this as Json), (TValue)template.Properties[t]);
                            }
                        }
                        _Values.CheckpointAt(t);
                    }
                    else {
                        var p = template.Properties[t];
                        if (p is TContainer) {
                            var c = ((Container)_Values[t]);
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
                                    if (!val.Equals(_Values[t])) {
                                        _Values[t] = val;
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
                for (int t = 0; t < _Values.Count; t++) {
                    var value = _Values[t];
                    if (value is Container) {
                        ((Container)value).LogValueChangesWithDatabase(session);
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
                                if (val != _Values[t]) {
                                    _Values[t] = val;
                                    Session.UpdateValue(j, (TValue)template.Properties[t]);
                                }
                            }
                        }
                    }
                }
            }
            else {
                foreach (var e in _Values) {
                    if (e is Container) {
                        ((Container)e).LogValueChangesWithDatabase(session);
                    }
                }
            }
        }
    }
}
