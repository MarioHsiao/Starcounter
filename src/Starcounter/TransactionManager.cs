using System;
using System.Collections.Generic;
using System.Diagnostics;
using Starcounter.Binding;
using Starcounter.Logging;

namespace Starcounter.Internal {
    /// <summary>
    /// Class that keeps track of created (kernel) transactions during a task. All transactions 
    /// that have not been explicitly claimed will be released in the end of the task, the others
    /// is up to the claimee to properly release them when finished.
    /// </summary>
    /// <remarks>
    /// 1) The transactions created using <c>Db.Transact</c> is not handled by this class. The transactions here
    /// are created using <c>Db.Scope</c> or <c>Transaction</c> object directly.
    /// 2) Since parts of this class needs to be exposed to other projects it needs to use an interface that is
    /// injected with this instance during startup. The implementation is threadsafe however. No state is kept on 
    /// the instance.
    /// </remarks>
    internal class TransactionManager : ITransactionManager {
        public const int ShortListCount = 5;

        [ThreadStatic]
        private unsafe static TransactionHandle* Refs;

        [ThreadStatic]
        private static int Used;

        [ThreadStatic]
        private static List<TransactionHandle> SlowList;

        [ThreadStatic]
        private static TransactionHandle CurrentHandle;

        internal unsafe static void Init(TransactionHandle* shortListPtr) {
            Refs = shortListPtr;
            Used = 0;
            CurrentHandle = TransactionHandle.Invalid;
        }

        /// <summary>
        /// Creates a new transaction in the kernel and adds a reference to the transactionlist and 
        /// returns a handle to the transaction.
        /// </summary>
        /// <param name="readOnly"></param>
        /// <param name="detectConflicts"></param>
        /// <returns></returns>
        public TransactionHandle Create(bool readOnly, bool detectConflicts) {
            int index = Used;

            ulong handle;
            ulong verify;
            uint ec;

            uint flags = detectConflicts ? 0 : TransactionHandle.FLAG_MERGING_WRITES;
            if (readOnly)
                flags |= TransactionHandle.FLAG_TRANSCREATE_READ_ONLY;

            ec = sccoredb.sccoredb_create_transaction(flags, out handle, out verify);
            if (ec == 0) {
                try {
                    TransactionHandle th = new TransactionHandle(handle, verify, flags, index);

                    if (index < ShortListCount) {
                        unsafe {
                            Refs[index] = th;
                        }
                    } else {
                        // The ShortList is filled. We need to switch over to slower list that can manage more transactions.
                        if (SlowList == null)
                            SlowList = new List<TransactionHandle>();

                        // The index will be recalculated based on the shortlist when used.
                        Debug.Assert(SlowList.Count == (index - ShortListCount));

                        SlowList.Add(th);
                    }

                    Used++;

                    return th;
                } catch {
                    sccoredb.sccoredb_free_transaction(handle, verify);
                    throw;
                }
            }

            throw ErrorCode.ToException(ec);
        }

        /// <summary>
        /// Wraps a transaction handle in a managed transaction object.
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        public Starcounter.Advanced.ITransaction WrapHandle(TransactionHandle handle) {
            return new Transaction(handle);
        }

        TransactionHandle ITransactionManager.CreateImplicitAndSetCurrent() {
            return TransactionManager.CreateImplicitAndSetCurrent(true);
        }

        internal static TransactionHandle CreateImplicitAndSetCurrent(bool readOnly) {
            return CreateAndSetCurrent(readOnly, false, true);
        }

        internal static TransactionHandle CreateAndSetCurrent(bool readOnly, bool detectConflicts) {
            return CreateAndSetCurrent(readOnly, detectConflicts, false);
        }

