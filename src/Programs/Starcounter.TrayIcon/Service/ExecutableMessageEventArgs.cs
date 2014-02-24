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
    public class ExecutableMessageEventArgs : EventArgs {

        /// <summary>
        /// Message Header
        /// </summary>
        public string Header;


        /// <summary>
        /// Message Content
        /// </summary>
        public string Content;

    }

}
