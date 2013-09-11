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

namespace Starcounter.Administrator.FrontEndAPI {
    internal static partial class FrontEndAPI {

        public static void Database_GET(ushort port, IServerRuntime server) {


            Handle.GET("/api/admin/databases/{?}/settings", (string name, Request req) => {

                lock (LOCK) {

                    try {

                        DatabaseInfo database = Master.ServerInterface.GetDatabaseByName(name);

                        if (database == null) {
                            // Database not found

                            dynamic errorJson = new DynamicJson();

                            errorJson.message = string.Format("Could not find the {0} database", name);
                            errorJson.code = (int)HttpStatusCode.NotFound;
                            errorJson.helpLink = "http://en.wikipedia.org/wiki/HTTP_404"; // TODO

							return RESTUtility.JSON.CreateResponse(errorJson.ToString(), (int)HttpStatusCode.NotFound);
                        }
                        else {
                        }

                        dynamic resultJson = new DynamicJson();

                        // Return the database
                        resultJson.settings = new {
                            name = database.Name,
                            hostProcessId = database.Engine == null ? 0 : database.Engine.HostProcessId,
                            httpPort = database.Configuration.Runtime.DefaultUserHttpPort,
                            schedulerCount = database.Configuration.Runtime.SchedulerCount ?? Environment.ProcessorCount,
                            chunksNumber = database.Configuration.Runtime.ChunksNumber,
                            sqlAggregationSupport = database.Configuration.Runtime.SqlAggregationSupport,

                            //dumpDirectory = database.Configuration.Runtime.DumpDirectory,
                            tempDirectory = database.Configuration.Runtime.TempDirectory,
                            imageDirectory = database.Configuration.Runtime.ImageDirectory,
                            transactionLogDirectory = database.Configuration.Runtime.TransactionLogDirectory,

                        };

                        // collationFile
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