        private static TransactionHandle CreateAndSetCurrent(bool readOnly, bool detectConflicts, bool isImplicit) {
            int index = Used;

            ulong handle;
            ulong verify;
            uint ec;

            uint flags = detectConflicts ? 0 : TransactionHandle.FLAG_MERGING_WRITES;
            if (readOnly)
                flags |= TransactionHandle.FLAG_TRANSCREATE_READ_ONLY;

            ec = sccoredb.sccoredb_create_transaction_and_set_current(flags, 0, out handle, out verify);
            if (ec == 0) {
                try {
                    TransactionHandle th = new TransactionHandle(handle, verify, flags, index);
                    if (isImplicit)
                        th.flags |= TransactionHandle.FLAG_IMPLICIT;

                    if (index < ShortListCount) {
                        unsafe {
                            Refs[index] = th;
                        }
                    } else {
                        // The ShortList is filled. We need to switch over to slower list that can manage more transactions.
                        if (SlowList == null)
                            SlowList = new List<TransactionHandle>();

                        // The index will be recalculated based on the shortlist when used.
                        Debug.Assert(SlowList.Count == (index - ShortListCount));

                        SlowList.Add(th);
                    }

                    Used++;
                    CurrentHandle = th;

                    return th;
                } catch {
                    sccoredb.sccoredb_free_transaction(handle, verify);
                    throw;
                }
            }

            throw ErrorCode.ToException(ec);
        }

        /// <summary>
        /// Releases the kerneltransaction and removes the handle from the references.
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="index"></param>
        internal static uint DisposeNoException(TransactionHandle handle) {
            uint ec = 0;
            if (CurrentHandle == handle)
                SetCurrentTransaction(TransactionHandle.Invalid);

            if (handle.IsAlive) {
                ec  = sccoredb.sccoredb_free_transaction(handle.handle, handle.verify);
                if (ec != 0)
                    return ec;
            }

            if (handle.index >= 0) {
                TransactionHandle keptHandle;
                if (handle.index < ShortListCount) {
                    unsafe {
                        keptHandle = Refs[handle.index];
                        if (keptHandle == handle)
                            Refs[handle.index] = TransactionHandle.Invalid;
                    }
                } else {
                    int calcIndex = handle.index - ShortListCount;

                    keptHandle = SlowList[calcIndex];
                    if (keptHandle == handle)
                        SlowList[calcIndex] = TransactionHandle.Invalid;
                }

                // If the last one added is the one disposed we decrease the used count and allow the position to be reused.
                if (handle.index == (Used - 1))
                    Used--;
            }
            return 0;
        }

        public void Dispose(TransactionHandle handle) {
            uint ec = DisposeNoException(handle);
            if (ec == 0)
                return;
            throw ErrorCode.ToException(ec);
        }

        /// <summary>
        /// Checks if the specified transaction has any temporary references or is managed by another.
        /// If not it will be disposed.
        /// </summary>
        /// <param name="handle"></param>
        internal unsafe static void CheckForRefOrDisposeTransaction(TransactionHandle handle) {
            uint ec;
            int isDirty = 0;
            TransactionHandle keptHandle;

            // There is no need to check if transaction already have been disposed since if it have 
            // been temporary ref will be set anyways.

            if (handle.index < ShortListCount) {
                unsafe {
                    keptHandle = Refs[handle.index];
                    if (!keptHandle.HasTemporaryRef()) {
                        ec = sccoredb.Mdb_TransactionIsReadWrite(handle.handle, handle.verify, &isDirty);
                        if (ec == 0)
                            ec = sccoredb.sccoredb_free_transaction(handle.handle, handle.verify);

                        if (ec == 0) {
                            Refs[handle.index] = TransactionHandle.Invalid;

                            if (isDirty != 0)
                                throw ErrorCode.ToException(Error.SCERRTRANSACTIONMODIFIEDBUTNOTREFERENCED); 

                            // If the last one added is the one disposed we decrease the used count and allow the position to be reused.
                            if (handle.index == (Used - 1))
                                Used--;
                            return;
                        }
                        throw ErrorCode.ToException(ec);
                    }
                }
            } else {
                int calcIndex = handle.index - ShortListCount;
                keptHandle = SlowList[calcIndex];
                if (!keptHandle.HasTemporaryRef()) {
                    ec = sccoredb.Mdb_TransactionIsReadWrite(handle.handle, handle.verify, &isDirty);
                    if (ec == 0)
                        ec = sccoredb.sccoredb_free_transaction(handle.handle, handle.verify);

                    if (ec == 0) {
                        SlowList[calcIndex] = TransactionHandle.Invalid;

                        if (isDirty != 0)
                            throw ErrorCode.ToException(Error.SCERRTRANSACTIONMODIFIEDBUTNOTREFERENCED);

                        // If the last one added is the one disposed we decrease the used count and allow the position to be reused.
                        if (handle.index == (Used - 1))
                            Used--;
                        return;
                    }
                    throw ErrorCode.ToException(ec);
                }
            }
        }

