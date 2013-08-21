﻿
using Starcounter.Templates;
using System;
namespace Starcounter {
    partial class Obj {

        /// <summary>
        /// 
        /// </summary>
        internal override void CheckpointChangeLog() {
            _BrandNew = false;
            if (_Dirty) {
                for (int t = 0; t < _Values.Length; t++) {
                    if (_DirtyProperties[t]) {
                        _DirtyProperties[t] = false;
                    }
                    var p = Template.Properties[t];
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
                    var p = Template.Properties[t];
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
            if (_Dirty) {
                for (int t = 0; t < _Values.Length; t++) {
                    if (_DirtyProperties[t]) {
                        var s = Session;
                        if (s != null) {
                            Session.UpdateValue(this, (TValue)Template.Properties[t]);
                        }
                        _DirtyProperties[t] = false;
                    }
                    else {
                        var p = Template.Properties[t];
                        if (p is TContainer) {
                            var c = ((Container)_Values[t]);
                            if (c != null) {
                                c.LogValueChangesWithDatabase(session);
                            }
                        }
                        else {
                            if (((TValue)p).UseBinding(Data)) {
                                var val = this.GetBound((TValue)p);
                                if (!val.Equals(this._BoundDirtyCheck[t])) {
                                    _BoundDirtyCheck[t] = val;
                                    Session.UpdateValue(this, (TValue)Template.Properties[t]);
                                }
                            }
                        }
                    }
                }
                _Dirty = false;
            }
            else if (Template.HasAtLeastOneBoundProperty) {
                for (int t = 0; t < _Values.Length; t++) {
                    var p = Template.Properties[t];
                    if (p is TContainer) {
                        ((Container)_Values[t]).LogValueChangesWithDatabase(session);
                    }
                    else {
                        if (((TValue)p).UseBinding(Data)) {
                            var val = this.GetBound((TValue)p);
                            if (!val.Equals(this._BoundDirtyCheck[t])) {
                                _BoundDirtyCheck[t] = val;
                                Session.UpdateValue(this, (TValue)Template.Properties[t]);
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
