
using Starcounter.Binding;
using Starcounter.Internal;
using System;

namespace Starcounter.Legacy {

    public static class LegacyRuntimeSupport {
        public static IObjectView New(int runtimeClassHandle) {
            if (runtimeClassHandle < 0 || runtimeClassHandle > ushort.MaxValue) {
                throw new ArgumentOutOfRangeException();
            }

            var binding = Bindings.GetTypeBinding(runtimeClassHandle);
            ulong oid = 0, addr = 0;
            DbState.Insert((ushort)runtimeClassHandle, ref oid, ref addr);
            return binding.NewInstance(addr, oid);
        }
    }
}
