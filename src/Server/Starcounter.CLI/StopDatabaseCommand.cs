
using System;

namespace Starcounter.CLI {
    /// <summary>
    /// Provides functionality for a client to stop hosts and databases.
    /// </summary>
    public class StopDatabaseCommand : CLIClientCommand {
        /// <summary>
        /// Indicates stopping of the code host only. Other processes
        /// running as part of the target database will remain.
        /// </summary>
        public bool StopCodeHostOnly { get; set; }

        /// <inheritdoc/>
        protected override void Run() {
            throw new NotImplementedException();
        }
    }
}