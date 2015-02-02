﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter.Binding;
using Starcounter.Internal;

namespace Starcounter {
    /// <summary>
    /// Class that keeps track of created (kernel) transactions during a task. All transactions 
    /// that have not been explicitly claimed will be released in the end of the task, the others
    /// is up to the claimee to properly release them when finished.
    /// </summary>
    /// <remarks>
    /// The transactions created using <c>Db.Transact</c> is not handled by this class. The transactions here
    /// are created using <c>Db.Scope</c> or <c>Transaction</c> object directly.
    /// </remarks>
    internal static class TransactionManager {
        public const int ShortListCount = 5;

        /// <summary>
        /// Struct containing references to the fast short list
        /// </summary>
        private unsafe struct TransactionRefs {
            internal TransactionHandle* refs;
            internal int used;
        }

        [ThreadStatic]
        private static TransactionRefs transactionRefs;

        [ThreadStatic]
        private static List<TransactionHandle> SlowList;

        [ThreadStatic]
        private static TransactionHandle currentHandle;

        internal unsafe static void Init(TransactionHandle* shortListPtr) {
            TransactionRefs refs;
            refs.refs = shortListPtr;
            refs.used = 0;
            transactionRefs = refs;
            currentHandle = TransactionHandle.Invalid;
        }

        /// <summary>
        /// Creates a new transaction in the kernel and adds a reference to the transactionlist and 
        /// returns a handle to the transaction.
        /// </summary>
        /// <param name="readOnly"></param>
        /// <param name="detectConflicts"></param>
        /// <returns></returns>
        internal static TransactionHandle Create(bool readOnly, bool detectConflicts) {
            var tr = transactionRefs;
            int index = tr.used;

            ulong handle;
            ulong verify;
            uint ec;
            
            uint flags = detectConflicts ? 0 : sccoredb.MDB_TRANSCREATE_MERGING_WRITES;
            if (readOnly)
                flags |= sccoredb.MDB_TRANSCREATE_READ_ONLY;

            ec = sccoredb.sccoredb_create_transaction(flags, out handle, out verify);
            if (ec == 0) {
                try {
                    TransactionHandle th = new TransactionHandle(handle, verify, flags, index);

                    if (index < ShortListCount) {
                        unsafe {
                            tr.refs[index] = th;
                        }
                    } else {
                        // The ShortList is filled. We need to switch over to slower list that can manage more transactions.
                        if (SlowList == null)
                            SlowList = new List<TransactionHandle>();

                        // The index will be recalculated based on the shortlist when used.
                        Debug.Assert(SlowList.Count == (index - ShortListCount));
                        SlowList.Add(th);
                    }

                    transactionRefs.used++;

                    return th;
                } catch {
                    sccoredb.sccoredb_free_transaction(handle, verify);
                    throw;
                }
            }

            throw ErrorCode.ToException(ec);
        }

        internal static TransactionHandle CreateAndSetCurrent(bool readOnly, bool detectConflicts) {
            var tr = transactionRefs;
            int index = tr.used;

            ulong handle;
            ulong verify;
            uint ec;

            uint flags = detectConflicts ? 0 : sccoredb.MDB_TRANSCREATE_MERGING_WRITES;
            if (readOnly)
                flags |= sccoredb.MDB_TRANSCREATE_READ_ONLY;

            ec = sccoredb.sccoredb_create_transaction_and_set_current(flags, 0, out handle, out verify);
            if (ec == 0) {
                try {
                    TransactionHandle th = new TransactionHandle(handle, verify, flags, index);

                    if (index < ShortListCount) {
                        unsafe {
                            tr.refs[index] = th;
                        }
                    } else {
                        // The ShortList is filled. We need to switch over to slower list that can manage more transactions.
                        if (SlowList == null)
                            SlowList = new List<TransactionHandle>();

                        // The index will be recalculated based on the shortlist when used.
                        Debug.Assert(SlowList.Count == (index - ShortListCount));
                        SlowList.Add(th);
                    }

                    transactionRefs.used++;

                    return th;
                } catch {
                    sccoredb.sccoredb_free_transaction(handle, verify);
                    throw;
                }
            }

            throw ErrorCode.ToException(ec);
        }

