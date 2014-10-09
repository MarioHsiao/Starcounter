using System;
using Codeplex.Data;
using Starcounter;
using Starcounter.Advanced;
using Starcounter.Server.PublicModel;
using System.Net;
using System.Diagnostics;
using Starcounter.Internal;
using Starcounter.Internal.Web;
using Starcounter.Administrator.API.Utilities;
using Starcounter.Administrator.API.Handlers;
using Starcounter.Server.Rest.Representations.JSON;
using Starcounter.Server.Rest;
using Starcounter.CommandLine;
using System.IO;
using Starcounter.Rest.ExtensionMethods;
using System.Collections.Generic;
using Starcounter.Administrator.Server.Utilities;
using System.Windows.Forms;
using System.Text;
using System.Reflection;
using System.ComponentModel;
using Administrator.Server.ApplicationContainer;

namespace Starcounter.Administrator.Server.Handlers {
    internal static partial class StarcounterAdminAPI {

        /// <summary>
        /// Register Application GET
        /// </summary>
        public static void InstalledApplication_PUT(string appsRootFolder) {

            //
            // Install Application zip package (from the body)
            //
            Handle.PUT("/api/admin/installed/apps", (Request request) => {

                try {
                    using (MemoryStream packageZip = new MemoryStream(request.BodyBytes)) {

                        string host = request["Host"];

                        // TODO: Assure that the url is a full url. like file://mypackage.zip or something like that
                        string url = host;
                        Package.Install(url, packageZip, appsRootFolder, false);
                    }

                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.Created };
                }
                catch (Exception e) {
                    return RestUtils.CreateErrorResponse(e);
                }
            });
        }
    }
}
