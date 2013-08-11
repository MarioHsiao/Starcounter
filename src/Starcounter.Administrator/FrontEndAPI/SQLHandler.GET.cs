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

        public static void SQL_GET(ushort port) {

            Handle.POST("/api/admin/databases/{?}/sql", (string name, Request req) => {
                lock (LOCK) {

                    try {

                        DatabaseInfo database = Master.ServerInterface.GetDatabaseByName(name);

                        string bodyData = req.Body;   // Retrieve the sql command in the body

                        Response response = Node.LocalhostSystemPortNode.POST(
                            string.Format("/__{0}/sql", database.Name),
                            bodyData,
                            "MyHeader1: 123\r\nMyHeader2: 456\r\n",
                            null);

                        if (response.StatusCode >= 200 && response.StatusCode < 300) {
                            return response.Body;
                        }
                        else {
                            dynamic errorJson = new DynamicJson();
                            errorJson.message = string.Format("Could not execute the query on database {0}, Caused by a missing or not started database", name);
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
