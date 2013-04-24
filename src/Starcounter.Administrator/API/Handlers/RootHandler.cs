
using Starcounter.Server;
using Starcounter.Server.PublicModel;
using Starcounter.Server.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Administrator.API.Handlers {

    /// <summary>
    /// Provides handlers for REST calls targeting the admin server API
    /// root resource (i.e. the principal API REST entrypoint).
    /// </summary>
    internal static partial class RootHandler {
        /// <summary>
        /// Provide all handler classes with a single URI set and makes
        /// sure it's only accessible from one place.
        /// </summary>
        /// <remarks>
        /// This way, we can start the admin server with a dependency
        /// injection pattern with what URI set to use for the API, using
        /// different sets in tests (like test doubles) and experiment
        /// with a new set for a newer version of the API if we like.
        /// </remarks>
        public static AdminUri Uris { get; private set; }

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
        /// <param name="adminUris">The URI set to be used by all
        /// fellow handlers.</param>
        public static void Setup(AdminUri adminUris) {
            Uris = adminUris;
        }
    }
}