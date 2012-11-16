﻿// ***********************************************************************
// <copyright file="Transaction.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Internal;
using System;

namespace Starcounter
{

    /// <summary>
    /// </summary>
    public partial class Transaction
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

    /// <summary>
    /// Represents a long running transaction.
    /// </summary>
    public partial class Transaction : IDisposable {

        private const ulong _INVALID_VERIFY = 0xFF;

        /// <summary>
        /// Current long running transaction.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Note that in the case of a short running transaction (a transaction
        /// scope) this value will be null (motivation for not supporting
        /// fetching current transaction).
        /// </para>
        /// </remarks>
        [ThreadStatic]
        private static Transaction _current; // TODO: Only long running transactions.

        /// <summary>
        /// </summary>
        public static void SetCurrent(Transaction value) {
            ulong handle;
            ulong verify;

            if (value != null) {
                handle = value._handle;
                verify = value._verify;
            }
            else {
                handle = 0;
                verify = _INVALID_VERIFY;
            }

            uint r = sccoredb.sccoredb_set_current_transaction(0, handle, verify);
            if (r == 0) {
                _current = value;
                return;
            }

            throw ToException(value, r);
        }

        private static Exception ToException(Transaction transaction, uint r) {
            // If the error indicates that the object isn't owned we check if
            // verification is set to 0. If so the object has been disposed.

            if (
                r == Error.SCERRTRANSACTIONNOTOWNED &&
                transaction != null &&
                transaction._verify == _INVALID_VERIFY
                ) {
                return new ObjectDisposedException(null);
            }

            return ErrorCode.ToException(r);
        }

        private readonly ulong _handle;
        private ulong _verify;

        private readonly TransactionScrap _scrap;

        /// <summary>
        /// </summary>
        public Transaction() {
            ulong handle;
            ulong verify;
            uint r = sccoredb.sccoredb_create_transaction(
                sccoredb.MDB_TRANSCREATE_MERGING_WRITES,
                out handle,
                out verify
                );
            if (r == 0) {
                try {
                    _scrap = new TransactionScrap(handle, verify);
                    _handle = handle;
                    _verify = verify;
                    return;
                }
                catch (Exception) {
                    _verify = _INVALID_VERIFY;
                    r = sccoredb.sccoredb_free_transaction(handle, verify);
                    if (r != 0) ExceptionManager.HandleInternalFatalError(r);
                    throw;
                }
            }
            _verify = _INVALID_VERIFY;
            throw ErrorCode.ToException(r);
        }

        /// <summary>
        /// </summary>
        ~Transaction() {
            if (_verify == _INVALID_VERIFY) return;

            // Add transaction scrap to scrap heap, a job will be scheduled on
            // the owning thread to release the transaction (the finalizer
            // thread is not the owner of the transaction so it's not allowed
            // to).

            _verify = _INVALID_VERIFY;
            ScrapHeap.ThrowAway(_scrap);
        }

        /// <summary>
        /// <c>System.IDisposable</c> interface implementation.
        /// </summary>
        public void Dispose() {
            uint r;

            // If another thread attempts to dispose the context at the same
            // time as the current thread attempts to dispose it then it will
            // fail since it doesn't own the object (assuming that this does).
            // Therefore it's thread safe regardless of if the verification
            // used by the other thread is 0 or the original value.
            //
            // If disposed context is the current context then the current
            // context is always set to null before the context is disposed.

            if (_current == this) SetCurrent(null);
            r = sccoredb.sccoredb_free_transaction(_handle, _verify);
            if (r == 0) {
                _verify = _INVALID_VERIFY;
                GC.SuppressFinalize(this);
                return;
            }
            if (r == Error.SCERRTRANSACTIONNOTOWNED && _verify == _INVALID_VERIFY) {
                return;
            }
            throw ErrorCode.ToException(r);
        }

