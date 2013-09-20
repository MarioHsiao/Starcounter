using Starcounter;
using Starcounter.Advanced;
using Starcounter.Applications.UsageTrackerApp.API.Installer;
using Starcounter.Applications.UsageTrackerApp.API.Starcounter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter.Applications.UsageTrackerApp.API;
using StarcounterApplicationWebSocket.VersionHandler;

namespace Starcounter.Applications.UsageTrackerApp {
    internal static class UsageTrackerAPI {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="incomingPort">Incomping POST/PUT port number</param>
        /// <param name="backendPort">Backend port for GUI</param>
        /// <param name="publicPort">Public port</param>
        /// <param name="folder">Static resource root folder</param>
        public static void Bootstrap(ushort incomingPort, ushort backendPort, ushort publicPort, string folder) {

            // Starcounter General and usage tracking
            StarcounterCollectionHandler.Bootstrap(incomingPort);

            // Installer tracking
            InstallerCollectionHandler.Bootstrap(incomingPort);

			// Error reporting
			ErrorReportHandler.Setup_PUT(incomingPort);

            // Version handling (Uploads and Downloads)
            VersionHandlerApp.BootStrap(incomingPort, backendPort, publicPort, folder);

        }

    }
}
