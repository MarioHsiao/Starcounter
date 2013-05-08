
namespace Starcounter.Bootstrap.Management {
    /// <summary>
    /// Implements the code host functionality behind the code host "Host"
    /// management resource.
    /// </summary>
    internal static class CodeHostHandler {

        /// <summary>
        /// Performs setup of the <see cref="CodeHostHandler"/>.
        /// </summary>
        internal static void Setup() {
            var uri = CodeHostAPI.Uris.Host;
            var port = ManagementService.Port;

            Handle.GET(port, uri, CodeHostHandler.OnGET);
            Handle.DELETE(port, uri, CodeHostHandler.OnDELETE);
        }

        static object OnGET() {
            if (ManagementService.Unavailable) {
                return 503;
            }
            return 204;
        }

        static object OnDELETE() {
            if (ManagementService.Unavailable) {
                return 503;
            }
            ManagementService.Shutdown();
            return 204;
        }
    }
}