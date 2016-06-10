using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Internal {
    internal interface ITransactionManager {
        TransactionHandle Create(bool readOnly, bool applyHooks = true);
        Starcounter.Advanced.ITransaction WrapHandle(TransactionHandle handle);
        void Commit(TransactionHandle handle);
        void Rollback(TransactionHandle handle);
        bool IsDirty(TransactionHandle handle);
        bool IsReadOnly(TransactionHandle handle);
        void Dispose(TransactionHandle handle);
        void SetTemporaryRef(TransactionHandle handle);
        bool HasTemporaryRef(TransactionHandle handle);
        void ClaimOwnership(TransactionHandle handle);
        TransactionHandle CurrentTransaction { get; set; }
        TransactionHandle CreateImplicitAndSetCurrent();
        void Scope(TransactionHandle handle, Action action);
        void Scope<T>(TransactionHandle handle, Action<T> action, T arg);
        void Scope<T1, T2>(TransactionHandle handle, Action<T1, T2> action, T1 arg1, T2 arg2);
        void Scope<T1, T2, T3>(TransactionHandle handle, Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3);
        void Scope<T1, T2, T3, T4>(TransactionHandle handle, Action<T1, T2, T3, T4> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4);
        TResult Scope<TResult>(TransactionHandle handle, Func<TResult> func);
        TResult Scope<T, TResult>(TransactionHandle handle, Func<T, TResult> func, T arg);
        TResult Scope<T1, T2, TResult>(TransactionHandle handle, Func<T1, T2, TResult> func, T1 arg1, T2 arg2);
        TResult Scope<T1, T2, T3, TResult>(TransactionHandle handle, Func<T1, T2, T3, TResult> func, T1 arg1, T2 arg2, T3 arg3);
        TResult Scope<T1, T2, T3, T4, TResult>(TransactionHandle handle, Func<T1, T2, T3, T4, TResult> func, T1 arg1, T2 arg2, T3 arg3, T4 arg4);
        TResult Scope<T1, T2, T3, T4, T5, TResult>(TransactionHandle handle, Func<T1, T2, T3, T4, T5, TResult> func, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
    }

    public struct TransactionHandle {
        public const byte INVALID_VERIFY = 0xFF;
        internal const uint FLAG_LONG_RUNNING = 0x0004;
        internal const uint FLAG_TRANSCREATE_READ_ONLY = 0x0008;
        internal const uint FLAG_IMPLICIT = 0x2000;
        private const uint FLAG_CLAIMED = 0x4000;
        private const uint FLAG_TEMPORARY_REF = 0x8000;
        

        internal static TransactionHandle Invalid = new TransactionHandle(0, INVALID_VERIFY, FLAG_TEMPORARY_REF | FLAG_CLAIMED | FLAG_IMPLICIT, -1, true);

        internal ulong handle;        // 8
        internal ulong verify;        // 8
        internal uint flags;          // 4
        internal int index;           // 4
        internal bool applyHooks;     // 4
                                      // 28
        internal TransactionHandle(ulong handle, ulong verify, uint flags, int index, bool applyHooks) {
            this.handle = handle;
            this.verify = (ushort)verify;
            this.flags = flags;
            this.index = index;
            this.applyHooks = applyHooks;
        }

        internal void SetTemporaryRef() {
            flags |= FLAG_TEMPORARY_REF;
        }

        internal bool HasTemporaryRef() {
            return ((flags & FLAG_TEMPORARY_REF) != 0);
        }

        internal void SetClaimed() {
            flags |= FLAG_CLAIMED | FLAG_TEMPORARY_REF;
        }

        internal bool HasTransferedOwnership() {
            return ((flags & FLAG_CLAIMED) != 0);
        }

        internal bool IsAlive {
            get { return (verify != INVALID_VERIFY); }
        }

        internal bool IsReadOnly {
            get { return ((flags & FLAG_TRANSCREATE_READ_ONLY) != 0); }
        }

        internal bool IsImplicit {
            get { return ((flags & FLAG_IMPLICIT) != 0); }
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
