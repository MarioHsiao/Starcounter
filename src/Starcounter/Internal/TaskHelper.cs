using System;

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
        }

        private static void ResetCurrentTransaction() {
            uint r = sccoredb.star_set_current_transaction(0, 0, 0);
            ImplicitTransaction.Release();
            Transaction.SetManagedCurrentToNull();
        }
    }
}
