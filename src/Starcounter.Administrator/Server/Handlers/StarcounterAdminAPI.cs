using Administrator.Server.Managers;
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

            // Read Advanced settings file
#if REMOTE_CONTROL
            string advSettingsFile = System.IO.Path.Combine(appsRootFolder, "advancedsettings.json");
            if (System.IO.File.Exists(advSettingsFile)) {
                AdministratorLogSource.LogNotice("Reading advancedsettings.json");

                try {
                    Representations.JSON.AdvancedSettings settings = new Representations.JSON.AdvancedSettings();
                    settings.PopulateFromJson(System.IO.File.ReadAllText(advSettingsFile));

                    if (settings.RemoteAccess) {
                        if (settings.RemoteAccessPort >= IPEndPoint.MinPort && settings.RemoteAccessPort <= IPEndPoint.MaxPort && settings.RemoteAccessPort != port) {
                            // NOTE! This allows a remote computer to install/uninstall/update and start/stop applications
                            StarcounterAdminAPI.ServerTaskHandler_POST((ushort)settings.RemoteAccessPort, appStoreHost, imageResourceFolder);
                        }
                    }
                }
                catch (Exception e) {
                    AdministratorLogSource.LogException(e);
                }
            }

            StarcounterAdminAPI.ServerTaskHandler_POST(port, appStoreHost, imageResourceFolder);
#endif

            StarcounterAdminAPI.AppStore_GET(port);

            StarcounterAdminAPI.Database_GET(port, server);

            StarcounterAdminAPI.DatabaseSettings_GET(port, server);
            StarcounterAdminAPI.Applications_GET(port, appImagesSubFolder);

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