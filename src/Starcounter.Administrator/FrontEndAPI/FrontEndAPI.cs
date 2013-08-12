
using Starcounter.Administrator.API.Handlers;
using Starcounter.Server;
using Starcounter.Server.PublicModel;
using Starcounter.Server.Rest;
using System;

namespace Starcounter.Administrator.FrontEndAPI {
    /// <summary>
    /// Provides the entrypoint for the admin frontend
    /// REST API.
    /// </summary>
    internal static partial class FrontEndAPI {

        private static Object LOCK = new Object();


        /// <summary>
        /// Prepares the admin server REST API for use, mainly registering
        /// all it's handlers and setting up the context.
        /// </summary>
        /// <param name="admin">The AdminAPI providing the context.</param>
        public static void Bootstrap(ushort port, ServerEngine engine, IServerRuntime server) {

            FrontEndAPI.Database_GET(port, server);
            FrontEndAPI.Database_PUT(port, server);
            FrontEndAPI.Database_POST(port, server);
            FrontEndAPI.DatabaseDefaultSettings_GET(port, server);

            FrontEndAPI.VerifyDatabaseProperties_POST(port, server);

            FrontEndAPI.Server_GET(port, server);
            FrontEndAPI.Server_PUT(port, server);
            FrontEndAPI.VerifyServerProperties_POST(port, server);

//            FrontEndAPI.Console_GET(port);
            FrontEndAPI.ServerLog_GET(port);
//            FrontEndAPI.SQL_GET(port);


        }
    }
}