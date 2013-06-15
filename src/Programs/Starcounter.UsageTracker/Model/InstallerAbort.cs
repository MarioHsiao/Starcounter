using System;
using Starcounter;

namespace Starcounter.Applications.UsageTrackerApp.Model {

    /// <summary>
    /// Installer Aborts
    /// When the user aborts the installation
    /// </summary>
    [Database]
    public class InstallerAbort {

        /// <summary>
        /// Installation
        /// </summary>
        public Installation Installation;

        /// <summary>
        /// Date of event
        /// </summary>
        public DateTime Date;

        /// <summary>
        /// IP Address of the reporter
        /// </summary>
        public string IP;

        /// <summary>
        /// Stripped Mac address of the reporter
        /// </summary>
        public string Mac;

        /// <summary>
        /// Starcounter version used when reporting
        /// </summary>
        public string Version;

        /// <summary>
        /// Installation mode
        /// 1 = full Installation, 2 = partial installation, 3 = full uninstallation, 4 = partial uninstallation
        /// </summary>
        public int Mode;

        /// <summary>
        /// Message
        /// </summary>
        public string Message;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="installation"></param>
        public InstallerAbort(Installation installation) {
            this.Installation = installation;
        }
    }


}
