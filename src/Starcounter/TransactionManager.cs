using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Threading.Tasks;
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

        private static ConcurrentDictionary<long, TaskCompletionSource<uint>> completions_ = new ConcurrentDictionary<long, TaskCompletionSource<uint>>();
        private static long max_completion_cookie;

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
        /// <returns></returns>
        public TransactionHandle Create(bool readOnly, bool applyHooks) {
            int index = Used;

            ulong handle;
            ulong verify;
            uint ec;

            uint flags = TransactionHandle.FLAG_LONG_RUNNING;
            if (readOnly)
                flags |= TransactionHandle.FLAG_TRANSCREATE_READ_ONLY;

            ec = sccoredb.star_create_transaction(flags, out handle);
            if (ec == 0) {
                verify = ThreadData.ObjectVerify;
                try {
                    TransactionHandle th = new TransactionHandle(handle, verify, flags, index, applyHooks);

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
                    sccoredb.star_transaction_free(handle, verify);
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
            return CreateAndSetCurrent(readOnly, true, true);
        }

        internal static TransactionHandle CreateAndSetCurrent(bool readOnly) {
            return CreateAndSetCurrent(readOnly, false, true);
        }

        private static TransactionHandle CreateAndSetCurrent(bool readOnly, bool isImplicit, bool applyHooks) {
            if (ThreadData.inTransactionScope_ != 0)
                throw ErrorCode.ToException(Error.SCERRTRANSACTIONLOCKEDONTHREAD);

            int index = Used;

            ulong handle;
            ulong verify;
            uint ec;

            uint flags = TransactionHandle.FLAG_LONG_RUNNING;
            if (readOnly)
                flags |= TransactionHandle.FLAG_TRANSCREATE_READ_ONLY;

            ec = sccoredb.star_create_transaction(flags, out handle);
            if (ec == 0) {
                verify = ThreadData.ObjectVerify;
                try {
                    // Can not fail (only fails if transaction is already bound to context).
                    sccoredb.star_context_set_transaction(ThreadData.ContextHandle, handle);

                    TransactionHandle th = new TransactionHandle(handle, verify, flags, index, applyHooks);
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
                    sccoredb.star_transaction_free(handle, verify);
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
                ec = sccoredb.star_transaction_free(handle.handle, handle.verify);
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

                    // If the last one added is the one disposed we decrease the used count and allow the position to be reused.
                    if (handle.index == (Used - 1))
                        Used--;
                } else {
                    int calcIndex = handle.index - ShortListCount;

                    keptHandle = SlowList[calcIndex];
                    if (keptHandle == handle)
                        SlowList[calcIndex] = TransactionHandle.Invalid;
                }
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
                        ec = sccoredb.star_transaction_is_dirty(handle.handle, out isDirty, handle.verify);
                        if (ec == 0)
                            ec = sccoredb.star_transaction_free(handle.handle, handle.verify);

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
                    ec = sccoredb.star_transaction_is_dirty(handle.handle, out isDirty, handle.verify);
                    if (ec == 0)
                        ec = sccoredb.star_transaction_free(handle.handle, handle.verify);

                    if (ec == 0) {
                        SlowList[calcIndex] = TransactionHandle.Invalid;

                        if (isDirty != 0)
                            throw ErrorCode.ToException(Error.SCERRTRANSACTIONMODIFIEDBUTNOTREFERENCED);

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

            if (ThreadData.inTransactionScope_ == 0) {
                sccoredb.star_context_set_transaction( // Can not fail.
                    ThreadData.ContextHandle, handle.handle
                    );
                CurrentHandle = handle;
                ThreadData.applyHooks_ = handle.applyHooks;
                return;
            }

            throw ErrorCode.ToException(Error.SCERRTRANSACTIONLOCKEDONTHREAD);
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
                    ec = sccoredb.star_transaction_free(th.handle, th.verify);
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
                        ec = ec = sccoredb.star_transaction_free(th.handle, th.verify);
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
            var ir = sccoredb.star_context_set_trans_flags(
                ThreadData.ContextHandle, proxy.ThisHandle, proxy.Identity, 0
                );
            if (ir < 0) throw ErrorCode.ToException((uint)(-ir));
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

            ec = sccoredb.star_transaction_is_dirty(handle.handle, out isDirty, handle.verify);

            if (ec == 0) return (isDirty != 0);

            throw ErrorCode.ToException(ec);
        }

        public bool IsReadOnly(TransactionHandle handle) {
            return handle.IsReadOnly;
        }

        public void Commit(TransactionHandle handle) {
            Scope(handle, () => {
                Commit(handle.handle, 0).Wait();

                // Note that transaction is no longer current. We need to restore the scope but
                // again setting it as the current transaction on context.

                var contextHandle = ThreadData.ContextHandle; // Makes sure thread is attached.
                sccoredb.star_context_set_transaction(  // Can not fail.
                    contextHandle, handle.handle
                    );
                sccoredb.star_transaction_reset(handle.handle); // Can not fail.
            });
        }

        private static bool is_write_transaction_ignore_error(ulong handle)
        {
            int dirty_transaction;
            if (sccoredb.star_transaction_is_dirty(handle, out dirty_transaction) != 0)
                dirty_transaction = 1;

            return dirty_transaction != 0;
        }

        /// <summary>
        /// Commits current transaction.
        /// </summary>
        internal static System.Threading.Tasks.Task Commit(ulong handle, int free) {

            bool write_transaction = is_write_transaction_ignore_error(handle);

            TaskCompletionSource<uint> tcs = null;
            long current_cookie = 0;

            if (write_transaction)
            {
                if (ThreadData.applyHooks_)
                    InvokeHooks();

                current_cookie = System.Threading.Interlocked.Increment(ref max_completion_cookie);
                tcs = new TaskCompletionSource<uint>();
                completions_[current_cookie] = tcs;
            }

            uint r = sccoredb.star_context_commit_async(ThreadData.ContextHandle, free, 1, (ulong)current_cookie);

            if ((r != Error.SCERROPERATIONPENDING) && (write_transaction))
            {
                TaskCompletionSource<uint> dummy;
                completions_.TryRemove(current_cookie, out dummy);
            }

            if ((r != 0) && (r != Error.SCERROPERATIONPENDING)) throw ErrorCode.ToException(r);

            if ((r == 0) || (!write_transaction)) //read transactions are expected to be synchronous
                return System.Threading.Tasks.Task.FromResult(0);

            return tcs.Task.ContinueWith(
                (t) =>
                {
                    if (t.Result != 0)
                        throw ErrorCode.ToException(r);
                    else
                        return t.Result;
                }
            );
        }

        internal static void CompleteCommit(long cookie, uint result)
        {
            TaskCompletionSource<uint> tcs;
            completions_.TryRemove(cookie, out tcs);
            tcs.SetResult(result);
        }

        internal static void InvokeHooks()
        {
            uint r;
            ulong vi, hi;

            // Lock transaction on thread while invoking hook callbacks.
            ThreadData.inTransactionScope_++;
            try
            {
                vi = ThreadData.ObjectVerify;
                unsafe
                {
                    r = sccoredb.star_context_create_update_iterator(ThreadData.ContextHandle, &hi);
                }
                if (r != 0) throw ErrorCode.ToException(r);
                try
                {
                    TypeBinding binding = null;
                    IObjectProxy proxy = null;
                    HookKey key = null;

                    for (;;)
                    {
                        sccoredb.STAR_REFERENCE_VALUE rv;
                        ulong recordId, recordRef;
                        unsafe
                        {
                            r = sccoredb.star_iterator_next(hi, &rv, vi);
                        }
                        if (r != 0) throw ErrorCode.ToException(r);
                        recordId = rv.handle.id;
                        recordRef = DbHelper.EncodeObjectRef(rv.handle.opt, rv.layout_handle);
                        if (recordId == 0) break;
                        int s = sccoredb.star_context_get_trans_state(
                            ThreadData.ContextHandle, recordId, recordRef
                            );
                        if (s < 0) throw ErrorCode.ToException((uint)(-s));
                        uint hookType = (uint)s;

                        ushort layoutHandle = (ushort)(recordRef & 0xFFFF);

                        if (HookType.IsCommitInsertOrUpdate(hookType))
                        {
                            if (binding == null || binding.TableId != layoutHandle)
                            {
                                binding = TypeRepository.GetTypeBinding(layoutHandle);

                                // Handle if actual layout is different from expected layout.
                                if (binding.TableId != layoutHandle)
                                {
                                    layoutHandle = binding.TableId;
                                    recordRef = DbHelper.EncodeObjectRefWithLayoutHandle(
                                        recordRef, layoutHandle
                                        );
                                }

                                proxy = binding.NewInstanceUninit();
                            }

                            proxy.Bind(recordRef, recordId, binding);
                        }

                        key = HookKey.FromTable(layoutHandle, hookType, key);
                        switch (hookType)
                        {
                            case HookType.CommitInsert:
                                InvokableHook.InvokeInsert(key, proxy);
                                break;
                            case HookType.CommitUpdate:
                                InvokableHook.InvokeUpdate(key, proxy);
                                break;
                            case HookType.CommitDelete:
                                InvokableHook.InvokeDelete(key, recordId);
                                break;
                        }
                    }
                }
                finally
                {
                    sccoredb.star_iterator_free(hi, vi);
                }
            }
            finally
            {
                Debug.Assert(ThreadData.inTransactionScope_ > 0);
                ThreadData.inTransactionScope_--;
            }
        }

        /// <summary>
        /// Rollbacks uncommitted changes on transaction.
        /// </summary>
        public void Rollback(TransactionHandle handle) {
            Scope(handle, () => {
                var contextHandle = ThreadData.ContextHandle; // Make sure thread is attached.
                sccoredb.star_transaction_reset(handle.handle); // Can not fail.
            });
        }
    }
}
