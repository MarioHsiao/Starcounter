
namespace Starcounter.Internal {

    /// <summary>
    /// </summary>
    public static class TaskHelper { // Internal
        internal static void CreateOrSetImplicitTransaction() {
            var state = ThreadData.Current;

            if (state._implicitTransaction == null)
                state._implicitTransaction = new ImplicitTransaction();

            state._implicitTransaction.SetCurrent();
        }

        /// <summary>
        /// Called when exiting managed task entry point to cleanup managed
        /// resources attached to the task.
        /// </summary>
        public static void Reset() {
            ResetCurrentTransaction();
        }

        private static void ResetCurrentTransaction() {
            var threadData = ThreadData.Current;
            if (threadData._implicitTransaction != null) {
                threadData._implicitTransaction.Cleanup();
                threadData._implicitTransaction = null;
            }

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
