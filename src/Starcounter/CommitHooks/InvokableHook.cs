
using Starcounter.Internal;
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
        internal const uint Insert = sccoredb.STAR_HOOKS_ON_COMMIT_INSERT;
        /// <summary>
        /// Internal token representing updates.
        /// </summary>
        internal const uint Update = sccoredb.STAR_HOOKS_ON_COMMIT_UPDATE;
        /// <summary>
        /// Internal token representing deletes.
        /// </summary>
        internal const uint Delete = sccoredb.STAR_HOOKS_ON_COMMIT_DELETE;

        /// <summary>
        /// Code host scoped dictonary mapping every triggering type
        /// and operation to a set of hooks that are to be invoked when
        /// the corresponding commit occurs.
        /// </summary>
        internal static Dictionary<HookKey, List<InvokableHook>> HooksPerTrigger;
        
        static InvokableHook() {
            HooksPerTrigger = new Dictionary<HookKey, List<InvokableHook>>(HookKey.EqualityComparer);
        }

        /// <summary>
        /// Invokes all hooks installed to watch when instances of 
        /// <paramref name="key"/> are inserted.
        /// </summary>
        /// <param name="key">The key to look for.</param>
        /// <param name="proxy">The carry to pass to the delegate.</param>
        internal static void InvokeInsert(HookKey key, IObjectView proxy) {
            InvokeAllWithKey(key, proxy);
        }

        /// <summary>
        /// Invokes all hooks installed to watch when instances of 
        /// <paramref name="key"/> are updated.
        /// </summary>
        /// <param name="key">The key to look for.</param>
        /// <param name="proxy">The carry to pass to the delegate.</param>
        internal static void InvokeUpdate(HookKey key, IObjectView proxy) {
            InvokeAllWithKey(key, proxy);
        }

        /// <summary>
        /// Invokes all hooks installed to watch when instances of 
        /// <paramref name="key"/> are deleted.
        /// </summary>
        /// <param name="key">The key to look for.</param>
        /// <param name="proxy">The carry in the form of an object ID
        /// to pass to the delegate.</param>
        internal static void InvokeDelete(HookKey key, ulong objectID) {
            InvokeAllWithKey(key, objectID);
        }

        /// <summary>
        /// Invokes every hook installed for a given key.
        /// </summary>
        /// <param name="key">The key whose hooks are to be invoked.</param>
        /// <param name="instance">Carry to each hook.</param>
        internal static void InvokeAllWithKey(HookKey key, object instance) {
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
