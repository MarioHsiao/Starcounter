using Starcounter.Applications.UsageTrackerApp.API.Versions;
using Starcounter.Applications.UsageTrackerApp.VersionHandler;
using StarcounterApplicationWebSocket.API.Versions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarcounterApplicationWebSocket.VersionHandler {
    internal class VersionHandlerApp {

        internal static UnpackerWorker UnpackWorker;
        internal static BuildWorker BuildkWorker;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="uPort">Incoming POST port</param>
        internal static void BootStrap(ushort uPort) {

            SyncData.Start();

            VersionHandlerApp.UnpackWorker = new UnpackerWorker();
            VersionHandlerApp.UnpackWorker.Start();

            VersionHandlerApp.BuildkWorker = new BuildWorker();
            VersionHandlerApp.BuildkWorker.Start();

            StarcounterApplicationWebSocket.API.Versions.Version.BootStrap(uPort);
            Channel.BootStrap(uPort);

            Download.BootStrap(uPort);

            Upload.BootStrap(8585); // TODO: Hardcoded portnumber for incoming requests.  use TrackingEnvironment.StarcounterTrackerPort

            Utils.BootStrap(uPort);


            // Kickstart unpacker worker
            VersionHandlerApp.UnpackWorker.Trigger();

            // Kickstart needed builds worker
            VersionHandlerApp.BuildkWorker.Trigger();

        }

    }
}
