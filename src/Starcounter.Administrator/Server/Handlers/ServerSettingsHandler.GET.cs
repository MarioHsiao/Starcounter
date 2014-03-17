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
using Starcounter.Administrator.Server.Utilities;

namespace Starcounter.Administrator.Server.Handlers {
    internal static partial class StarcounterAdminAPI {

        /// <summary>
        /// Register Server Settings GET
        /// </summary>
        public static void ServerSettings_GET(ushort port, IServerRuntime server) {

            // Get server settings
            Handle.GET("/api/admin/servers/{?}/settings", (string name, Request req) => {

                try {

                    ServerInfo serverInfo = Program.ServerInterface.GetServerInfo();
                    if (serverInfo == null) {
                        // server not found
                        ErrorResponse errorResponse = new ErrorResponse();
                        errorResponse.Text = string.Format("Could not find the {0} server", name);
                        errorResponse.Helplink = "http://en.wikipedia.org/wiki/HTTP_404"; // TODO
                        return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NotFound, BodyBytes = errorResponse.ToJsonUtf8() };
                    }

                    ServerSettings settings = RestUtils.CreateSettings(serverInfo);

                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.OK, BodyBytes = settings.ToJsonUtf8() };
                }
                catch (Exception e) {
                    return RestUtils.CreateErrorResponse(e);
                }
            });
        }
    }
}
