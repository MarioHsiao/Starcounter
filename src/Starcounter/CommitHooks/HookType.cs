using Starcounter.Internal;

namespace Starcounter {
    /// <summary>
    /// Defines the type of hooks supported by Starcounter.
    /// </summary>
    internal static class HookType {
        /// <summary>
        /// Integer returned on commit when an INSERT is detected
        /// on a kernel type that has the corresponding hook flag set.
        /// </summary>
        internal const uint Insert = sccoredb.STAR_HOOKTYPE_COMMIT_INSERT;

        /// <summary>
        /// Integer returned on commit when an UPDATE is detected
        /// on a kernel type that has the corresponding hook flag set.
        /// </summary>
        internal const uint Update = sccoredb.STAR_HOOKTYPE_COMMIT_UPDATE;

        /// <summary>
        /// Integer returned on commit when a DELETE is detected
        /// on a kernel type that has the corresponding hook flag set.
        /// </summary>
        internal const uint Delete = sccoredb.STAR_HOOKTYPE_COMMIT_DELETE;

        /// <summary>
        /// Returns true if the given type represents either an
        /// insert or an update; false otherwise.
        /// </summary>
        /// <param name="type">The type to evaluate.</param>
        /// <returns>True if insert or update; false otherwise.</returns>
        internal static bool IsInsertOrUpdate(uint type) {
            return type == HookType.Insert || type == HookType.Update;
        }
    }
}