        /// <summary>
        /// </summary>
        public void Commit() {
            Transaction current = _current;
            if (current == this) {
                Transaction.Commit(0, 0);
            }
            else {
                Transaction.SetCurrent(this);
                try {
                    Transaction.Commit(0, 0);
                }
                finally {
                    // We can't guarantee that old transaction is restored here
                    // since in theory the transaction or current context can
                    // have been manipulated by user code preventing this from
                    // happening. But if this is so there will be an exception
                    // at least.
                    //
                    // NOTE:
                    // It should be possible to make restore fail-safe if
                    // transaction is still bound while executing the commit
                    // within the context of the other transaction.

                    Transaction.SetCurrent(current);
                }
            }
        }

        /// <summary>
        /// </summary>
        public void Rollback() {
            Transaction current = _current;
            if (current == this) {
                uint r = sccoredb.sccoredb_rollback();
                if (r == 0) return;
                throw ToException(this, r);
            }
            else {
                Transaction.SetCurrent(this);
                try {
                    uint r = sccoredb.sccoredb_rollback();
                    if (r == 0) return;
                    throw ToException(this, r);
                }
                finally {
                    Transaction.SetCurrent(current);
                }
            }
        }
    }

    internal sealed class TransactionScrap {

        internal TransactionScrap Next;

        private readonly ulong _handle;
        private readonly ulong _verify;

        internal TransactionScrap(ulong handle, ulong verify) {
            //Next = null;
            _handle = handle;
            _verify = verify;
        }

        internal Byte OwnerCpu { get { return (byte)_verify; } }

        internal void Cleanup() {
            uint r = sccoredb.sccoredb_free_transaction(_handle, _verify);
            if (r == 0) return;
            throw ErrorCode.ToException(r);
        }
    }
    
    /// <summary>
    /// Used to clean up garbage collected objects that could not be freed by
    /// garbage collector directly because the associated memory is associated
    /// with a specific scheduler.
    /// </summary>
    public sealed class ScrapHeap {

        private static ScrapHeap[] _instances;
        private static unsafe void* _hsched;

        /// <summary>
        /// </summary>
        public unsafe static void Setup(void* hsched) {
            var schedulerCount = sccorelib.GetCpuCount((IntPtr)hsched);
            var instances = new ScrapHeap[schedulerCount];
            for (var i = 0; i < instances.Length; i++) {
                instances[i] = new ScrapHeap();
            }
            _instances = instances;

            _hsched = hsched;
        }

        internal static void ThrowAway(TransactionScrap scrap) {
            byte cpuNumber;
            ScrapHeap instance;
            TransactionScrap next;
            uint r;

            cpuNumber = scrap.OwnerCpu;
            instance = _instances[cpuNumber];

            // Add the scrap to the scrap heap.
            lock (instance._syncRoot) {
                next = instance._firstScrap;
                scrap.Next = next;
                instance._firstScrap = scrap;
            }

            // If the scrap was the first bit of scrap on the heap we post a
            // request to cleanup the scrap on the heap.
            if (next != null) return;

            unsafe {
                r = sccorelib.cm2_schedule(
                    _hsched,
                    cpuNumber,
                    sccorelib_ext.TYPE_RECYCLE_SCRAP,
                    0,
                    0,
                    0,
                    0
                    );
            }

            if (r == 0) return;
            throw ErrorCode.ToException(r); // TODO: Handle queue full.
        }

        /// <summary>
        /// </summary>
        public static void RecycleScrap() {

            ThreadHelper.SetYieldBlock();
            try {
                var schedulerNumber = sccorelib.GetCpuNumber();
                var instance = _instances[schedulerNumber];
                instance.CleanupAll();
            }
            finally {
                ThreadHelper.ReleaseYieldBlock();
            }
        }

        private readonly object _syncRoot;
        private volatile TransactionScrap _firstScrap;

        private ScrapHeap() {
            _syncRoot = new object();
        }

        private void CleanupAll() {
            TransactionScrap scrap;

            lock (_syncRoot) {
                scrap = _firstScrap;
                _firstScrap = null;
            }

            while (scrap != null) {
                scrap.Cleanup();
                scrap = scrap.Next;
            }
        }
    }
}
