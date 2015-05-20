using Starcounter;
using Starcounter.Advanced;
using Starcounter.Applications.UsageTrackerApp.API.Versions;
using Starcounter.Applications.UsageTrackerApp.VersionHandler;
using StarcounterApplicationWebSocket.API.Versions;
using StarcounterApplicationWebSocket.VersionHandler.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter.Internal;

namespace StarcounterApplicationWebSocket.VersionHandler {
    internal class VersionHandlerApp {

        internal static UnpackerWorker UnpackWorker;
        internal static BuildWorker BuildkWorker;
        internal static VersionHandlerSettings Settings;
#if ANDWAH
        internal static String StarcounterTrackerUrl = "192.168.60.104";
#else
        internal static String StarcounterTrackerUrl = "downloads.starcounter.com";
#endif

        /// <summary>
        /// 
        /// </summary>
        /// <param name="incomingPort">Incomping POST/PUT port number</param>
        /// <param name="backendPort">Backend port for GUI</param>
        /// <param name="publicPort">Public port</param>
        /// <param name="folder">Static resource root folder</param>
        internal static void BootStrap(ushort incomingPort, ushort backendPort, ushort publicPort, string folder) {

            // Get Settings
            VersionHandlerApp.Settings = VersionHandlerSettings.GetSettings();

            // Add public static resource
            AppsBootstrapper.AddStaticFileDirectory(publicPort, Path.GetFullPath(System.IO.Path.Combine(folder, "public")));

            // Documentation
            String publicDocumentationFolder = System.IO.Path.Combine(VersionHandlerApp.Settings.DocumentationFolder, "public");
            if (!Directory.Exists(publicDocumentationFolder)) {
                Directory.CreateDirectory(publicDocumentationFolder);
            }

            AppsBootstrapper.AddStaticFileDirectory(publicPort, Path.GetFullPath(publicDocumentationFolder));

            // Set log filename to logwriter
            LogWriter.Init(VersionHandlerApp.Settings.LogFile);

            SyncData.Start();

            VersionHandlerApp.UnpackWorker = new UnpackerWorker();
            VersionHandlerApp.UnpackWorker.Start();

            VersionHandlerApp.BuildkWorker = new BuildWorker();
            VersionHandlerApp.BuildkWorker.Start();

            // Public API
            Download.BootStrap(publicPort);
            Versions_Get.BootStrap(publicPort);
            Documentation_Get.BootStrap(publicPort);

            Upload.BootStrap(incomingPort);
            Register.BootStrap(publicPort);
            Login.BootStrap(publicPort);

            // Not Public API
            Utils.BootStrap(backendPort);
            Statistics.BootStrap(backendPort);

            // Kickstart unpacker worker
            VersionHandlerApp.UnpackWorker.Trigger();

            // Kickstart needed builds worker
            VersionHandlerApp.BuildkWorker.Trigger();

        }

    }
}
