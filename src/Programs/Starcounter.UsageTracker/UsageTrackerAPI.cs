﻿using Starcounter;
using Starcounter.Advanced;
using Starcounter.Programs.UsageTrackerApp.API.Installer;
using Starcounter.Programs.UsageTrackerApp.API.Starcounter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Programs.UsageTrackerApp {
    internal static class UsageTrackerAPI {

        public static void Bootstrap(ushort port) {

            // Starcounter General and usage tracking
            StarcounterCollectionHandler.Bootstrap(port);

            // Installer tracking
            InstallerCollectionHandler.Bootstrap(port);

        }


    }
}
