
using System;

namespace Starcounter.Bootstrap.Management {
    /// <summary>
    /// Governs the interface the code host process expose over HTTP
    /// to allow code host level management.
    /// </summary>
    /// <remarks>
    /// Managing the code host is primarly done by the admin server as
    /// part of providing a way to run executables and start and stop
    /// engines and code host processes.
    /// </remarks>
    static internal class ManagementService {
        /// <summary>
        /// Initializes the management service.
        /// </summary>
        /// <param name="port"></param>
        /// <param name="databaseName"></param>
        internal static void Setup(ushort port, string databaseName) {
            // Setup all handlers we need.
            // TODO:
            throw new NotImplementedException();
        }
    }
}
