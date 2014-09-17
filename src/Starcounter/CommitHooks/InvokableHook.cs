
using System.Collections.Generic;

namespace Starcounter {
    /// <summary>
    /// Provides a way for Starcounter to list all hooks in a unified way
    /// and allows them to be mapped and invoked.
    /// </summary>
    internal abstract class InvokableHook {
        /// <summary>
        /// Internal token representing inserts.
        /// </summary>
        internal const int Insert = 1;
        /// <summary>
        /// Internal token representing updates.
        /// </summary>
        internal const int Update = 2;
        /// <summary>
        /// Internal token representing deletes.
        /// </summary>
        internal const int Delete = 3;

        /// <summary>
        /// Code host scoped dictonary mapping every triggering type
        /// and operation to a set of hooks that are to be invoked when
        /// the corresponding commit occurs.
        /// </summary>
        internal static Dictionary<string, List<InvokableHook>> HooksPerTrigger = new Dictionary<string, List<InvokableHook>>();

        /// <summary>
        /// Invokes every hook installed for a given key.
        /// </summary>
        /// <param name="key">The key whose hooks are to be invoked.</param>
        /// <param name="instance">Carry to each hook.</param>
        internal static void InvokeAllWithKey(string key, object instance) {
            List<InvokableHook> all;
            if (HooksPerTrigger.TryGetValue(key, out all)) {
                foreach (var hook in all) {
                    hook.Invoke(instance);
                }
            }
        }

        /// <summary>
        /// Invokes the current hook.
        /// </summary>
        /// <param name="triggeringObject">Reference to the instance
        /// representing the carry the hook receives.</param>
        public abstract void Invoke(object triggeringObject);
    }
}
