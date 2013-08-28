
using Starcounter.Templates;
using System;
namespace Starcounter {
    partial class Json<DataType> {

        /// <summary>
        /// 
        /// </summary>
        internal override void CheckpointChangeLog() {
            if (IsArray) {
//                CheckpointChangeLog((ArrSchema<Json<object>>)Template);
                throw new NotImplementedException();
            }
            else {
                CheckpointChangeLog((Schema<Json<object>>)Template);
            }
        }

        private void CheckpointChangeLog( Schema<Json<object>> template ) {
            _BrandNew = false;
            if (_Dirty) {
                for (int t = 0; t < _Values.Length; t++) {
                    if (_DirtyProperties[t]) {
                        _DirtyProperties[t] = false;
                    }
                    var p = template.Properties[t];
                    if (p is TContainer) {
                        ((Container)_Values[t]).CheckpointChangeLog();
                    }
                    if (p is TValue) {
                        var tv = (TValue)p;
                        if (tv.Bind != null) {
                            _BoundDirtyCheck[t] = this.Get(tv);
                        }
                    }
                }
                _Dirty = false;
            }
            else {
                for (int t = 0; t < _Values.Length; t++) {
                    var p = template.Properties[t];
                    if (p is TValue) {
                        var tv = (TValue)p;
                        if (tv.Bind != null) {
                            _BoundDirtyCheck[t] = this.Get(tv);
                        }
                    }
                    if (p is TContainer) {
                        ((Container)_Values[t]).CheckpointChangeLog();
                    }
                }
            }
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
            return (_DirtyProperties[prop.TemplateIndex]);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        internal override void LogValueChangesWithoutDatabase(Session session) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Logs all property changes made to this object or its bound data object
        /// </summary>
        /// <param name="session">The session (for faster access)</param>
        internal override void LogValueChangesWithDatabase(Session session) {
            if (this.IsArray) {
                throw new NotImplementedException();
//                LogValueChangesWithDatabase(session, (ArrSchema<Json<object>>)template);
            }
            else {
                LogValueChangesWithDatabase(session, (Schema<Json<object>>)Template);
            }
        }

        private void LogValueChangesWithDatabase(Session session, Schema<Json<object>> template ) {
            if (_Dirty) {
                for (int t = 0; t < _Values.Length; t++) {
                    if (_DirtyProperties[t]) {
                        var s = Session;
                        if (s != null) {
                            Session.UpdateValue(this, (TValue)template.Properties[t]);
                        }
                        _DirtyProperties[t] = false;
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
                            if (((TValue)p).UseBinding(DataAsBindable)) {
                                var val = this.GetBound((TValue)p);
                                if (!val.Equals(this._BoundDirtyCheck[t])) {
                                    _BoundDirtyCheck[t] = val;
                                    Session.UpdateValue(this, (TValue)template.Properties[t]);
                                }
                            }
                        }
                    }
                }
                _Dirty = false;
            }
            else if (template.HasAtLeastOneBoundProperty) {
                for (int t = 0; t < _Values.Length; t++) {
                    var p = template.Properties[t];
                    if (p is TContainer) {
                        ((Container)_Values[t]).LogValueChangesWithDatabase(session);
                    }
                    else {
                        if (((TValue)p).UseBinding(DataAsBindable)) {
                            var val = this.GetBound((TValue)p);
                            if (!val.Equals(this._BoundDirtyCheck[t])) {
                                _BoundDirtyCheck[t] = val;
                                Session.UpdateValue(this, (TValue)template.Properties[t]);
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
