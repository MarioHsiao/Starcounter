using System;
using Starcounter;

namespace Starcounter.Applications.UsageTrackerApp.Model {

    /// <summary>
    /// Installer Aborts
    /// When the user aborts the installation
    /// </summary>
    [Database]
    public class InstallerException {

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
        /// Message
        /// </summary>
        public string Message;

        /// <summary>
        /// StackTrace
        /// </summary>
        public string StackTrace;

        /// <summary>
        /// HelpLink
        /// </summary>
        public string HelpLink;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="installation"></param>
        public InstallerException(Installation installation) {
            this.Installation = installation;
        }
    }


}
