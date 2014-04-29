using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Applications.UsageTracker.VersionHandling.Model {

    /// <summary>
    /// Registred user
    /// </summary>
    [Database]
    public class RegisteredUser {

        /// <summary>
        /// Date (UTC) when the user registred
        /// </summary>
        public DateTime RegistredDate;

        /// <summary>
        /// IPAdress used
        /// </summary>
        public string IPAdress;

        /// <summary>
        /// Email
        /// </summary>
        public string Email;
    }
}