        /// <summary>
        /// Marks the transaction with the specified index as temporary in use. This means
        /// the transaction cannot be cleaned up in the end of the scope, but will be cleaned
        /// up in the end of the request, unless the ownership of the transaction is claimed.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public void SetTemporaryRef(TransactionHandle handle) {
            if (handle.index == -1)
                return;
            
            if (handle.index < ShortListCount) {
                unsafe {
                    Refs[handle.index].SetTemporaryRef();
                }
            } else {
                var keptHandle = SlowList[handle.index - ShortListCount];
                keptHandle.SetTemporaryRef();
                SlowList[handle.index - ShortListCount] = keptHandle;
            }
        }

        public bool HasTemporaryRef(TransactionHandle handle) {
            if (handle.index == -1)
                return true;

            if (handle.index < ShortListCount) {
                unsafe {
                    return Refs[handle.index].HasTemporaryRef();
                }
            } else {
                return SlowList[handle.index - ShortListCount].HasTemporaryRef();
            }
        }

        public void ClaimOwnership(TransactionHandle handle) {
            if (handle.index == -1 ) {
                throw ErrorCode.ToException(Error.SCERRTRANSACTIONALREADYOWNED);
            }

            TransactionHandle keptHandle;
            
            if (handle.index < ShortListCount) {
                unsafe {        
                    keptHandle = Refs[handle.index];
                    if (keptHandle.HasTransferedOwnership())
                        throw ErrorCode.ToException(Error.SCERRTRANSACTIONALREADYOWNED);
                    keptHandle.SetClaimed();
                    Refs[handle.index] = keptHandle;
                }
            } else {
                int calcIndex = handle.index - ShortListCount;
                keptHandle = SlowList[calcIndex];
                if (keptHandle.HasTransferedOwnership())
                    throw ErrorCode.ToException(Error.SCERRTRANSACTIONALREADYOWNED);
                keptHandle.SetClaimed();
                SlowList[calcIndex] = keptHandle;
            }
            keptHandle.index = -1;

            // Update the current struct if it points to the same transaction.
            if (CurrentHandle == keptHandle)
                CurrentHandle = keptHandle;
        }

        public TransactionHandle CurrentTransaction {
            get { return CurrentHandle; }
            set { SetCurrentTransaction(value); }
        }

        internal static TransactionHandle GetCurrentAndSetToNoneManagedOnly() {
            var handle = CurrentHandle;
            CurrentHandle = TransactionHandle.Invalid;
            return handle;
        }

        internal static void SetCurrentTransaction(TransactionHandle handle) {
            if (CurrentHandle == handle)
                return;

            uint ec = sccoredb.sccoredb_set_current_transaction(0, handle.handle, handle.verify);
            if (ec == 0) {
                CurrentHandle = handle;
                return;
            }
            throw ErrorCode.ToException(ec);
        }

        internal TransactionHandle Get(int index) {
            if (index < 0)
                return TransactionHandle.Invalid;

            if (index < ShortListCount) {
                unsafe {
                    return Refs[index];
                }
            }
            return SlowList[index - ShortListCount];
        }

