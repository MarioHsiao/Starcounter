using System;
using Codeplex.Data;
using Starcounter;
using Starcounter.Advanced;
using Starcounter.Server.PublicModel;
using System.Net;
using System.Diagnostics;

namespace Starcounter.Administrator.FrontEndAPI {
    internal static partial class FrontEndAPI {

        public static void SQL_GET(ushort port) {

            Handle.POST("/api/admin/databases/{?}/sql", (string name, Request req) => {
                lock (LOCK) {

                    try {

                        DatabaseInfo database = Master.ServerInterface.GetDatabaseByName(name);

                        string bodyData = req.GetBodyStringUtf8_Slow();   // Retrieve the sql command in the body

                        Response resp = Node.LocalhostSystemPortNode.POST(
                            string.Format("/__{0}/sql", database.Name),
                            bodyData,
                            "MyHeader1: 123\r\nMyHeader2: 456\r\n",
                            null);

                        if (resp.StatusCode >= 200 && resp.StatusCode < 300) {
                            return resp.GetBodyStringUtf8_Slow();
                        }

                        // TODO: Do not return error code. return a more user friendly message
                        return (int)resp.StatusCode;
                    }
                    catch (Exception e) {
                        return RestUtils.CreateErrorResponse(e);
                    }
                }

            });






        }

    }
}
