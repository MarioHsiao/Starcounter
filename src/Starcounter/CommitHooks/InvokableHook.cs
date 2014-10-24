
using Starcounter.Internal;
using System.Collections.Generic;

namespace Starcounter {
    /// <summary>
    /// Provides a way for Starcounter to list all hooks in a unified way
    /// and allows them to be mapped and invoked.
    /// </summary>
    internal abstract class InvokableHook {
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
        /// Returns a mask with all operations / hook types installed for
        /// the type given a <see cref="HookKey"/>.
        /// </summary>
        /// <param name="key">Key whose type we should gather installed
        /// operations for.</param>
        /// <returns>Mask with all hook types installed for the given type.
        /// </returns>
        internal static uint GetInstalledOperations(HookKey key) {
            uint result = 0;
            foreach (var installedKey in HooksPerTrigger.Keys) {
                if (installedKey.TypeId == key.TypeId) {
                    result |= installedKey.TypeOfHook;
                }
            }
            return result;
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