        /// <summary>
        /// Creates a new kerneltransaction and returns a handle to it. Does not add a reference to 
        /// it (i.e making sure that the transactions is released is up to the caller).
        /// </summary>
        /// <param name="readOnly"></param>
        /// <param name="detecConflicts"></param>
        /// <returns></returns>
        internal static TransactionHandle CreateClaimed(bool readOnly, bool detectConflicts) {
            ulong handle;
            ulong verify;
            uint ec;
            
            uint flags = detectConflicts ? 0 : sccoredb.MDB_TRANSCREATE_MERGING_WRITES;
            if (readOnly)
                flags |= sccoredb.MDB_TRANSCREATE_READ_ONLY;

            ec = sccoredb.sccoredb_create_transaction(flags, out handle, out verify);
            if (ec == 0)
                return new TransactionHandle(handle, verify, flags, -1);

            throw ErrorCode.ToException(ec);
        }

        /// <summary>
        /// Releases the kerneltransaction and removes the handle from the references.
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="index"></param>
        internal static uint Dispose(TransactionHandle handle) {
            uint ec = 0;
            if (currentHandle == handle)
                SetCurrentTransaction(TransactionHandle.Invalid);

            if (handle.IsAlive) {
                // TODO:
                // Throw error if already disposed?
                ec  = sccoredb.sccoredb_free_transaction(handle.handle, handle.verify);
                if (ec != 0)
                    return ec;
            }

            if (handle.index >= 0) {
                TransactionRefs tr = transactionRefs;
                TransactionHandle keptHandle;
                if (handle.index < ShortListCount) {
                    unsafe {
                        keptHandle = tr.refs[handle.index];
                        if (keptHandle == handle)
                            tr.refs[handle.index] = TransactionHandle.Invalid;
                    }
                } else {
                    int calcIndex = handle.index - ShortListCount;

                    keptHandle = SlowList[calcIndex];
                    if (keptHandle == handle)
                        SlowList[calcIndex] = TransactionHandle.Invalid;
                }

                // If the last one added is the one disposed we decrease 
                // the used count and allo the position to be reused.
                if (handle.index == (tr.used - 1))
                    transactionRefs.used--;
            }
            return 0;
        }

        /// <summary>
        /// Marks the transaction with the specified index as temporary in use. This means
        /// the transaction cannot be cleaned up in the end of the scope, but will be cleaned
        /// up in the end of the request, unless the ownership of the transaction is claimed.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        internal static void TemporaryRef(TransactionHandle handle) {
            if (handle.index == -1)
                return;
            VerifyHandle(handle);

            if (handle.index < ShortListCount) {
                unsafe {
                    transactionRefs.refs[handle.index].SetTemporaryRef();
                }
            } else {
                SlowList[handle.index - ShortListCount].SetTemporaryRef();
            }
        }

        internal static bool HasTemporaryRef(TransactionHandle handle) {
            if (handle.index == -1)
                return true;

            if (handle.index < ShortListCount) {
                unsafe {
                    return transactionRefs.refs[handle.index].HasTemporaryRef();
                }
            } else {
                return SlowList[handle.index - ShortListCount].HasTemporaryRef();
            }
        }

        internal static TransactionHandle ClaimOwnership(TransactionHandle handle) {
            VerifyHandle(handle);

            TransactionHandle keptHandle;
            var tr = transactionRefs;

            if (handle.index < ShortListCount) {
                unsafe {        
                    keptHandle = tr.refs[handle.index];
                    tr.refs[handle.index] = TransactionHandle.Invalid;
                }
            } else {
                int calcIndex = handle.index - ShortListCount;
                keptHandle = SlowList[calcIndex];
                SlowList[calcIndex] = TransactionHandle.Invalid;
            }
            keptHandle.index = -1;
            return keptHandle;
        }

        internal static TransactionHandle CurrentTransaction {
            get { return currentHandle; }
        }

