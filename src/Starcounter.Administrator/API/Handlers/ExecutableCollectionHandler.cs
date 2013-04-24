
using Starcounter.Advanced;
using Starcounter.Server;
using Starcounter.Server.PublicModel;
using Starcounter.Server.Rest;

namespace Starcounter.Administrator.API.Handlers {

    /// <summary>
    /// Excapsulates the admin server functionality acting on the resource
    /// "executables hosted in a database". 
    /// </summary>
    /// <remarks>
    /// This resource is a collection of executables currently running
    /// inside a named database (under a particular server). The resource
    /// should support retreival using GET, execution of a new application
    /// using POST, stopping of an application using DELETE, and possibly
    /// patching the set of running executables using PATCH and maybe even
    /// assure a set of running executables using PUT.
    /// </remarks>
    internal static partial class ExecutableCollectionHandler {
        static ServerEngine engine;
        static IServerRuntime runtime;
        static string serverHost;
        static int serverPort;

        internal static void Setup(
            string serverHost,
            int serverPort,
            AdminUri admin,
            ServerEngine engine, 
            IServerRuntime runtime) {
            ExecutableCollectionHandler.engine = engine;
            ExecutableCollectionHandler.runtime = runtime;
            ExecutableCollectionHandler.serverHost = serverHost;
            ExecutableCollectionHandler.serverPort = serverPort;

            Handle.POST<string, Request>(admin.Executables, OnPOST);
        }
    }
}