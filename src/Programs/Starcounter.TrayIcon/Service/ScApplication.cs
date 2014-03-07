using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Tools.Service {

    /// <summary>
    /// 
    /// </summary>
    public class ScApplication {

        /// <summary>
        /// Application name
        /// </summary>
        public string Name;

        /// <summary>
        /// Application listening ports
        /// This can be empty
        /// </summary>
        public IList<int> Ports;
    }

}
