using System;
using Starcounter;

namespace Starcounter.Applications.UsageTrackerApp.Model {

    /// <summary>
    /// Installer finish
    /// When the installer is finished with executing some work (installing/uninstalling)
    /// </summary>
    [Database]
    public class InstallerFinish {

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
        /// True or false depending if the installation ended successfully
        /// </summary>
        public bool Success;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="installation"></param>
        public InstallerFinish(Installation installation) {
            this.Installation = installation;
        }

    }

  
}
