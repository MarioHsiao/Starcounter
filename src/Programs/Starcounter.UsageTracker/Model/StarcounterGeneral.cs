using System;
using Starcounter;

namespace Starcounter.Applications.UsageTrackerApp.Model {

    /// <summary>
    /// General tracking info
    /// </summary>
    [Database]
    public class StarcounterGeneral {

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
        /// 
        /// </summary>
        public string Module;

        /// <summary>
        /// 
        /// </summary>
        public int Type;

        /// <summary>
        /// 
        /// </summary>
        public string Message;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="installation"></param>
        public StarcounterGeneral(Installation installation) {
            this.Installation = installation;
        }

    }


}
