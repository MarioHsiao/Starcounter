
using Starcounter.Internal;
using System;

namespace Starcounter
{
    
    public static class Transaction
    {

        internal static void Commit(int tran_locked_on_thread, int detach_and_free)
        {
            uint r;
            ulong hiter;
            ulong viter;

            for (; ; )
            {
                r = sccoredb.sccoredb_begin_commit(tran_locked_on_thread, out hiter, out viter);
                if (r == 0)
                {
                    // TODO: Handle triggers. Call abort commit on failure.
                    // r = sccoredb.sccoredb_abort_commit(tran_locked_on_thread);

                    r = sccoredb.sccoredb_complete_commit(tran_locked_on_thread, detach_and_free);
                    if (r == 0) break;

                    r = TryRecoverAndPrepareRetryAfterCommitFailed(r);
                    if (r == 0) continue;
                }
                
                throw ErrorCode.ToException(r);
            }
        }

        private static uint TryRecoverAndPrepareRetryAfterCommitFailed(uint r)
        {
            switch (r)
            {
                case Error.SCERRLOGOVERFLOWABORT:
                    return TryRecoverAndPrepareRetryAfterLogOverflowAbort();
                case Error.SCERROUTOFLOGMEMORYABORT:
                    return TryRecoverAndPrepareRetryAfterOufOfLogMemoryAbort();
                default:
                    return r;
            }
        }

        private static uint TryRecoverAndPrepareRetryAfterLogOverflowAbort()
        {
            uint dr_retry;
            uint wait_flags;

            // Log overflow abort. If possible, reset the abort and wait checkpoint
            // to complete before trying again.
            //
            // Note that log overflow only applies when the transaction fails
            // because we would have to write to a space in the log needed on
            // recovery. In case we can't write to a space in the log because it is
            // waiting to be consumed by the replicator we get another error.

            //LogSources.Base.LogWarning("Log overflow detected. Trying to recover.");

            // Wait for checkpoint.

            wait_flags = 1; // Block scheduler.
            //wait_flags = 0; // Don't block scheduler.
            dr_retry = sccoredb.sccoredb_wait_for_low_checkpoint_urgency(wait_flags);
            if (dr_retry != 0) return dr_retry;

            // Reset abort status (restoring transaction to read mode).
            //
            // We do this after waiting for checkpoint so that should the wait fail
            // we don't leave the transaction in a state inconsistent with the
            // exception (no longer aborted despite a log overflow thrown). We
            // could probably safe some CPU by doing this before waiting for
            // checkpoint but since this is error handling it is not worth the
            // added complexity of somehow restoring the log overflow abort state.

            dr_retry = sccoredb.sccoredb_reset_abort();
            if (dr_retry != 0) return dr_retry;

            // Returning success.
            return 0;
        }

        private static uint TryRecoverAndPrepareRetryAfterOufOfLogMemoryAbort()
        {
            uint dr_retry;
            uint wait_flags;

            //LogSources.Base.LogWarning("Failure to allocate log memory. Trying to recover.");

            wait_flags = 1; // Block scheduler.
            //wait_flags = 0; // Don't block scheduler.
            dr_retry = sccoredb.sccoredb_wait_for_high_avail_log_memory(wait_flags);
            if (dr_retry != 0) return dr_retry;

            // Reset abort status (restoring transaction to read mode).
            //
            // We do this after waiting for checkpoint so that should the wait fail
            // we don't leave the transaction in a state inconsistent with the
            // exception (no longer aborted despite a log overflow thrown). We
            // could probably safe some CPU by doing this before waiting for
            // checkpoint but since this is error handling it is not worth the
            // added complexity of somehow restoring the log overflow abort state.

            dr_retry = sccoredb.sccoredb_reset_abort();
            if (dr_retry != 0) return dr_retry;

            // Returning success.
            return 0;
        }
    }
}
