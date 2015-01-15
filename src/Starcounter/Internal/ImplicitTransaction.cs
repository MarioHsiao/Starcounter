using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Internal {
    /// <summary>
    /// Class wrapping an implicit transaction, that is a transaction that is created
    /// when no transaction is explicitly created by the user. It will always be released
    /// and cleaned up in the end of a task.
    /// </summary>
    internal struct ImplicitTransaction {
        [ThreadStatic]
        private static ImplicitTransaction instance;

        private ulong handle;
        private ulong verify;
        
        internal static void CreateOrSetCurrent() {
            if (Db.Environment.HasDatabase)
                instance.DoCreateOrSetCurrent();
        }

        internal static uint Release() {
            return instance.DoRelease();
        }

        private void DoCreateOrSetCurrent() {
            uint ec;

            if (this.handle == 0) {
                ulong handle;
                ulong verify;
                ec = sccoredb.star_create_transaction_and_set_current(sccoredb.MDB_TRANSCREATE_READ_ONLY, 0, out handle, out verify);
                if (ec == 0) {
                    this.handle = handle;
                    this.verify = verify;
                    return;
                }
            } else {
                ec = sccoredb.star_set_current_transaction(0, this.handle, this.verify);
                if (ec == 0)
                    return;
            }

            if (ec != Error.SCERRTRANSACTIONLOCKEDONTHREAD)
                throw ErrorCode.ToException(ec);
        }

        private uint DoRelease() {
            if (this.handle == 0)
                return 0;

            uint r = sccoredb.star_free_transaction(this.handle, this.verify);
            this.handle = 0;
            this.verify = 0xFF;
            return r;
        }
    }
}
