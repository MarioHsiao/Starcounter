
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

            StarcounterAdminAPI.Application_GET();
            StarcounterAdminAPI.Database_GET(port, server);

            StarcounterAdminAPI.DatabaseSettings_GET(port, server);

            StarcounterAdminAPI.Database_PUT(port, server);
            StarcounterAdminAPI.Database_POST(port, server);
            StarcounterAdminAPI.DatabaseDefaultSettings_GET(port, server);

            StarcounterAdminAPI.VersionCheck_GET(port, server);

            StarcounterAdminAPI.ServerSettings_GET(port, server);
            StarcounterAdminAPI.ServerSettings_PUT(port, server);

            StarcounterAdminAPI.CollationFiles_GET(port, server);

            StarcounterAdminAPI.ServerLog_GET(port);

        }
    }
}