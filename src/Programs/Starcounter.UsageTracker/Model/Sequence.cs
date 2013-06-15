using System;
using Starcounter;

namespace Starcounter.Applications.UsageTrackerApp.Model {

    /// <summary>
    /// Sequence
    /// This keeps track of sequence numbers used
    /// </summary>
    [Database]
    public class Sequence {

        /// <summary>
        /// TableName
        /// </summary>
        public string TableName;

        /// <summary>
        /// Last used sequence number
        /// </summary>
        public int No;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableName"></param>
        public Sequence(string tableName) {
            this.TableName = tableName;
            this.No = 1;
        }

    }
  
}
