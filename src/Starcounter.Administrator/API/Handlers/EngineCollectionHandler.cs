
using Starcounter.Advanced;
using Starcounter.Server.Rest;

namespace Starcounter.Administrator.API.Handlers {

    /// <summary>
    /// Excapsulates the admin server functionality acting on the
    /// engine collection resource. 
    /// </summary>
    internal static partial class EngineCollectionHandler {
        /// <summary>
        /// Install handlers for the resource represented by this class and
        /// performs custom setup.
        internal static void Setup() {
            var uri = RootHandler.API.Uris.Engines;
            
            Handle.POST<Request>(uri, OnPOST);
            Handle.POST<Request>(uri, OnGET);
            RootHandler.Register405OnAllUnsupported(uri, new string[] { "GET", "POST" });
        }
    }
}