        internal static TransactionHandle GetCurrentAndSetToNoneManagedOnly() {
            var handle = currentHandle;
            currentHandle = TransactionHandle.Invalid;
            return handle;
        }

        internal static void SetCurrentTransaction(TransactionHandle handle) {
            if (currentHandle == handle)
                return;

            uint ec = sccoredb.sccoredb_set_current_transaction(0, handle.handle, handle.verify);
            if (ec == 0) {
                currentHandle = handle;
                return;
            }

            // TODO: 
            // old solution checked for ObjectDisposedException. See Transaction.ToException.
            throw ErrorCode.ToException(ec);
        }

        internal static TransactionHandle Get(int index) {
            if (index < 0)
                return TransactionHandle.Invalid;

            if (index < ShortListCount) {
                unsafe {
                    return transactionRefs.refs[index];
                }
            }
            return SlowList[index - ShortListCount];
        }

        internal static unsafe void Cleanup() {
            TransactionHandle th;
            TransactionRefs tr = transactionRefs;
            uint ec;

            SetCurrentTransaction(TransactionHandle.Invalid);

            int fastCount = (tr.used >= ShortListCount) ? ShortListCount : tr.used;

            for (int i = 0; i < fastCount; i++) {
                // All handles that have been created will be cleaned up.
                // If noone have taken ownership (i.e. the session/viewmodel)
                // the kerneltransaction will be released.
                // If someone have taken ownership it is up to them to properly 
                // dispose the kerneltransaction.

                th = tr.refs[i];
                if (th.IsAlive) {
                    ec = sccoredb.sccoredb_free_transaction(th.handle, th.verify);
                    if (ec == 0)
                        continue;
                    // TODO:
                    // How do we handle exception here? We need to clean all transactions before throwing anything.
                    throw ErrorCode.ToException(ec);
                }
            }

            if (tr.used > ShortListCount){
                List<TransactionHandle> slowList = SlowList;
                for (int i = 0; i < slowList.Count; i++) {
                    th = slowList[i];
                    if (th.IsAlive) {
                        ec = ec  = sccoredb.sccoredb_free_transaction(th.handle, th.verify);
                        if (ec == 0)
                            continue;

                        // TODO:
                        // How do we handle exception here? We need to clean all transactions before throwing anything.
                        throw ErrorCode.ToException(ec);
                    }
                }
                SlowList = null;
            }

            transactionRefs.refs = null;
            transactionRefs.used = 0;
        }

        /// <summary>
        /// Adds <paramref name="obj"/> to the write list of the
        /// currently attached transaction.
        /// </summary>
        /// </exception>
        /// <param name="obj">The object to be added to the transaction
        /// write list. If the object is not an instance of a database
        /// class, an exception will be raised.</param>
        internal static void Touch(object obj) {
            var proxy = obj as IObjectProxy;
            if (proxy == null) {
                if (obj == null) {
                    throw new ArgumentNullException("obj");
                } else {
                    throw ErrorCode.ToException(
                        Error.SCERRCODENOTENHANCED,
                        string.Format("The type {0} is not a database class.", obj.GetType()),
                        (msg, inner) => { return new InvalidCastException(msg, inner); });
                }
            }

            Touch(proxy);
        }

        /// <summary>
        /// Adds <paramref name="proxy"/> to the write list of the
        /// currently attached transaction.
        /// </summary>
        /// </exception>
        /// <param name="proxy">The proxy referencing the kernel
        /// object to be added to the transaction write list.</param>
        internal static void Touch(IObjectProxy proxy) {
            var dr = sccoredb.SCObjectFakeWrite(proxy.Identity, proxy.ThisHandle);
            if (dr != 0) throw ErrorCode.ToException(dr);
        }

        /// <summary>
        /// Executes some code within this transaction scope.
        /// </summary>
        /// <param name="action">Delegate that is called on transaction.</param>
        internal static void Scope(TransactionHandle handle, Action action) {
            var old = currentHandle;

            try {
                SetCurrentTransaction(handle);
                action();
            } finally {
                SetCurrentTransaction(old);
            }
        }

