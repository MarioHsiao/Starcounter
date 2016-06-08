// ***********************************************************************
// <copyright file="Transaction.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Internal;
using System;
using Starcounter.Advanced;
using Starcounter.Binding;

namespace Starcounter {
    /// <summary>
    /// Represents a longrunning transaction.
    /// </summary>
    public class Transaction : Finalizing, ITransaction, IDisposable {
        private TransactionHandle _handle;
        private TransactionScrap _scrap;

        /// <summary>
        /// Current longrunning user transaction.
        /// </summary>
        /// <remarks>
        /// This instance will be used together with TransactionManager.current
        /// </remarks>
        [ThreadStatic]
        private static Transaction _current;

        public Transaction()
            : this(false) {
        }
        
        /// <summary>
        /// New transaction constructor.
        /// </summary>
        /// <param name="readOnly">Transaction read-only flag.</param>
        public Transaction(bool readOnly, bool applyHooks = true) {
            _handle = StarcounterBase.TransactionManager.Create(readOnly, applyHooks);
        }

        internal Transaction(TransactionHandle handle) {
            _handle = handle;
        }

        internal override void DestroyByFinalizer() {
            if (_handle.verify == TransactionHandle.INVALID_VERIFY) return;

            // Add transaction scrap to scrap heap, a job will be scheduled on
            // the owning thread to release the transaction (the finalizer
            // thread is not the owner of the transaction so it's not allowed
            // to).

            _handle.verify = TransactionHandle.INVALID_VERIFY;
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

            if (_handle == StarcounterBase.TransactionManager.CurrentTransaction) {
                TransactionManager.SetCurrentTransaction(TransactionHandle.Invalid);
                _current = null;
            }

            r = TransactionManager.DisposeNoException(_handle);
            if (r == 0) {
                _handle.verify = TransactionHandle.INVALID_VERIFY;
                UnLinkFinalizer();
                return;
            }

            if (r == Error.SCERRTRANSACTIONNOTOWNED && _handle.verify == TransactionHandle.INVALID_VERIFY) {
                return;
            }
            
            throw ErrorCode.ToException(r);
        }

        /// <summary>
        /// Gets a value indicating if the current transaction is a
        /// read-only transaction, i.e. a transaction where commits
        /// are not allowed.
        /// </summary>
        public bool IsReadOnly {
            get {
                return _handle.IsReadOnly;
            }
        }

        /// <summary>
        /// Returns current transaction if any.
        /// </summary>
        /// <returns></returns>
        public static Transaction Current {
            get {
                Transaction t = null;
                TransactionHandle h;

                h = StarcounterBase.TransactionManager.CurrentTransaction;
                if (h != TransactionHandle.Invalid) {
                    t = _current;
                    if (t == null) {
                        t = new Transaction(h);
                        StarcounterBase.TransactionManager.SetTemporaryRef(h);
                        _current = t;
                    } else if (t._handle != h) {
                        StarcounterBase.TransactionManager.SetTemporaryRef(h);
                        t = new Transaction(h);
                        _current = t;
                    }
                }
                return t;
            }
        }

        /// <summary>
        /// Adds <paramref name="obj"/> to the write list of the
        /// currently attached transaction.
        /// </summary>
        /// </exception>
        /// <param name="obj">The object to be added to the transaction
        /// write list. If the object is not an instance of a database
        /// class, an exception will be raised.</param>
        public static void Touch(object obj) {
            TransactionManager.Touch(obj);
        }

        /// <summary>
        /// Adds <paramref name="proxy"/> to the write list of the
        /// currently attached transaction.
        /// </summary>
        /// </exception>
        /// <param name="proxy">The proxy referencing the kernel
        /// object to be added to the transaction write list.</param>
        public static void Touch(IObjectProxy proxy) {
            TransactionManager.Touch(proxy);
        }

        //private static Exception ToException(Transaction transaction, uint r) {
        //    // If the error indicates that the object isn't owned we check if
        //    // verification is set to 0. If so the object has been disposed.

