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

        public static void Database_PUT(ushort port, IServerRuntime server) {

            Handle.PUT("/api/admin/databases/{?}/settings", (string name, Request req) => {

                lock (LOCK) {

                    try {

                        String content = req.Body;

                        Response response = Node.LocalhostSystemPortNode.POST("/api/admin/verify/databaseproperties", content, null, null);

                        if (response.StatusCode >= 200 && response.StatusCode < 300) {

                            dynamic incomingJson = DynamicJson.Parse(content);

                            DatabaseInfo database = Master.ServerInterface.GetDatabaseByName(name);

                            if (database == null) {
                                // Database not found

                                dynamic errorJson = new DynamicJson();

                                errorJson.message = string.Format("Could not find the {0} database", name);
                                errorJson.code = (int)HttpStatusCode.NotFound;
                                errorJson.helpLink = "http://en.wikipedia.org/wiki/HTTP_404"; // TODO

                                return new Response() { Uncompressed = HttpResponseBuilder.Slow.FromStatusHeadersAndStringContent((int)HttpStatusCode.NotFound, null, errorJson.ToString()) };
                            }
                            else {

                                dynamic resultJson = new DynamicJson();
                                resultJson.validationErrors = new object[] { };

                                // Port number
                                // Port number
                                ushort httpPort;
                                if (!ushort.TryParse(incomingJson.httpPort.ToString(), out httpPort)) {
                                    throw new FormatException("Invalid message format: httpPort");
                                }
                                else {
                                    database.Configuration.Runtime.DefaultUserHttpPort = httpPort;
                                }


                                // Scheduler Count
                                int schedulerCount;
                                if (!int.TryParse(incomingJson.schedulerCount.ToString(), out schedulerCount)) {
                                    throw new FormatException("Invalid message format: schedulerCount");
                                }
                                else {
                                    database.Configuration.Runtime.SchedulerCount = schedulerCount;
                                }

                                // Chunks Number
                                int chunksNumber;
                                if (!int.TryParse(incomingJson.chunksNumber.ToString(), out chunksNumber)) {
                                    throw new FormatException("Invalid message format: chunksNumber");
                                }
                                else {
                                    database.Configuration.Runtime.ChunksNumber = chunksNumber;
                                }

                                // SQL Aggregation support
                                bool sqlAggregationSupport;
                                if (!bool.TryParse(incomingJson.sqlAggregationSupport.ToString(), out sqlAggregationSupport)) {
                                    throw new FormatException("Invalid message format: sqlAggregationSupport");
                                }
                                else {
                                    database.Configuration.Runtime.SqlAggregationSupport = sqlAggregationSupport;
                                }

                                database.Configuration.Save();
                                resultJson.message = "Settings saved. The new settings will be used at the next start of the database";

                                // Get new database with the new settings
                                database = Master.ServerInterface.GetDatabaseByName(database.Name);
                                if (database == null) {
                                    // Database not found

                                    dynamic errorJson = new DynamicJson();

                                    errorJson.message = string.Format("Could not find the {0} database", name);
                                    errorJson.code = (int)HttpStatusCode.NotFound;
                                    errorJson.helpLink = "http://en.wikipedia.org/wiki/HTTP_404"; // TODO

                                    return new Response() { Uncompressed = HttpResponseBuilder.Slow.FromStatusHeadersAndStringContent((int)HttpStatusCode.NotFound, null, errorJson.ToString()) };
                                }
                                else {

                                    // Return the database
                                    resultJson.settings = new {
                                        name = database.Name,
                                        hostProcessId = database.Engine == null ? 0 : database.Engine.HostProcessId,
                                        httpPort = database.Configuration.Runtime.DefaultUserHttpPort,
                                        schedulerCount = database.Configuration.Runtime.SchedulerCount ?? Environment.ProcessorCount,
                                        chunksNumber = database.Configuration.Runtime.ChunksNumber,
                                        sqlAggregationSupport = database.Configuration.Runtime.SqlAggregationSupport
                                    };

                                    return new Response() { Uncompressed = HttpResponseBuilder.Slow.FromStatusHeadersAndStringContent((int)HttpStatusCode.OK, null, resultJson.ToString()) };
                                }

                            }








                        }
                        else if (response.StatusCode == (int)HttpStatusCode.Forbidden) {
                            String validationErrors = response.Body;
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
