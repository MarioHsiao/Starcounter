
using Starcounter.Advanced;
using System;

namespace Starcounter.Administrator.API.Handlers {

    internal static partial class EngineCollectionHandler {

        /// <summary>
        /// Handles a POST to this resource.
        /// </summary>
        /// <param name="request">
        /// The REST request.</param>
        /// <returns>The response to be sent back to the client.</returns>
        static object OnPOST(Request request) {
            var engine = RootHandler.Host.Engine;
            var runtime = RootHandler.Host.Runtime;
            throw new NotImplementedException();

        }
    }
}