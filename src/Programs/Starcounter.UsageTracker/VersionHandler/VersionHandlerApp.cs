﻿using Starcounter.Applications.UsageTrackerApp.API.Versions;
using Starcounter.Applications.UsageTrackerApp.VersionHandler;
using StarcounterApplicationWebSocket.API.Versions;
using StarcounterApplicationWebSocket.VersionHandler.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarcounterApplicationWebSocket.VersionHandler {
    internal class VersionHandlerApp {

        internal static UnpackerWorker UnpackWorker;
        internal static BuildWorker BuildkWorker;
        internal static VersionHandlerSettings Settings;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="port">Backend gui port</param>
        internal static void BootStrap(ushort port) {

            // Get Settings
            VersionHandlerApp.Settings = VersionHandlerSettings.GetSettings();

            // Set log filename to logwriter
            LogWriter.Init(VersionHandlerApp.Settings.LogFile);

            SyncData.Start();

            VersionHandlerApp.UnpackWorker = new UnpackerWorker();
            VersionHandlerApp.UnpackWorker.Start();

            VersionHandlerApp.BuildkWorker = new BuildWorker();
            VersionHandlerApp.BuildkWorker.Start();

            Download.BootStrap(80); // Download port, should be accessable from internet

            Upload.BootStrap(8585); // TODO: Hardcoded portnumber for incoming requests.  use TrackingEnvironment.StarcounterTrackerPort

            Utils.BootStrap(port); // Utils gui (should not be accessable from outside (internet)

            // Kickstart unpacker worker
            VersionHandlerApp.UnpackWorker.Trigger();

            // Kickstart needed builds worker
            VersionHandlerApp.BuildkWorker.Trigger();

        }

    }
}
