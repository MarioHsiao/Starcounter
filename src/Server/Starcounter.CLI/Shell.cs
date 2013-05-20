using System;
using System.Diagnostics;

namespace Starcounter.CLI {
    /// <summary>
    /// Provides the CLI specifics used when an executable is
    /// being ran from the OS shell (e.g. by double-clicking the
    /// executable).
    /// </summary>
    public static class Shell {
        /// <summary>
        /// Boots the executable that caused the current process to
        /// launch in the Starcounter code host.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If this method is executed inside the code host process,
        /// it will silently return.
        /// </para>
        /// <para>
        /// This is the call that will be weaved, during compile-time,
        /// into executables that want to support shell bootstrapping.
        /// </para>
        /// </remarks>
        [DebuggerNonUserCode]
        public static void BootInHost() {
            throw new NotImplementedException();
        }
    }
}
