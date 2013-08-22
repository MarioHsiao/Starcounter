

using System;
namespace Starcounter {
    partial class Arr {

        internal override void LogValueChangesWithoutDatabase(Session session) {
            throw new NotImplementedException();
        }

        internal override void CheckpointChangeLog() {
            _Dirty = false;
            _BrandNew = false;
            if (Changes != null) {
                Changes.Clear();
            }
            foreach (var e in QuickAndDirtyArray) {
                e.CheckpointChangeLog();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        internal override void LogValueChangesWithDatabase(Session session) {
            if (Changes != null) {
                Session._Changes.AddRange(this.Changes);
            }
            foreach (var e in QuickAndDirtyArray) {
                e.LogValueChangesWithDatabase(session);
            }
            if (_Dirty) {
                for (int t = 0; t < QuickAndDirtyArray.Count; t++) {
                    QuickAndDirtyArray[t].LogValueChangesWithDatabase(session);
                }
            }
        }
    }
}
