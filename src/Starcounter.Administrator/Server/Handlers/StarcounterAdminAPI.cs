
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
        public static void Bootstrap(ushort port, ServerEngine engine, IServerRuntime server, string resourceFolder) {

            // TODO: Get the AppStore url
#if ANDWAH
            string appStoreHost = "http://127.0.0.1:8787";
#else
            string appStoreHost = "http://appstore.polyjuice.com:8787";
#endif


            // TODO: Add an "apps" folder to the Server Configuration
            ServerInfo serverInfo = Program.ServerInterface.GetServerInfo();
            string appsRootFolder = System.IO.Path.Combine(serverInfo.Configuration.EnginesDirectory, "apps");

            // Where AppStore Item images will be saved and shared
            string appImagesSubFolder = "appImages";
            string imageResourceFolder = System.IO.Path.Combine(resourceFolder, appImagesSubFolder);

            StarcounterAdminAPI.Application_GET();
            StarcounterAdminAPI.InstalledApplication_GET(port, appsRootFolder, appImagesSubFolder);
            StarcounterAdminAPI.InstalledApplicationTask_POST(port, appsRootFolder, appStoreHost, imageResourceFolder);
            StarcounterAdminAPI.InstalledApplication_PUT(port, appsRootFolder, appStoreHost, imageResourceFolder);

            StarcounterAdminAPI.AppStore_GET(port, appStoreHost);

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