        internal static unsafe void Cleanup() {
            TransactionHandle th;
            uint ec;

            SetCurrentTransaction(TransactionHandle.Invalid);

            int used = Used;
            int fastCount = used;
            if (fastCount >= ShortListCount)
                fastCount = ShortListCount;

            for (int i = 0; i < fastCount; i++) {
                // All handles that have been created will be cleaned up.
                // If noone have taken ownership (i.e. the session/viewmodel) the kerneltransaction will be released.
                // If someone have taken ownership it is up to them to properly dispose the kerneltransaction.

                th = Refs[i];
                if (!th.HasTransferedOwnership()) {
                    ec = sccoredb.sccoredb_free_transaction(th.handle, th.verify);
                    if (ec == 0)
                        continue;
                    // TODO:
                    // How do we handle exception here? We need to clean all transactions before throwing anything.
                    throw ErrorCode.ToException(ec);
                }
            }

            if (used > ShortListCount){
                List<TransactionHandle> slowList = SlowList;
                for (int i = 0; i < slowList.Count; i++) {
                    th = slowList[i];
                    if (!th.HasTransferedOwnership()) {
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

            Refs = null;
            Used = 0;
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
        public void Scope(TransactionHandle handle, Action action) {
            var old = CurrentHandle;

            try {
                SetCurrentTransaction(handle);
                action();
            } finally {
                SetCurrentTransaction(old);
            }
        }

        public void Scope<T>(TransactionHandle handle, Action<T> action, T arg) {
            var old = CurrentHandle;

            try {
                SetCurrentTransaction(handle);
                action(arg);
            } finally {
                SetCurrentTransaction(old);
            }
        }

        public void Scope<T1, T2>(TransactionHandle handle, Action<T1, T2> action, T1 arg1, T2 arg2) {
            var old = CurrentHandle;

            try {
                SetCurrentTransaction(handle);
                action(arg1, arg2);
            } finally {
                SetCurrentTransaction(old);
            }
        }

        public void Scope<T1, T2, T3>(TransactionHandle handle, Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3) {
            var old = CurrentHandle;

            try {
                SetCurrentTransaction(handle);
                action(arg1, arg2, arg3);
            } finally {
                SetCurrentTransaction(old);
            }
        }

        public void Scope<T1, T2, T3, T4>(TransactionHandle handle, Action<T1, T2, T3, T4> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4) {
            var old = CurrentHandle;

            try {
                SetCurrentTransaction(handle);
                action(arg1, arg2, arg3, arg4);
            } finally {
                SetCurrentTransaction(old);
            }
        }

        public TResult Scope<TResult>(TransactionHandle handle, Func<TResult> func) {
            var old = CurrentHandle;

            try {
                SetCurrentTransaction(handle);
                return func();
            } finally {
                SetCurrentTransaction(old);
            }
        }

        public TResult Scope<T, TResult>(TransactionHandle handle, Func<T, TResult> func, T arg) {
            var old = CurrentHandle;

            try {
                SetCurrentTransaction(handle);
                return func(arg);
            } finally {
                SetCurrentTransaction(old);
            }
        }

        public TResult Scope<T1, T2, TResult>(TransactionHandle handle, Func<T1, T2, TResult> func, T1 arg1, T2 arg2) {
            var old = CurrentHandle;

            try {
                SetCurrentTransaction(handle);
                return func(arg1, arg2);
            } finally {
                SetCurrentTransaction(old);
            }
        }

        public TResult Scope<T1, T2, T3, TResult>(TransactionHandle handle, Func<T1, T2, T3, TResult> func, T1 arg1, T2 arg2, T3 arg3) {
            var old = CurrentHandle;

            try {
                SetCurrentTransaction(handle);
                return func(arg1, arg2, arg3);
            } finally {
                SetCurrentTransaction(old);
            }
        }

        public TResult Scope<T1, T2, T3, T4, TResult>(TransactionHandle handle, Func<T1, T2, T3, T4, TResult> func, T1 arg1, T2 arg2, T3 arg3, T4 arg4) {
            var old = CurrentHandle;

            try {
                SetCurrentTransaction(handle);
                return func(arg1, arg2, arg3, arg4);
            } finally {
                SetCurrentTransaction(old);
            }
        }

        public TResult Scope<T1, T2, T3, T4, T5, TResult>(TransactionHandle handle, Func<T1, T2, T3, T4, T5, TResult> func, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) {
            var old = CurrentHandle;

            try {
                SetCurrentTransaction(handle);
                return func(arg1, arg2, arg3, arg4, arg5);
            } finally {
                SetCurrentTransaction(old);
            }
        }

        public TResult Scope<T1, T2, T3, T4, T5, T6, TResult>(TransactionHandle handle, Func<T1, T2, T3, T4, T5, T6, TResult> func, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) {
            var old = CurrentHandle;

            try {
                SetCurrentTransaction(handle);
                return func(arg1, arg2, arg3, arg4, arg5, arg6);
            } finally {
                SetCurrentTransaction(old);
            }
        }

        //public void MergeTransaction(TransactionHandle mainHandle, TransactionHandle toMergeHandle) {
        //    var old = currentHandle;
        //    uint ec;

        //    try {
        //        SetCurrentTransaction(mainHandle);
        //        ec = sccoredb.star_transaction_merge_into_current(toMergeHandle.handle, toMergeHandle.verify);
        //        if (ec == 0) {
        //            // TODO: 
        //            // Do we need to dispose the merged transaction or is that done when merging?
        //            Dispose(toMergeHandle);
        //            return;
        //        }

        //        throw ErrorCode.ToException(ec);
        //    } finally {
        //        SetCurrentTransaction(old);
        //    }
        //}

        public bool IsDirty(TransactionHandle handle) {
            Int32 isDirty;
            uint ec;

            if (handle.handle == 0)
                return false;

            unsafe {
                ec = sccoredb.Mdb_TransactionIsReadWrite(handle.handle, handle.verify, &isDirty);
            }

            if (ec == 0) return (isDirty != 0);

            throw ErrorCode.ToException(ec);
        }

        public bool IsReadOnly(TransactionHandle handle) {
            return handle.IsReadOnly;
        }

        public void Commit(TransactionHandle handle) {
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

                    if (hiter != 0) {
                        ulong oid;
                        ulong address;
                        ushort tableId;
                        ulong hookType;
                        TypeBinding binding;
                        IObjectProxy proxy;
                        HookKey key;

                        try {

                            binding = null;
                            proxy = null;
                            key = null;

                            while (true) {
                                unsafe {
                                    r = sccoredb.SCIteratorNext(hiter, viter, &oid, &address, &tableId, &hookType);
                                }
                                if (r != 0) throw ErrorCode.ToException(r);
                                if (oid == 0) break;

                                if (HookType.IsCommitInsertOrUpdate((uint)hookType)) {
                                    if (binding == null || binding.TableId != tableId) {
                                        binding = TypeRepository.GetTypeBinding(tableId);
                                        proxy = binding.NewInstanceUninit();
                                    }

                                    proxy.Bind(address, oid, binding);
                                }

                                key = HookKey.FromTable(tableId, (uint)hookType, key);
                                try {
                                    switch (hookType) {
                                        case HookType.CommitInsert:
                                            InvokableHook.InvokeInsert(key, proxy);
                                            break;
                                        case HookType.CommitUpdate:
                                            InvokableHook.InvokeUpdate(key, proxy);
                                            break;
                                        case HookType.CommitDelete:
                                            InvokableHook.InvokeDelete(key, oid);
                                            break;
                                    }

                                } catch {
                                    sccoredb.sccoredb_abort_commit(tran_locked_on_thread);
                                    throw;
                                }
                            }

                        } finally {
                            sccoredb.SCIteratorFree(hiter, viter);
                        }
                    }

                    if (r == 0) {
                        r = sccoredb.sccoredb_complete_commit(
                                tran_locked_on_thread, detach_and_free
                                );
                    }
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
        public void Rollback(TransactionHandle handle) {
            Scope(handle, () => {
                uint ec = sccoredb.sccoredb_rollback();
                if (ec == 0) return;

                throw ErrorCode.ToException(ec);
            });
        }
    }
}
