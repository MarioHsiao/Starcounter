

using Starcounter.Internal.XSON;
using System;
using System.Collections.Generic;

namespace Starcounter {
    partial class Arr {

        /// <summary>
        /// Keeps track on when we added/inserted or removed elements
        /// </summary>
        internal List<Change> ArrayAddsAndDeletes = null;

        internal override void CheckpointChangeLog() {
            _Dirty = false;
            _BrandNew = false;
            if (ArrayAddsAndDeletes != null) {
                ArrayAddsAndDeletes.Clear();
            }
            foreach (var e in _Values) {
                (e as Json).CheckpointChangeLog();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        internal override void LogValueChangesWithDatabase(Session session) {
            if (ArrayAddsAndDeletes != null) {
                Session._Changes.AddRange(this.ArrayAddsAndDeletes);
            }
            foreach (var e in _Values) {
                (e as Json).LogValueChangesWithDatabase(session);
            }
            if (_Dirty) {
                for (int t = 0; t < _Values.Count; t++) {
                    (_Values[t] as Json).LogValueChangesWithDatabase(session);
                }
            }
        }
    }
}
