
using Starcounter.Advanced;

namespace Starcounter.Bootstrap.Management {
    /// <summary>
    /// Implements the code host functionality behind the code host "Executables"
    /// management resource.
    /// </summary>
    internal static class ExecutablesHandler {

        /// <summary>
        /// Performs setup of the <see cref="ExecutablesHandler"/>.
        /// </summary>
        internal static void Setup() {
            var uri = CodeHostAPI.Uris.Host;
            var port = ManagementService.Port;

            Handle.POST<Request>(port, uri, ExecutablesHandler.OnPOST);
        }

        static object OnPOST(Request request) {
            if (ManagementService.Unavailable) {
                return 503;
            }

            // Not implemented currently.
            return 501;
        }
    }
}