using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Tools.Service {

    /// <summary>
    /// 
    /// </summary>
    public class ErrorEventArgs : EventArgs {

        /// <summary>
        /// 
        /// </summary>
        public bool HasError {
            get {
                return !string.IsNullOrEmpty(this.ErrorMessage);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        public string ErrorMessage { get; set; }


    }

}
