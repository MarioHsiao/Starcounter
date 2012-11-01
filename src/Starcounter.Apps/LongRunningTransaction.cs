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

        [ThreadStatic]
        private static LongRunningTransaction _current;

        /// <summary>
        /// 
        /// </summary>
        private LongRunningTransaction() {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static LongRunningTransaction NewCurrent() {
            uint ec;
            ulong handle;
            ulong verify;
            LongRunningTransaction transaction;

            ec = sccoredb.sccoredb_create_transaction_and_set_current(0, out handle, out verify);
            if (ec != 0)
                throw ErrorCode.ToException(ec);

            transaction = new LongRunningTransaction(handle, verify);
            _current = transaction;
            return transaction;
        }

        /// <summary>
        /// 
        /// </summary>
        public static LongRunningTransaction Current {
            get { return _current; }
        }

        /// <summary>
        /// 
        /// </summary>
        internal LongRunningTransaction(ulong handle, ulong verify) {
            _handle = handle;
            _verify = verify;
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
        internal void SetTransactionAsCurrent() {
            uint ec;

            if (_handle == 0)
                return;

            ec = sccoredb.sccoredb_set_current_transaction(0, _handle, _verify);
            if (ec != 0)
                throw ErrorCode.ToException(ec);

            _current = this;
        }

        /// <summary>
        /// 
        /// </summary>
        internal void ReleaseCurrentTransaction() {
            uint ec;

            _current = null;
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
                ReleaseCurrentTransaction();
                FreeTransaction();
            }
        }
    }
}