        //    if (
        //        r == Error.SCERRTRANSACTIONNOTOWNED &&
        //        transaction != null &&
        //        transaction._handle.verify == 0xFF //_INVALID_VERIFY
        //        ) {
        //        return new ObjectDisposedException(null);
        //    }

        //    return ErrorCode.ToException(r);
        //}

        //internal void Release() {
        //    //uint r;

        //    //// If another thread attempts to dispose the context at the same
        //    //// time as the current thread attempts to dispose it then it will
        //    //// fail since it doesn't own the object (assuming that this does). 
        //    //// Therefore it's thread safe regardless of if the verification
        //    //// used by the other thread is 0 or the original value.
        //    ////
        //    //// If disposed context is the current context then the current
        //    //// context is always set to null before the context is disposed.

        //    //if (_current == this) SetCurrent(null);
        //    //r = sccoredb.sccoredb_free_transaction(_handle, _verify);
        //    //if (r == 0) {
        //    //    _verify = _INVALID_VERIFY;
        //    //    return;
        //    //}
        //    //if (r == Error.SCERRTRANSACTIONNOTOWNED && _verify == _INVALID_VERIFY) {
        //    //    return;
        //    //}
        //    //throw ErrorCode.ToException(r);
        //}

        /// <summary>
        /// Executes some code within this transaction scope.
        /// </summary>
        /// <param name="action">Delegate that is called on transaction.</param>
        public void Scope(Action action) {
            Transaction old = _current;
            try {
                _current = this;
                StarcounterBase.TransactionManager.Scope(_handle, action);
            } finally {
                _current = old;
            }
        }

        public void Scope<T>(Action<T> action, T arg) {
            Transaction old = _current;
            try {
                _current = this;
                StarcounterBase.TransactionManager.Scope<T>(_handle, action, arg);
            } finally {
                _current = old;
            }
        }

        public void Scope<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg2) {
            Transaction old = _current;
            try {
                _current = this;
                StarcounterBase.TransactionManager.Scope<T1, T2>(_handle, action, arg1, arg2);
            } finally {
                _current = old;
            }
        }

        public void Scope<T1, T2, T3>(Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3) {
            Transaction old = _current;
            try {
                _current = this;
                StarcounterBase.TransactionManager.Scope<T1, T2, T3>(_handle, action, arg1, arg2, arg3);
            } finally {
                _current = old;
            }
        }

