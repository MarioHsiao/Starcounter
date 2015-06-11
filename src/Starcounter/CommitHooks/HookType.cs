using Starcounter.Internal;
using System;

namespace Starcounter {
    /// <summary>
    /// Defines the type of hooks supported by Starcounter.
    /// </summary>
    internal static class HookType {
        /// <summary>
        /// Integer returned on commit when an INSERT is detected
        /// on a kernel type that has the corresponding hook flag set.
        /// </summary>
        internal const uint Insert = sccoredb.CommitHookTypeInsert;

        /// <summary>
        /// Integer returned on commit when an UPDATE is detected
        /// on a kernel type that has the corresponding hook flag set.
        /// </summary>
        internal const uint Update = sccoredb.CommitHookTypeUpdate;

        /// <summary>
        /// Integer returned on commit when a DELETE is detected
        /// on a kernel type that has the corresponding hook flag set.
        /// </summary>
        internal const uint Delete = sccoredb.CommitHookTypeDelete;

        /// <summary>
        /// Returns true if the given type represents either an
        /// insert or an update; false otherwise.
        /// </summary>
        /// <param name="type">The type to evaluate.</param>
        /// <returns>True if insert or update; false otherwise.</returns>
        internal static bool IsInsertOrUpdate(uint type) {
            return type == HookType.Insert || type == HookType.Update;
        }

        /// <summary>
        /// Gets the <see cref="HookConfiguration"/> corresponding to
        /// the given <see cref="HookType"/>.
        /// </summary>
        /// <param name="type">The configuration whose
        /// corresponding type of hook to return.</param>
        /// <returns>Hook configuration corresponding to the given type.</returns>
        internal static uint ToHookConfiguration(uint type) {
            uint configuration = 0;
            switch (type) {
                case HookType.Insert:
                    configuration = HookConfiguration.Insert;
                    break;
                case HookType.Update:
                    configuration = HookConfiguration.Update;
                    break;
                case HookType.Delete:
                    configuration = HookConfiguration.Delete;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        string.Format("Hook type {0} can not be mapped to a configuration", type));
            };
            return configuration;
        }
    }
}
