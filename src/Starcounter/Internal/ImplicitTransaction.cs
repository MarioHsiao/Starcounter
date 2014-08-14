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
        private ulong readonlyHandle;
        private ulong readonlyVerify;

        private ulong lockedHandle;
        private ulong lockedVerify;

        internal bool explicitTransactionCreated;
        internal bool inImplicitScope;
        private bool lockedOnThread;

        internal bool IsCreated() {
            return (this.readonlyHandle != 0);
        }

        internal bool IsCreatedForScope() {
            return (this.lockedHandle != 0 && this.lockedOnThread == true);
        }

        //internal bool IsDirty() {
        //    int isDirty;
        //    uint r;

        //    unsafe {
        //        r = sccoredb.Mdb_TransactionIsReadWrite(handle, verify, &isDirty);
        //    }
        //    if (r == 0)
        //        return (isDirty != 0);
        //    throw ErrorCode.ToException(r);
        //}

        internal void CreateOrSetReadOnly() {
            uint ec;

            if (this.readonlyHandle == 0) {
                ulong handle;
                ulong verify;
                ec = sccoredb.sccoredb_create_transaction_and_set_current(sccoredb.MDB_TRANSCREATE_READ_ONLY, 0, out handle, out verify);
                if (ec == 0) {
                    this.readonlyHandle = handle;
                    this.readonlyVerify = verify;
                    return;
                }
            } else {
                ec = sccoredb.sccoredb_set_current_transaction(0, this.readonlyHandle, this.readonlyVerify);
                if (ec == 0)
                    return;
            }
            throw ErrorCode.ToException(ec);
        }

        private void CreateReadWriteLocked() {
            ulong handle;
            ulong verify;

            if (this.lockedHandle != 0) {
                // TODO: 
                // Clean up existing transaction or should this never happen?
                Debugger.Launch();
                throw new NotImplementedException("Clean up old implicit transaction");
            }

            uint r = sccoredb.sccoredb_create_transaction_and_set_current(0, 1, out handle, out verify);
            if (r == 0) {
                this.lockedHandle = handle;
                this.lockedVerify = verify;
                this.lockedOnThread = true;
                return;
            }
            throw ErrorCode.ToException(r);
        }

        internal void UpgradeToReadWrite() {
            if (this.explicitTransactionCreated)
                throw ErrorCode.ToException(Error.SCERRAMBIGUOUSIMPLICITTRANSACTION);
            CreateReadWriteLocked();
        }

        internal void Commit() {
            Starcounter.Transaction.Commit(1, 1);
            this.lockedHandle = 0;
            this.lockedVerify = 0xFF;
            this.lockedOnThread = false;
        }

        internal void ReleaseReadOnly() {
            if (this.readonlyHandle == 0)
                return;

            uint r = sccoredb.sccoredb_free_transaction(this.readonlyHandle, this.readonlyVerify);
            if (r == 0) {
                this.readonlyHandle = 0;
                this.readonlyVerify = 0xFF;
                return;
            }
            throw ErrorCode.ToException(r);
        }

        internal uint ReleaseLocked() {
            uint ec = sccoredb.sccoredb_set_current_transaction(1, 0, 0);
            if (ec == 0) {
                ec = sccoredb.sccoredb_free_transaction(this.lockedHandle, this.lockedVerify);
            }
            return ec;
        }
    }
}