        public void Scope<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4) {
            Transaction old = _current;
            try {
                _current = this;
                StarcounterBase.TransactionManager.Scope<T1, T2, T3, T4>(_handle, action, arg1, arg2, arg3, arg4);
            } finally {
                _current = old;
            }
        }

        public TResult Scope<TResult>(Func<TResult> func) {
            Transaction old = _current;
            try {
                _current = this;
                return StarcounterBase.TransactionManager.Scope<TResult>(_handle, func);
            } finally {
                _current = old;
            }
        }

        public TResult Scope<T, TResult>(Func<T, TResult> func, T arg) {
            Transaction old = _current;
            try {
                _current = this;
                return StarcounterBase.TransactionManager.Scope<T, TResult>(_handle, func, arg);
            } finally {
                _current = old;
            }
        }

        public TResult Scope<T1, T2, TResult>(Func<T1, T2, TResult> func, T1 arg1, T2 arg2) {
            Transaction old = _current;
            try {
                _current = this;
                return StarcounterBase.TransactionManager.Scope<T1, T2, TResult>(_handle, func, arg1, arg2);
            } finally {
                _current = old;
            }
        }

        public TResult Scope<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> func, T1 arg1, T2 arg2, T3 arg3) {
            Transaction old = _current;
            try {
                _current = this;
                return StarcounterBase.TransactionManager.Scope<T1, T2, T3, TResult>(_handle, func, arg1, arg2, arg3);
            } finally {
                _current = old;
            }
        }

        public TResult Scope<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, TResult> func, T1 arg1, T2 arg2, T3 arg3, T4 arg4) {
            Transaction old = _current;
            try {
                _current = this;
                return StarcounterBase.TransactionManager.Scope<T1, T2, T3, T4, TResult>(_handle, func, arg1, arg2, arg3, arg4);
            } finally {
                _current = old;
            }
        }

        //void ITransaction.MergeTransaction(ITransaction toMerge) {
        //    Transaction trans = (Transaction)toMerge;
        //    TransactionManager.MergeTransaction(_handle, trans._handle);

        //    // The TransactionManager have already released the kerneltransaction.
        //    GC.SuppressFinalize(this);
        //    _handle.verify = TransactionHandle.INVALID_VERIFY;
        //}

        /// <summary>
        /// Commits changes made on transaction.
        /// </summary>
        public void Commit() {
            StarcounterBase.TransactionManager.Commit(_handle);
        }

        /// <summary>
        /// Rollbacks uncommitted changes on transaction.
        /// </summary>
        public void Rollback() {
            StarcounterBase.TransactionManager.Rollback(_handle);
        }

        /// <summary>
        /// Checks if there are any changes on transaction since last commit.
        /// </summary>
        public Boolean IsDirty {
            get {
                return StarcounterBase.TransactionManager.IsDirty(_handle);
            }
        }

        public void ClaimOwnership() {
            StarcounterBase.TransactionManager.ClaimOwnership(_handle);
            _handle.SetClaimed();
            _scrap = new TransactionScrap(_handle.handle, _handle.verify);
            CreateFinalizer();
        }
    }

    internal sealed class TransactionScrap {

        internal TransactionScrap Next;

        private readonly ulong _handle;
        private readonly ulong _verify;

        internal TransactionScrap(ulong handle, ulong verify) {
            _handle = handle;
            _verify = verify;
        }

        internal Byte OwnerCpu { get { return (byte)_verify; } }

        internal void Cleanup() {
            uint r = sccoredb.star_transaction_free(_handle, _verify);
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
            bool scheduleRecycle;

            cpuNumber = scrap.OwnerCpu;
            instance = _instances[cpuNumber];

            // Add the scrap to the scrap heap.
            //
            // If the scrap was the first bit of scrap on the heap we post a
            // request to cleanup the scrap on the heap.
            //
            // NOTE:
            // To be able to handle the the scheduler input queue is full we
            // _scheduleOnNextAdd to determin if to schedule a task instead of
            // if the scrap heap was empty. If the operation should fail we
            // reset this value and it will try again later. Currently we only
            // retry when more scrap is added so we could have a big leak on a
            // burst activity. Motivation for not doing a better solution is
            // that the problem with finit input queue should be solved in the
            // future so no point making a big effort on the workaround.

            lock (instance._syncRoot) {
                next = instance._firstScrap;
                scrap.Next = next;
                instance._firstScrap = scrap;
                //scheduleRecycle = (next == null);
                scheduleRecycle = instance._scheduleOnNextAdd;
                instance._scheduleOnNextAdd = false;
            }

            if (!scheduleRecycle) return;

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

            if (r == Error.SCERRINPUTQUEUEFULL) {
                lock (instance._syncRoot) {
                    instance._scheduleOnNextAdd = true;
                }
            }

            throw ErrorCode.ToException(r);
        }

        /// <summary>
        /// </summary>
        public static void RecycleScrap() {
            ThreadHelper.SetYieldBlock();
            try {
                var schedulerNumber = sccorelib.GetCpuNumber();
                var instance = _instances[schedulerNumber];
                instance.CleanupAll();
            } finally {
                ThreadHelper.ReleaseYieldBlock();
            }
        }

        private readonly object _syncRoot;
        private volatile TransactionScrap _firstScrap;

        /// <summary>
        /// This is here to provide a workaround to avoid unhandled exception
        /// should the scheduler input queue be full. See ThrowAway for
        /// details.
        /// </summary>
        private volatile bool _scheduleOnNextAdd;

        private ScrapHeap() {
            _syncRoot = new object();
            _scheduleOnNextAdd = true;
        }

        private void CleanupAll() {
            TransactionScrap scrap;

            lock (_syncRoot) {
                scrap = _firstScrap;
                _firstScrap = null;
                _scheduleOnNextAdd = true;
            }

            while (scrap != null) {
                scrap.Cleanup();
                scrap = scrap.Next;
            }
        }
    }
}
