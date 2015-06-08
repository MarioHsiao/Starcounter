
using Starcounter.Internal;
using System;

namespace Starcounter {
    /// <summary>
    /// Defines constants and methods relating to configuration of
    /// hooks in the kernel.
    /// </summary>
    internal static class HookConfiguration {
        /// <summary>
        /// Internal token/flag representing inserts.
        /// </summary>
        internal const uint Insert = sccoredb.CommitHookConfigInsert;
        /// <summary>
        /// Internal token/flag representing updates.
        /// </summary>
        internal const uint Update = sccoredb.CommitHookConfigUpdate;
        /// <summary>
        /// Internal token/flag representing deletes.
        /// </summary>
        internal const uint Delete = sccoredb.CommitHookConfigDelete;

        /// <summary>
        /// Gets the <see cref="HookType"/> corresponding to the given
        /// <see cref="HookConfiguration"/>.
        /// </summary>
        /// <param name="configuration">The configuration whose
        /// corresponding type of hook to return.</param>
        /// <returns>Hook type corresponding to the given config.</returns>
        internal static uint ToHookType(uint configuration) {
            uint type = 0;
            switch (configuration) {
                case HookConfiguration.Insert:
                    type = HookType.Insert;
                    break;
                case HookConfiguration.Update:
                    type = HookType.Update;
                    break;
                case HookConfiguration.Delete:
                    type = HookType.Delete;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        string.Format("Hook configuration {0} can not be mapped to a type", configuration));
            };
            return type;
        }

        /// <summary>
        /// Returns a mask with all configuration installed for
        /// the type given a <see cref="HookKey"/>.
        /// </summary>
        /// <param name="key">Key whose type we should gather installed
        /// configuration for.</param>
        /// <returns>Mask with all hook configuration installed for the
        /// given type.
        /// </returns>
        internal static uint GetConfiguration(HookKey key) {
            uint result = 0;
            foreach (var installedKey in InvokableHook.HooksPerTrigger.Keys) {
                if (installedKey.TypeId == key.TypeId) {
                    result |= HookType.ToHookConfiguration(installedKey.TypeOfHook);
                }
            }
            return result;
        }
    }
}
