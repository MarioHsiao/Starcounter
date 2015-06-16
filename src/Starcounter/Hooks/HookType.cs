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
        internal const uint CommitInsert = sccoredb.CommitHookTypeInsert;

        /// <summary>
        /// Integer returned on commit when an UPDATE is detected
        /// on a kernel type that has the corresponding hook flag set.
        /// </summary>
        internal const uint CommitUpdate = sccoredb.CommitHookTypeUpdate;

        /// <summary>
        /// Integer returned on commit when a DELETE is detected
        /// on a kernel type that has the corresponding hook flag set.
        /// </summary>
        internal const uint CommitDelete = sccoredb.CommitHookTypeDelete;

        /// <summary>
        /// Represents a hook that trigger just before a delete is carried
        /// out.
        /// </summary>
        internal const uint BeforeDelete = 100;

        /// <summary>
        /// Returns true if the given type represents either an
        /// commt hook insert or an update; false otherwise.
        /// </summary>
        /// <param name="type">The type to evaluate.</param>
        /// <returns>True if insert or update; false otherwise.</returns>
        internal static bool IsCommitInsertOrUpdate(uint type) {
            return type == HookType.CommitInsert || type == HookType.CommitUpdate;
        }

        /// <summary>
        /// Returns true if the given type represents one of the
        /// commit hooks; false otherwise.
        /// </summary>
        /// <param name="type">The type to evaluate.</param>
        /// <returns>True the type represents a commit hook; false otherwise.</returns>
        internal static bool IsCommitHook(uint type) {
            return type == HookType.CommitInsert || type == HookType.CommitUpdate || type == HookType.CommitDelete;
        }
    }
}
