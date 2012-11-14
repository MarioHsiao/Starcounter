using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter.Internal;

namespace Starcounter {
    /// <summary>
    /// 
    /// </summary>
    public class LongRunningTransaction {
        private ulong _handle;
        private ulong _verify;

        // TODO:
        // This should be kept in the active Session and not as a 
        // threadstatic value here. 
        // Needed to avoid excessive switching current transaction
        // in kernel.
        [ThreadStatic]
        private static LongRunningTransaction _cachedActiveTransaction;

        internal static void ReleaseCached() {
            if (_cachedActiveTransaction != null) {
                _cachedActiveTransaction.Release();
                _cachedActiveTransaction = null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public LongRunningTransaction() {
            uint ec;
            ulong handle;
            ulong verify;
            
            ec = sccoredb.sccoredb_create_transaction_and_set_current(0, out handle, out verify);
            if (ec != 0)
                throw ErrorCode.ToException(ec);

            _handle = handle;
            _verify = verify;
            LongRunningTransaction._cachedActiveTransaction = this;
        }

        /// <summary>
        /// 
        /// </summary>
        internal void FreeTransaction() {
            uint ec;

            if (_handle == 0)
                return;

            ec = sccoredb.sccoredb_free_transaction(_handle, _verify);
        }

        /// <summary>
        /// 
        /// </summary>
        internal void Activate() {
            uint ec;
            LongRunningTransaction active = _cachedActiveTransaction;

            if (active != null){
                if (active._handle == _handle
                    && active._verify == _verify) {
                    return;
                }
                active.Release();
                _cachedActiveTransaction = null;
            }

            ec = sccoredb.sccoredb_set_current_transaction(0, _handle, _verify);
            if (ec != 0)
                throw ErrorCode.ToException(ec);
            _cachedActiveTransaction = this;
        }

        /// <summary>
        /// 
        /// </summary>
        internal void Release() {
            uint ec;
            ec = sccoredb.sccoredb_set_current_transaction(0, 0, 0);
            if (ec != 0)
                throw ErrorCode.ToException(ec);
        }

        /// <summary>
        ///
        /// </summary>
        internal void Commit() {
            if (_handle != 0)
                Transaction.Commit(0, 0);
        }

        /// <summary>
        /// 
        /// </summary>
        internal void Abort() {
            if (_handle != 0) {
                Release();
                FreeTransaction();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="action"></param>
        public void Add(Action action) {
            Activate();
            action();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public Entity Add(Func<Entity> func) {
            Activate();
            return func();
        }
    }
}
