using System;

namespace Starcounter.Applications.UsageTrackerApp.API.Backend {
    internal static partial class Administrator {

        // Installation

        //private static string key = "dfhgry43gAj3cDvl4G0H3H4T34R04D3tw46464fsd";

        private static Object LOCK = new Object();

        public static void Bootstrap(ushort port) {

            Administrator.Installation_GET(port);
            Administrator.Installation_DELETE(port);

            Administrator.Start_GET(port);
            Administrator.Start_DELETE(port);

            Administrator.Executing_GET(port);
            Administrator.Executing_DELETE(port);

            Administrator.Finish_GET(port);
            Administrator.Finish_DELETE(port);

            Administrator.End_GET(port);
            Administrator.End_DELETE(port);

            Administrator.Abort_GET(port);
            Administrator.Abort_DELETE(port);

            Administrator.Usage_GET(port);
            Administrator.Usage_DELETE(port);

            Administrator.Overview_GET(port);

        }


    }
}
