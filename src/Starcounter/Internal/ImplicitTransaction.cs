using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Internal {
    /// <summary>
    /// Class representing the implicit created transaction that is used when the database is accessed 
    /// but no explicit transaction is set. This transaction is not directly accessible by users and 
    /// will be created if needed (<see cref="StarcounterInternal.Hosting.orange.orange_on_no_transaction"/>) 
    /// and cleaned up (<see cref="Starcounter.Internal.TaskHelper.Reset"/>) in the end of each task.
    /// </summary>
    internal class ImplicitTransaction {
        private const ulong INVALID_VERIFY = 0xFF;
        private const ulong INVALID_HANDLE = 0;

        private ulong _handle;
        private ulong _verify;

        internal ImplicitTransaction() {
            ulong handle;
            ulong verify;
            uint flags = sccoredb.MDB_TRANSCREATE_READ_ONLY;

            uint r = sccoredb.sccoredb_create_transaction(
                flags,
                out handle,
                out verify
                );

            if (r == 0) {
                _handle = handle;
                _verify = verify;
                return;
            }
            throw ErrorCode.ToException(r);
        }

        internal void SetCurrent() {
            uint r = sccoredb.sccoredb_set_current_transaction(0, _handle, _verify);
            if (r == 0) return;
            throw ErrorCode.ToException(r);
        }

        internal void Cleanup() {
            if (_verify == INVALID_VERIFY)
                return;

            uint r = sccoredb.sccoredb_free_transaction(_handle, _verify);
            if (r == 0) {
                _verify = INVALID_VERIFY;
                _handle = INVALID_HANDLE;
                return;
            }
            throw ErrorCode.ToException(r);
        }
    }
}
