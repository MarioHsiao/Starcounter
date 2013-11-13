
using Starcounter.Advanced;
using Starcounter.Bootstrap.Management;
using System.Threading;
using System.Threading.Tasks;

namespace Starcounter.Administrator.API.Handlers {

    internal static partial class ServerHandler {
        /// <summary>
        /// Handles a DELETE for this resource.
        /// </summary>
        /// <param name="request">The request being made.</param>
        /// <returns>The response to the request.</returns>
        static Response OnDELETE(Request request) {
            Task.Run(() => {
                Thread.Sleep(100);
                CodeHostAPI.Shutdown();
            });
            return 202;
        }
    }
}