        internal static void Scope<T>(TransactionHandle handle, Action<T> action, T arg) {
            var old = currentHandle;

            try {
                SetCurrentTransaction(handle);
                action(arg);
            } finally {
                SetCurrentTransaction(old);
            }
        }

        internal static void Scope<T1, T2>(TransactionHandle handle, Action<T1, T2> action, T1 arg1, T2 arg2) {
            var old = currentHandle;

            try {
                SetCurrentTransaction(handle);
                action(arg1, arg2);
            } finally {
                SetCurrentTransaction(old);
            }
        }

        internal static void Scope<T1, T2, T3>(TransactionHandle handle, Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3) {
            var old = currentHandle;

            try {
                SetCurrentTransaction(handle);
                action(arg1, arg2, arg3);
            } finally {
                SetCurrentTransaction(old);
            }
        }

        internal static void Scope<T1, T2, T3, T4>(TransactionHandle handle, Action<T1, T2, T3, T4> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4) {
            var old = currentHandle;

            try {
                SetCurrentTransaction(handle);
                action(arg1, arg2, arg3, arg4);
            } finally {
                SetCurrentTransaction(old);
            }
        }

        internal static TResult Scope<TResult>(TransactionHandle handle, Func<TResult> func) {
            var old = currentHandle;

            try {
                SetCurrentTransaction(handle);
                return func();
            } finally {
                SetCurrentTransaction(old);
            }
        }

        internal static TResult Scope<T, TResult>(TransactionHandle handle, Func<T, TResult> func, T arg) {
            var old = currentHandle;

            try {
                SetCurrentTransaction(handle);
                return func(arg);
            } finally {
                SetCurrentTransaction(old);
            }
        }

        internal static TResult Scope<T1, T2, TResult>(TransactionHandle handle, Func<T1, T2, TResult> func, T1 arg1, T2 arg2) {
            var old = currentHandle;

            try {
                SetCurrentTransaction(handle);
                return func(arg1, arg2);
            } finally {
                SetCurrentTransaction(old);
            }
        }

        internal static TResult Scope<T1, T2, T3, TResult>(TransactionHandle handle, Func<T1, T2, T3, TResult> func, T1 arg1, T2 arg2, T3 arg3) {
            var old = currentHandle;

            try {
                SetCurrentTransaction(handle);
                return func(arg1, arg2, arg3);
            } finally {
                SetCurrentTransaction(old);
            }
        }

        internal static TResult Scope<T1, T2, T3, T4, TResult>(TransactionHandle handle, Func<T1, T2, T3, T4, TResult> func, T1 arg1, T2 arg2, T3 arg3, T4 arg4) {
            var old = currentHandle;

            try {
                SetCurrentTransaction(handle);
                return func(arg1, arg2, arg3, arg4);
            } finally {
                SetCurrentTransaction(old);
            }
        }

        internal static void MergeTransaction(TransactionHandle mainHandle, TransactionHandle toMergeHandle) {
            var old = currentHandle;
            uint ec;
            
            try {
                SetCurrentTransaction(mainHandle);
                ec = sccoredb.star_transaction_merge_into_current(toMergeHandle.handle, toMergeHandle.verify);
                if (ec == 0) {
                    // TODO: 
                    // Do we need to dispose the merged transaction or is that done when merging?
                    Dispose(toMergeHandle);
                    return;
                }

                throw ErrorCode.ToException(ec);
            } finally {
                SetCurrentTransaction(old);
            }
        }

        internal static void Commit(TransactionHandle handle) {
            Scope(handle, () => {
                Commit(0, 0);
            });
        }

