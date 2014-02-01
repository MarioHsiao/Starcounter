using Starcounter;
using Starcounter.Applications.UsageTrackerApp.VersionHandler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarcounterApplicationWebSocket.VersionHandler.Model {

    /// <summary>
    /// Somebody
    /// </summary>
    [Database]
    public class Somebody {
        /// <summary>
        /// Unique generated key
        /// </summary>
        public string DownloadKey;

        /// <summary>
        /// Email
        /// </summary>
        public string Email;
    }
}
