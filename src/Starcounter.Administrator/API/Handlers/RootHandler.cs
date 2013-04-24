
using Starcounter.Administrator.API.Utilities;
using Starcounter.Server;
using Starcounter.Server.PublicModel;
using Starcounter.Server.Rest;

namespace Starcounter.Administrator.API.Handlers {
    /// <summary>
    /// Provides handlers for REST calls targeting the admin server API
    /// root resource (i.e. the principal API REST entrypoint).
    /// </summary>
    internal static partial class RootHandler {
        /// <summary>
        /// Provides all handler classes with a single URI set and makes
        /// sure it's only accessible from one place.
        /// </summary>
        /// <remarks>
        /// This way, we can start the admin server with a dependency
        /// injection pattern with what URI set to use for the API, using
        /// different sets in tests (like test doubles) and experiment
        /// with a new set for a newer version of the API if we like.
        /// </remarks>
        public static AdminAPI API { get; private set; }

        /// <summary>
        /// Provides a set of references to the currently running
        /// admin server host. Used and shared by handlers when fullfilling
        /// REST requests.
        /// </summary>
        public static class Host {
            public static ServerEngine Engine { get; private set; }
            public static IServerRuntime Runtime { get; private set; }
            public static string ServerHost { get; private set; }
            public static int ServerPort { get; private set; }

            public static void Setup(
                string serverHost,
                int serverPort,
                ServerEngine engine,
                IServerRuntime runtime) {
                Engine = engine;
                Runtime = runtime;
                ServerHost = serverHost;
                ServerPort = serverPort;
            }
        }

        /// <summary>
        /// Sets up the root API handler.
        /// </summary>
        /// <param name="adminAPI">The API to be used by all
        /// fellow handlers.</param>
        public static void Setup(AdminAPI adminAPI) {
            API = adminAPI;
            var uri = adminAPI.Uris.Root;
            Handle.GET(uri, () => { return 403; });
            Register405OnAllUnsupported(uri, new string[] { "GET" });
        }

        public static void Register405OnAllUnsupported(string uri, string[] methodsSupported, bool allowExtensionsBeyondPatch = false) {
            RESTUtility.Register405OnAllUnsupported(uri, (ushort)Host.ServerPort, methodsSupported, allowExtensionsBeyondPatch);
        }
    }
}