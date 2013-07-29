using System;
using Codeplex.Data;
using Starcounter;
using Starcounter.Advanced;
using Starcounter.Server.PublicModel;
using System.Net;
using System.Diagnostics;
using Starcounter.Internal.Web;

namespace Starcounter.Administrator.FrontEndAPI {
    internal static partial class FrontEndAPI {

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
                        String content = req.GetBodyStringUtf8_Slow();

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

                        return new Response() { Uncompressed = HttpResponseBuilder.Slow.FromStatusHeadersAndStringContent((int)HttpStatusCode.OK, null, resultJson.ToString()) };

                    }
                    catch (Exception e) {
                        return RestUtils.CreateErrorResponse(e);
                    }

                }

            });
 


        }

    }
}
