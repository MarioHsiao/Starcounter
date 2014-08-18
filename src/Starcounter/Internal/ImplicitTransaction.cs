﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Internal {
    /// <summary>
    /// Class wrapping an implicit transaction, that is a transaction that is created
    /// when no transaction is explicitly created by the user. This transaction can be both
    /// readonly and readwrite depending by what is needed. Used together with Db.MicroTask(...)
    /// and created with callback from kernel.
    /// </summary>
    internal sealed class ImplicitTransaction {
        [ThreadStatic]
        private static ImplicitTransaction current;

        private ulong handle;
        private ulong verify;
        private bool isWritable;

        internal bool insideMicroTask;
        internal bool explicitTransactionCreated;

        internal static ImplicitTransaction Current(bool createIfNull) {
            if (current == null && createIfNull)
                current = new ImplicitTransaction();
            return current;
        }

        // TODO:
        // We just check if it is created sometime during this task here. Is there some way to get
        // the current transaction from kernel?
        internal bool IsCurrent() {
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