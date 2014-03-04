using Starcounter.Server.Rest.Representations.JSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Tools.Service {

    /// <summary>
    /// 
    /// </summary>
    public class ExecutablesEventArgs : EventArgs {

        /// <summary>
        /// List of Started executables
        /// </summary>
        public IList<Executable> Items = new List<Executable>();

    }

}
