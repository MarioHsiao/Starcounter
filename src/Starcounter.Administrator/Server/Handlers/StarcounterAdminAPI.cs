using Administrator.Server.Managers;
using Administrator.Server.Model;
using Starcounter.Internal;
using Starcounter.Logging;
using Starcounter.Server;
using Starcounter.Server.PublicModel;
using System;
using System.Net;

namespace Starcounter.Administrator.Server.Handlers {
    /// <summary>
    /// Provides the entrypoint for the admin frontend
    /// REST API.
    /// </summary>
    internal static partial class StarcounterAdminAPI {

        private static Object LOCK = new Object();
        public static LogSource AdministratorLogSource = new LogSource("Administrator");

        /// <summary>
        /// Prepares the admin server REST API for use, mainly registering
        /// all it's handlers and setting up the context.
        /// </summary>
        /// <param name="admin">The AdminAPI providing the context.</param>
        public static void Bootstrap(ushort port, ServerEngine engine, IServerRuntime server, string resourceFolder) {

            // TODO: Add an "apps" folder to the Server Configuration
            //ServerInfo serverInfo = Program.ServerInterface.GetServerInfo();
            //string appsRootFolder = System.IO.Path.Combine(serverInfo.Configuration.EnginesDirectory, "apps");

            //string appsRootFolder = DeployManager.GetDeployFolder();
            // Where AppStore Item images will be saved and shared
            string appImagesSubFolder = DeployManager.GetAppImagesFolder();
            string imageResourceFolder = System.IO.Path.Combine(resourceFolder, appImagesSubFolder);

            StarcounterAdminAPI.Application_GET();
            //StarcounterAdminAPI.InstalledApplication_GET(port, appsRootFolder, appImagesSubFolder);

            StarcounterAdminAPI.AppStore_GET(port);

            StarcounterAdminAPI.Database_GET(port, server);

            StarcounterAdminAPI.DatabaseSettings_GET(port, server);
            StarcounterAdminAPI.Applications_GET(port, appImagesSubFolder);
            StarcounterAdminAPI.Applications_UPLOAD(port);

            StarcounterAdminAPI.Database_PUT(port, server);
            StarcounterAdminAPI.Database_POST(port, server);
            StarcounterAdminAPI.DatabaseDefaultSettings_GET(port, server);

            StarcounterAdminAPI.VersionCheck_GET(port, server);

            StarcounterAdminAPI.ServerSettings_GET(port, server);
            StarcounterAdminAPI.ServerSettings_PUT(port, server);

            StarcounterAdminAPI.CollationFiles_GET(port, server);

            StarcounterAdminAPI.ServerLog_GET(port);
            StarcounterAdminAPI.Redirects();
        }

        /// <summary>
        /// Register redirect
        /// </summary>
        static void Redirects() {

            Handle.GET("/sql", (Request req) => {

                lock (ServerManager.ServerInstance) {

                    if (ServerManager.ServerInstance.Databases.Count == 0) {
                        // No databases available
                        return System.Net.HttpStatusCode.NotFound;
                    }

                    // Get 'default' database or first one found.
                    Database database = ServerManager.ServerInstance.GetDatabase(StarcounterConstants.DefaultDatabaseName);
                    if (database == null) {
                        // No 'default' database, then pick the first one in the list.
                        database = ServerManager.ServerInstance.Databases[0];
                    }

                    return Self.GET(req.Uri + "/" + database.ID);
                }
            });

            Handle.GET("/sql/{?}", (string databaseName, Request req) => {

                lock (ServerManager.ServerInstance) {

                    Database database = ServerManager.ServerInstance.GetDatabase(databaseName);
                    if (database == null) {
                        return System.Net.HttpStatusCode.NotFound;
                    }

                    Response response = new Response();
                    response.Headers["location"] = "/#/databases/" + database.ID + "/sql";
                    response.StatusCode = (ushort)System.Net.HttpStatusCode.TemporaryRedirect;
                    return response;
                }
            });
        }
    }
}