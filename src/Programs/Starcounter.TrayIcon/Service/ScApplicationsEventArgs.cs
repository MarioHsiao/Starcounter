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
    public class ScApplicationsEventArgs : EventArgs {

        /// <summary>
        /// List of Started applications
        /// </summary>
        public IList<ScApplication> Items = new List<ScApplication>();

    }

}
