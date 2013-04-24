using Starcounter.Advanced;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Administrator.API.Handlers {
    /// <summary>
    /// Provides the handlers for the admin server database resource.
    /// </summary>
    internal static partial class DatabaseHandler {
        /// <summary>
        /// Install handlers for the resource represented by this class and
        /// performs custom setup.
        internal static void Setup() {
            var uri = RootHandler.API.Uris.Database;

            Handle.GET<string, Request>(uri, OnGET);
            RootHandler.Register405OnAllUnsupported(uri, new string[] { "GET" });
        }
    }
}