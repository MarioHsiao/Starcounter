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

namespace Starcounter.Applications.UsageTrackerApp {
    internal static class UsageTrackerAPI {

        public static void Bootstrap(ushort port) {

            // Starcounter General and usage tracking
            StarcounterCollectionHandler.Bootstrap(port);

            // Installer tracking
            InstallerCollectionHandler.Bootstrap(port);

			// Error reporting
			ErrorReportHandler.Setup_PUT(port);
        }

    }
}
