using Starcounter.Binding;
using System;

namespace Starcounter.Internal {

    internal static class DynamicTypesHelper {

        public static IObjectProxy RuntimeNew(int tableId) {
            // Proper error messages including new error codes.
            // Delayed until final implementation though (see
            // #2500 for more info).
            // TODO:
            if (IsValidInstantiatesValue(tableId)) throw new InvalidOperationException("This object is not a type.");
            var tb = Bindings.GetTypeBinding(tableId);
            ulong oid = 0, addr = 0;
            DbState.Insert(tb.TableId, ref oid, ref addr);
            return tb.NewInstance(addr, oid);
        }

        public static bool IsValidInstantiatesValue(int tableId) {
            // We currently treat 0 as invalid too, even though it should be
            // legal, at least in theory. Ultimately, we should use only the
            // invalid table ID token, but it means we need to assure its the
            // default for every instance created, where 0 is the default.
            return tableId != 0 && tableId != sccoredb.STAR_INVALID_TABLE_ID;
        }
    }
}