        /// <summary>
        /// Commits current transaction.
        /// </summary>
        /// <param name="tran_locked_on_thread"></param>
        /// <param name="detach_and_free"></param>
        internal static void Commit(int tran_locked_on_thread, int detach_and_free) {
            uint r;
            ulong hiter;
            ulong viter;

            for (; ; ) {
                r = sccoredb.sccoredb_begin_commit(tran_locked_on_thread, out hiter, out viter);
                if (r == 0) {
                    // TODO: Handle triggers. Call abort commit on failure.
                    // r = sccoredb.sccoredb_abort_commit(tran_locked_on_thread);

                    r = sccoredb.sccoredb_complete_commit(
                            tran_locked_on_thread, detach_and_free
                            );
                    if (r == 0) break;
                }

                String additionalErrorInfo = null;
                unsafe {
                    char* unsafeAdditionalErrorInfo;
                    r = sccoredb.star_get_last_error(&unsafeAdditionalErrorInfo);
                    if (unsafeAdditionalErrorInfo != null) {
                        additionalErrorInfo = string.Concat(new string(unsafeAdditionalErrorInfo), ".");
                    }
                }
                throw ErrorCode.ToException(r, additionalErrorInfo);
            }
        }

        /// <summary>
        /// Rollbacks uncommitted changes on transaction.
        /// </summary>
        internal static void Rollback(TransactionHandle handle) {
            Scope(handle, () => {
                uint ec = sccoredb.sccoredb_rollback();
                if (ec == 0) return;

                // TODO:
                // Original implementation checked for ObjectDisposedException. See Transaction.ToException().
                throw ErrorCode.ToException(ec);

            });
        }

        [Conditional("DEBUG")]
        private static void VerifyHandle(TransactionHandle handle) {
            if (handle.index == -1)
                return;

            TransactionHandle keptHandle = TransactionManager.Get(handle.index);
            if (!keptHandle.Equals(handle))
                throw ErrorCode.ToException(Error.SCERRUNSPECIFIED);
        }
    }

    public struct TransactionHandle {
        internal const uint INVALID_VERIFY = 0xFF;
        internal const uint FLAG_TEMPORARY_REF = 0x8000;

        internal static TransactionHandle Invalid = new TransactionHandle(0, INVALID_VERIFY, FLAG_TEMPORARY_REF, -1);

        internal readonly ulong handle; // 16
        internal ulong verify;          // 16
        internal uint flags;            // 8
        internal int index;             // 8
        
        internal TransactionHandle(ulong handle, ulong verify, uint flags, int index) {
            this.handle = handle;
            this.verify = (uint)verify;
            this.flags = flags;
            this.index = index;
        }

        internal void SetTemporaryRef() {
            flags |= FLAG_TEMPORARY_REF;
        }

        internal bool HasTemporaryRef() {
            return ((flags & FLAG_TEMPORARY_REF) != 0);
        }

        internal Byte OwnerCpu {
            get { return (byte)verify; }
        }

        internal bool IsAlive {
            get { return (verify != INVALID_VERIFY); }
        }

        internal bool IsReadOnly {
            get { return ((flags & sccoredb.MDB_TRANSCREATE_READ_ONLY) != 0); }
        }

        internal bool IsDirty {
            get {
                Int32 isDirty;
                uint ec;

                if (handle == 0)
                    return false;

                unsafe {
                    ec = sccoredb.Mdb_TransactionIsReadWrite(handle, verify, &isDirty);
                }

                if (ec == 0) return (isDirty != 0);

                // TODO:
                // Original implementation checked for ObjectDisposedException. See Transaction.ToException().
                throw ErrorCode.ToException(ec);
            }
        }

        public override int GetHashCode() {
            return handle.GetHashCode();
        }

        public override bool Equals(object obj) {
            return TransactionHandle.Equals(this, (TransactionHandle)obj);
        }

        public bool Equals(TransactionHandle handle) {
            return TransactionHandle.Equals(this, handle);
        }

        public static bool Equals(TransactionHandle t1, TransactionHandle t2) {
            return (t1.handle == t2.handle); // && t1.verify == t2.verify);
        }

        public static bool operator ==(TransactionHandle th1, TransactionHandle th2) {
            return TransactionHandle.Equals(th1, th2);
        }

        public static bool operator !=(TransactionHandle th1, TransactionHandle th2) {
            return !TransactionHandle.Equals(th1, th2);
        }
    }
}
