using Starcounter.Binding;
using System;

namespace Starcounter.Internal {

    internal static class DynamicTypesHelper {

        public static IObjectProxy RuntimeNew(int tableId) {
            // Proper error messages including new error codes.
            // Delayed until final implementation though (see
            // #2500 for more info).
            // TODO:
            if (tableId == sccoredb.STAR_INVALID_TABLE_ID) throw new InvalidOperationException("This object is not a type.");
            var tb = Bindings.GetTypeBinding(tableId);
            ulong oid = 0, addr = 0;
            DbState.Insert(tb.TableId, ref oid, ref addr);
            return tb.NewInstance(addr, oid);
        }
    }
}
