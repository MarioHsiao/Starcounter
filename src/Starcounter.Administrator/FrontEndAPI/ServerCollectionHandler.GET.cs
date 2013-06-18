using System;
using Codeplex.Data;
using Starcounter;
using Starcounter.Advanced;
using Starcounter.Server.PublicModel;
using System.Net;
using System.Diagnostics;
using Starcounter.Internal;

namespace Starcounter.Administrator.FrontEndAPI {
    internal static partial class FrontEndAPI {

        public static void Server_GET(ushort port, IServerRuntime server) {


            Handle.GET("/api/admin/servers/{?}/settings", (string id, Request req) => {

                lock (LOCK) {

                    try {

                        ServerInfo serverInfo = Master.ServerInterface.GetServerInfo();
                        if (serverInfo == null) {
                            throw new InvalidProgramException("Could not retrive server informaiton");
                        }

                        dynamic resultJson = new DynamicJson();

                        resultJson.settings = new {
                            name = serverInfo.Configuration.Name,
                            httpPort = serverInfo.Configuration.SystemHttpPort,
                            version = CurrentVersion.Version
                        };

                        return new Response() { Uncompressed = Starcounter.Internal.Web.HttpResponseBuilder.Slow.FromStatusHeadersAndStringContent((int)HttpStatusCode.OK, null, resultJson.ToString()) };

                    }
                    catch (Exception e) {
                        return RestUtils.CreateErrorResponse(e);
                    }
                }

            });





        }
    }
}
