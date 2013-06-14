using System;
using Starcounter;

namespace Starcounter.Programs.UsageTrackerApp.Model {

    /// <summary>
    /// Starcounter usage tracking data
    /// In a interval the starcounter sends usage
    /// </summary>
    [Database]
    public class StarcounterUsage {

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
        /// Transactions
        /// </summary>
        public long Transactions;

        /// <summary>
        /// Number of running databases at the moment of reporting
        /// </summary>
        public long RunningDatabases;

        /// <summary>
        /// Number of running executables at the moment of reporting
        /// </summary>
        public long RunningExecutables;

        /// <summary>
        /// Number of installed databases at the moment of reporting
        /// </summary>
        public long Databases;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="installation"></param>
        public StarcounterUsage(Installation installation) {
            this.Installation = installation;
        }

    }

}
