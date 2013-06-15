using System;
using Starcounter;

namespace Starcounter.Applications.UsageTrackerApp.Model {

    /// <summary>
    /// Installer End
    /// When the installer closed (exits)
    /// </summary>
    [Database]
    public class InstallerEnd {

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
        /// list of links the user cliced
        /// devided with 'Environment.NewLine'
        /// </summary>
        public string LinksClicked;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="installation"></param>
        public InstallerEnd(Installation installation) {
            this.Installation = installation;
        }


    }

  
}
