using Starcounter.Advanced;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Administrator.API.Handlers {
    /// <summary>
    /// Provides the handlers for the admin server root database
    /// collection resource.
    /// </summary>
    internal static partial class DatabaseCollectionHandler {
        /// <summary>
        /// Install handlers for the resource represented by this class and
        /// perform custom setup.
        /// </summary>
        internal static void Setup() {
            var uri = RootHandler.API.Uris.Databases;

            Handle.GET<Request>(uri, OnGET);
            Handle.POST<Request>(uri, OnPOST);
            RootHandler.Register405OnAllUnsupported(uri, new string[] { "GET", "POST" });
        }
    }
}