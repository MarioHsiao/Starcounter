
using Starcounter.Administrator.API.Handlers;
using Starcounter.Server;
using Starcounter.Server.PublicModel;
using Starcounter.Server.Rest;
using System;

namespace Starcounter.Administrator.Server.Handlers {
    /// <summary>
    /// Provides the entrypoint for the admin frontend
    /// REST API.
    /// </summary>
    internal static partial class StarcounterAdminAPI {

        private static Object LOCK = new Object();

        /// <summary>
        /// Prepares the admin server REST API for use, mainly registering
        /// all it's handlers and setting up the context.
        /// </summary>
        /// <param name="admin">The AdminAPI providing the context.</param>
        public static void Bootstrap(ushort port, ServerEngine engine, IServerRuntime server) {

            StarcounterAdminAPI.Executable_GET();
            StarcounterAdminAPI.Database_GET(port, server);
            StarcounterAdminAPI.Database_PUT(port, server);
            StarcounterAdminAPI.Database_POST(port, server);
            StarcounterAdminAPI.DatabaseDefaultSettings_GET(port, server);

            StarcounterAdminAPI.VersionCheck_GET(port, server);

            StarcounterAdminAPI.VerifyDatabaseProperties_POST(port, server);

            StarcounterAdminAPI.Server_GET(port, server);
            StarcounterAdminAPI.Server_PUT(port, server);
            StarcounterAdminAPI.VerifyServerProperties_POST(port, server);

            StarcounterAdminAPI.ServerLog_GET(port);

        }
    }
}