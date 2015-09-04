
using Starcounter.Binding;
using Starcounter.Internal;
using System;

namespace Starcounter.Legacy {

    public static class LegacyRuntimeSupport {

        /// <summary>
        /// Watch out: this is legacy support, intended to be used by those
        /// that know how. Focus is performance, no validations occur! Passing
        /// a bad argument has undefined behaviour.
        /// </summary>
        /// <param name="runtimeClassHandle">Runtime handle retreived from legacy layer
        /// </param>
        /// <returns>New instance</returns>
        public static IObjectView New(int runtimeClassHandle) {
            var binding = Bindings.GetTypeBinding(runtimeClassHandle);
            return NewInstance(binding);
        }

        /// <summary>
        /// Watch out: this is legacy support, intended to be used by those
        /// that know how. Focus is performance, no validations occur. Passing
        /// a bad argument has undefined behaviour.
        /// </summary>
        /// <param name="runtimeProxyTemplate">Template proxy</param>
        /// <returns>New instance</returns>
        public static IObjectView New(object runtimeProxyTemplate) {
            var proxy = (IObjectView)runtimeProxyTemplate;
            var binding = (TypeBinding)proxy.TypeBinding;
            return NewInstance(binding);
        }

        static IObjectView NewInstance(TypeBinding binding) {
            ulong oid = 0, addr = 0;
            DbState.Insert((ushort)binding.TableId, ref oid, ref addr);
            return binding.NewInstance(addr, oid);
        }
    }
}
