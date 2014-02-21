using System;
using Codeplex.Data;
using Starcounter;
using Starcounter.Advanced;
using Starcounter.Server.PublicModel;
using System.Net;
using System.Diagnostics;
using Starcounter.Internal.Web;
using Starcounter.Administrator.API.Utilities;
using Starcounter.Administrator.Server.Utilities;

namespace Starcounter.Administrator.Server.Handlers {
    internal static partial class StarcounterAdminAPI {

        public static void VerifyServerProperties_POST(ushort port, IServerRuntime server) {

            Handle.POST("/api/admin/verify/serverproperties", (Request req) => {

                lock (LOCK) {

                    try {
                        dynamic resultJson = new DynamicJson();
                        resultJson.commandId = null;
                        resultJson.validationErrors = new object[] { };

                        // Validate settings
                        int validationErrors = 0;

                        // Getting POST contents.
                        String content = req.Body;

                        var incomingJson = DynamicJson.Parse(content);

                        #region Validate incoming json data
                        // server name
                        if (incomingJson.IsDefined("name") == true && string.IsNullOrEmpty(incomingJson.name)) {
                            resultJson.validationErrors[validationErrors++] = new { property = "name", message = "invalid server name" };
                        }

                        // Port number
                        ushort httpPort;
                        if (incomingJson.IsDefined("httpPort") == true && (!ushort.TryParse(incomingJson.httpPort.ToString(), out httpPort) || httpPort < IPEndPoint.MinPort || httpPort > IPEndPoint.MaxPort)) {
                            resultJson.validationErrors[validationErrors++] = new { property = "httpPort", message = "invalid port number" };
                        }

                        #endregion

						return RESTUtility.JSON.CreateResponse(resultJson.ToString());
                    }
                    catch (Exception e) {
                        return RestUtils.CreateErrorResponse(e);
                    }

                }

            });
 


        }

    }
}
