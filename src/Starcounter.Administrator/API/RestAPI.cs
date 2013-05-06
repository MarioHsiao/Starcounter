
using Starcounter.Administrator.API.Handlers;
using Starcounter.Server;
using Starcounter.Server.PublicModel;
using Starcounter.Server.Rest;

namespace Starcounter.Administrator.API {
    /// <summary>
    /// Provides the principal hosting entrypoint for the admin server
    /// REST API.
    /// </summary>
    internal static class RestAPI {
        /// <summary>
        /// Prepares the admin server REST API for use, mainly registering
        /// all it's handlers and setting up the context.
        /// </summary>
        /// <param name="admin">The AdminAPI providing the context.</param>
        /// <param name="serverHost">The host the API is bootstrapped within.</param>
        /// <param name="serverPort">The port the API runs under.</param>
        /// <param name="engine">The application-level server engine the REST
        /// API should provide the interface for.</param>
        /// <param name="runtime">The application-level runtime.</param>
        public static void Bootstrap(
            AdminAPI admin,
            string serverHost,
            int serverPort,
            ServerEngine engine,
            IServerRuntime runtime) {
            
            RootHandler.Host.Setup(serverHost, serverPort, engine, runtime);
            RootHandler.Setup(admin);
            DatabaseCollectionHandler.Setup();
            DatabaseHandler.Setup();
            EngineHandler.Setup();
            EngineCollectionHandler.Setup();
            ExecutableHandler.Setup();
            ExecutableCollectionHandler.Setup();
        }
    }
}