using System;
using Starcounter;

namespace Starcounter.Applications.UsageTrackerApp.Model {


    /// <summary>
    /// 
    /// </summary>
    [Database]
    public class StatisticDay : StatisticSum {

        /// <summary>
        /// 
        /// </summary>
        public DateTime Date;
  
    }


    /// <summary>
    /// 
    /// </summary>
    [Database]
    public class StatisticSum {
        /// <summary>
        /// 
        /// </summary>
        public Int64 Downloads;
        /// <summary>
        /// 
        /// </summary>
        public Int64 Installations;
        /// <summary>
        /// 
        /// </summary>
        public Int64 Uninstallations;

    }

  
}
