
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
        internal const uint Insert = sccoredb.STAR_HOOKS_ON_COMMIT_INSERT;
        /// <summary>
        /// Internal token/flag representing updates.
        /// </summary>
        internal const uint Update = sccoredb.STAR_HOOKS_ON_COMMIT_UPDATE;
        /// <summary>
        /// Internal token/flag representing deletes.
        /// </summary>
        internal const uint Delete = sccoredb.STAR_HOOKS_ON_COMMIT_DELETE;

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
    }
}
