using System;
using Starcounter;

namespace Starcounter.Applications.UsageTrackerApp.Model {


    /// <summary>
    /// Installer executing
    /// When the installer executing some work (installing/uninstalling)
    /// </summary>
    [Database]
    public class InstallerExecuting {

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
        /// true or false depending if the user installed the PersonalServer
        /// </summary>
        public bool PersonalServer;

        /// <summary>
        /// true or false depending if the user installed the Visual Studio 2012 Extention
        /// </summary>
        public bool VS2012Extention;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="installation"></param>
        public InstallerExecuting(Installation installation) {
            this.Installation = installation;
        }
    }



}
