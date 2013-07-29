using System;
using Codeplex.Data;
using Starcounter;
using Starcounter.Advanced;
using Starcounter.Server.PublicModel;
using System.Net;
using System.Diagnostics;
using Starcounter.Internal;
using Starcounter.Internal.Web;

namespace Starcounter.Administrator.FrontEndAPI {
    internal static partial class FrontEndAPI {

        public static void Server_PUT(ushort port, IServerRuntime server) {

            Handle.PUT("/api/admin/servers/{?}/settings", (string id, Request req) => {
                lock (LOCK) {

                    try {

                        dynamic resultJson = new DynamicJson();
                        resultJson.validationErrors = new object[] { };

                        String content = req.GetBodyStringUtf8_Slow();

                        // Validate settings
                        Response response = Node.LocalhostSystemPortNode.POST("/api/admin/verify/serverproperties",content, null, null);

                        if (response.StatusCode >= 200 && response.StatusCode < 300) {

                            dynamic incomingJson = DynamicJson.Parse(content);

                            ServerInfo serverInfo = Master.ServerInterface.GetServerInfo();
                            if (serverInfo == null) {
                                throw new InvalidProgramException("Could not retrive server informaiton");
                            }

                            // Port number
                            ushort httpPort;
                            if (ushort.TryParse(incomingJson.httpPort.ToString(), out httpPort)) {
                                serverInfo.Configuration.SystemHttpPort = httpPort;
                            }
                            else {
                                throw new FormatException("Invalid message format: httpPort");
                            }

                            serverInfo.Configuration.Save(serverInfo.ServerConfigurationPath);
                            resultJson.message = "Settings saved. The new settings will be used at the next start of the server";

                            // Get new database settings
                            serverInfo = Master.ServerInterface.GetServerInfo();
                            if (serverInfo == null) {
                                throw new InvalidProgramException("Could not retrive server informaiton");
                            }

                            resultJson.settings = new {
                                name = serverInfo.Configuration.Name,
                                httpPort = serverInfo.Configuration.SystemHttpPort,
                                version = CurrentVersion.Version
                            };

                            return new Response() { Uncompressed = HttpResponseBuilder.Slow.FromStatusHeadersAndStringContent((int)HttpStatusCode.OK, null, resultJson.ToString()) };

                        }
                        else if (response.StatusCode == (int)HttpStatusCode.Forbidden) {
                            String validationErrors = response.GetBodyStringUtf8_Slow();
                            return new Response() { Uncompressed = HttpResponseBuilder.Slow.FromStatusHeadersAndStringContent((int)HttpStatusCode.Forbidden, null, validationErrors) };
                        }
                        else {
                            // TODO
                            throw new Exception("Validation error...");
                        }

                    }
                    catch (Exception e) {
                        return RestUtils.CreateErrorResponse(e);
                    }
                }
            });




        }
    }
}
