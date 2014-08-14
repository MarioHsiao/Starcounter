using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Internal {
    /// <summary>
    ///  There can only be one implicit transaction per scheduler, so this class is always 
    ///  instantiated once for every scheduler and reused.
    /// </summary>
    internal sealed class ImplicitTransaction {
        private ulong handle;
        private ulong verify;
        private bool isWritable;

        internal bool insideMicroTask;
        internal bool explicitTransactionCreated;
        
        internal bool IsCreated() {
            return (handle != 0);
        }

        internal bool IsWritable() {
            return isWritable;
        }

        internal void CreateOrSetReadOnly() {
            uint ec;

            if (this.handle == 0) {
                ulong handle;
                ulong verify;
                ec = sccoredb.sccoredb_create_transaction_and_set_current(sccoredb.MDB_TRANSCREATE_READ_ONLY, 0, out handle, out verify);
                if (ec == 0) {
                    this.handle = handle;
                    this.verify = verify;
                    this.isWritable = false;
                    return;
                }
            } else {
                // TODO:
                // writable should never be set here since then it will also be locked to the thread and used in scope.
                Debug.Assert(isWritable == false);
                
                ec = sccoredb.sccoredb_set_current_transaction(0, this.handle, this.verify);
                if (ec == 0)
                    return;
            }
            throw ErrorCode.ToException(ec);
        }

        private void CreateReadWriteLocked() {
            ulong handle;
            ulong verify;

            uint r = sccoredb.sccoredb_create_transaction_and_set_current(0, 1, out handle, out verify);
            if (r == 0) {
                this.handle = handle;
                this.verify = verify;
                this.isWritable = true;
                return;
            }
            throw ErrorCode.ToException(r);
        }

        internal void UpgradeToReadWrite() {
            if (this.explicitTransactionCreated)
                throw ErrorCode.ToException(Error.SCERRAMBIGUOUSIMPLICITTRANSACTION);
            ReleaseReadOnly();
            CreateReadWriteLocked();
        }

        internal void Commit() {
            Starcounter.Transaction.Commit(1, 1);
            this.handle = 0;
            this.verify = 0xFF;
            this.isWritable = false;
        }

        internal void ReleaseReadOnly() {
            // TODO:
            // throw exception instead if writable?
            Debug.Assert(isWritable == false);

            if (this.handle == 0 || this.isWritable)
                return;

            uint r = sccoredb.sccoredb_free_transaction(this.handle, this.verify);
            if (r == 0) {
                this.handle = 0;
                this.verify = 0xFF;
                return;
            }
            throw ErrorCode.ToException(r);
        }

        internal uint ReleaseLocked() {
            uint ec = sccoredb.sccoredb_set_current_transaction(1, 0, 0);
            if (ec == 0) {
                ec = sccoredb.sccoredb_free_transaction(this.handle, this.verify);
                if (ec == 0) {
                    this.handle = 0;
                    this.verify = 0;
                    this.isWritable = false;
                }
            }
            return ec;
        }
    }
}
