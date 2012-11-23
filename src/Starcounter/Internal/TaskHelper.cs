﻿
namespace Starcounter.Internal {

    /// <summary>
    /// </summary>
    public static class TaskHelper { // Internal

        /// <summary>
        /// Called when exiting managed task entry point to cleanup managed
        /// resources attached to the task.
        /// </summary>
        public static void Reset() {
            ResetCurrentTransaction();
            ThreadData.Current.CleanupAllObjects();
        }

        private static void ResetCurrentTransaction() {
            if (Transaction._current != null) {
                uint r = sccoredb.sccoredb_set_current_transaction(0, 0, 0);
                if (r == 0) {
                    Transaction._current = null;
                    return;
                }
                throw ErrorCode.ToException(r);
            }
        }
    }
}
