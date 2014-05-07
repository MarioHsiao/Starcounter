
namespace Starcounter.Internal {

    /// <summary>
    /// </summary>
    public static class TaskHelper { // Internal
        /// <summary>
        /// Creates or reuses an implicit transaction that will be used 
        /// during the current task
        /// </summary>
        internal static void CreateOrSetImplicitTransaction() {
            var data = ThreadData.Current;
            if (data._handle == 0)
                CreateImplicitTransaction(data);

            uint r = sccoredb.sccoredb_set_current_transaction(0, data._handle, data._verify);
            if (r == 0) return;
            throw ErrorCode.ToException(r);
        }

        /// <summary>
        /// Called when exiting managed task entry point to cleanup managed
        /// resources attached to the task.
        /// </summary>
        public static void Reset() {
            ResetCurrentTransaction();
        }

        private static void ResetCurrentTransaction() {
            // Clean up implicit transaction if it exists.
            var data = ThreadData.Current;
            if (data._handle != 0) {
                uint r = sccoredb.sccoredb_free_transaction(data._handle, data._verify);
                if (r == 0) {
                    data._verify = 0xFF;
                    data._handle = 0;
                    return;
                }
                throw ErrorCode.ToException(r);
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

        private static void CreateImplicitTransaction(ThreadData data) {
            ulong handle;
            ulong verify;
            uint flags = sccoredb.MDB_TRANSCREATE_READ_ONLY;

            uint r = sccoredb.sccoredb_create_transaction(
                flags,
                out handle,
                out verify
                );

            if (r == 0) {
                data._handle = handle;
                data._verify = verify;
                return;
            }
            throw ErrorCode.ToException(r);
        }
    }
}
