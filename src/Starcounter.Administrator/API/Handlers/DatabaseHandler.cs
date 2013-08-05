using Starcounter.Advanced;

namespace Starcounter.Administrator.API.Handlers {
    /// <summary>
    /// Provides the handlers for the admin server database resource.
    /// </summary>
    internal static partial class DatabaseHandler {
        /// <summary>
        /// Install handlers for the resource represented by this class and
        /// performs custom setup.
        /// </summary>
        internal static void Setup() {
            var uri = RootHandler.API.Uris.Database;

            Handle.GET<string, Request>(uri, OnGET);
            Handle.DELETE<string, Request>(uri, OnDELETE);
            RootHandler.Register405OnAllUnsupported(uri, new string[] { "GET", "DELETE" });
        }
    }
}