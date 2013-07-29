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

        public static void Console_GET(ushort port) {

            Handle.GET("/api/admin/databases/{?}/console", (string name, Request req) => {

                lock (LOCK) {

                    dynamic resultJson = new DynamicJson();
                    resultJson.console = null;
                    resultJson.exception = null;

                    try {
                        string bodyData = req.GetBodyStringUtf8_Slow();   // Retrieve the message

                        Response response = Node.LocalhostSystemPortNode.GET(string.Format("/__{0}/console", name), null, null);

                        if (response == null) {

                            dynamic errorJson = new DynamicJson();

                            errorJson.message = string.Format("Could not connect to the {0} database", name);
                            errorJson.code = (int)HttpStatusCode.NotFound;
                            errorJson.helpLink = "http://en.wikipedia.org/wiki/HTTP_" + response.StatusCode; // TODO

                            return new Response() { Uncompressed = HttpResponseBuilder.Slow.FromStatusHeadersAndStringContent((int)HttpStatusCode.NotFound, null, errorJson.ToString()) };
                        }

                        if (response.StatusCode >= 200 && response.StatusCode < 300) {
                            // Success
                            return new Response() { Uncompressed = HttpResponseBuilder.Slow.FromStatusHeadersAndStringContent((int)HttpStatusCode.OK, null, response.GetBodyStringUtf8_Slow()) };
                        }
                        else {
                            // Error
                            dynamic errorJson = new DynamicJson();
                            if (string.IsNullOrEmpty(bodyData)) {
                                errorJson.message = string.Format("Could not retrive the console output from the {0} database, Caused by a missing/not started database or there is no Executable running in the database", name);
                            }
                            else {
                                errorJson.message = bodyData;
                            }
                            errorJson.code = (int)response.StatusCode;
                            errorJson.helpLink = null;

                            return new Response() { Uncompressed = HttpResponseBuilder.Slow.FromStatusHeadersAndStringContent((int)response.StatusCode, null, errorJson.ToString()) };

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
