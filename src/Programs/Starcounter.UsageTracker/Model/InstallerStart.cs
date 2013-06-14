using System;
using Starcounter;

namespace Starcounter.Programs.UsageTrackerApp.Model {

    /// <summary>
    /// Installer Start
    /// When the installer starts (User clickes on the installer executable)
    /// </summary>
    [Database]
    public class InstallerStart {

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
        /// Installed ram on the reported computer
        /// In MB
        /// </summary>
        public long InstalledRam;

        /// <summary>
        /// Available ram on the reported computer
        /// In MB
        /// </summary>
        public long AvailableRam;

        /// <summary>
        /// CPU on the reported computer
        /// </summary>
        public string Cpu;

        /// <summary>
        /// OS on the reported computer
        /// </summary>
        public string Os;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="installation"></param>
        public InstallerStart(Installation installation) {
            this.Installation = installation;
        }

    }
  
}
