using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Tools.Service {

    /// <summary>
    /// 
    /// </summary>
    public class StatusEventArgs : EventArgs {

        /// <summary>
        /// 
        /// </summary>
        public bool Running { get; set; }


        /// <summary>
        /// 
        /// </summary>
        public bool InteractiveMode { get; set; }

    }

}
