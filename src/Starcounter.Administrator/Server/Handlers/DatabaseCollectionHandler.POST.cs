using System;
using Codeplex.Data;
using Starcounter;
using Starcounter.Advanced;
using Starcounter.Server.PublicModel;
using System.Net;
using System.Diagnostics;
using Starcounter.Server.PublicModel.Commands;
using Starcounter.Internal;
using Starcounter.Internal.Web;
using Starcounter.Administrator.API.Utilities;
using Starcounter.Administrator.Server.Utilities;

namespace Starcounter.Administrator.Server.Handlers {
    internal static partial class StarcounterAdminAPI {

        public static void Database_POST(ushort port, IServerRuntime server) {

            Handle.POST("/api/admin/databases/{?}/createdatabase", (string name, Request req) => {

                lock (LOCK) {

                    try {

                        String content = req.Body;

                        Response response = Node.LocalhostSystemPortNode.POST("/api/admin/verify/databaseproperties", content, null);

                        if (response.StatusCode >= 200 && response.StatusCode < 300) {

                            dynamic incomingJson = DynamicJson.Parse(content);

                            DatabaseInfo database = Program.ServerInterface.GetDatabaseByName(name);


                            dynamic resultJson = new DynamicJson();

                            var command = new CreateDatabaseCommand(Program.ServerEngine, incomingJson.name) {
                                EnableWaiting = true
                            };

                            // Port number
                            ushort httpPort;
                            if (!ushort.TryParse(incomingJson.httpPort.ToString(), out httpPort)) {
                                throw new FormatException("Invalid message format: httpPort");
                            }
                            else {
                                command.SetupProperties.Configuration.Runtime.DefaultUserHttpPort = httpPort;
                            }

                            // Scheduler Count
                            int schedulerCount;
                            if (!int.TryParse(incomingJson.schedulerCount.ToString(), out schedulerCount)) {
                                throw new FormatException("Invalid message format: schedulerCount");
                            }
                            else {
                                command.SetupProperties.Configuration.Runtime.SchedulerCount = schedulerCount;
                            }

                            // Chunks Number
                            int chunksNumber;
                            if (!int.TryParse(incomingJson.chunksNumber.ToString(), out chunksNumber)) {
                                throw new FormatException("Invalid message format: chunksNumber");
                            }
                            else {
                                command.SetupProperties.Configuration.Runtime.ChunksNumber = chunksNumber;
                            }

                            // SQL Aggregation support
                            bool sqlAggregationSupport;
                            if (!bool.TryParse(incomingJson.sqlAggregationSupport.ToString(), out sqlAggregationSupport)) {
                                throw new FormatException("Invalid message format: sqlAggregationSupport");
                            }
                            else {
                                command.SetupProperties.Configuration.Runtime.SqlAggregationSupport = sqlAggregationSupport;
                            }

                            if (incomingJson.IsDefined("collationFile") == true && !string.IsNullOrEmpty(incomingJson.collationFile)) {
                                command.SetupProperties.StorageConfiguration.CollationFile = incomingJson.collationFile;
                            }


                            //command.SetupProperties.Configuration.Runtime.DumpDirectory = incomingJson.dumpDirectory;
                            command.SetupProperties.Configuration.Runtime.TempDirectory = incomingJson.tempDirectory;
                            command.SetupProperties.Configuration.Runtime.ImageDirectory = incomingJson.imageDirectory;
                            command.SetupProperties.Configuration.Runtime.TransactionLogDirectory = incomingJson.transactionLogDirectory;

                            command.EnableWaiting = true;

                            var info = server.Execute(command);
                            info = server.Wait(info);
                            if (info.HasError) {
                                //return ToErrorResponse(info);
                                // TODO:
                                resultJson.errors = new object[] { };

                                for (int n = 0; n < info.Errors.Length; n++) {
                                    ErrorInfo errorInfo = info.Errors[n];
                                    ErrorMessage errorMessage = errorInfo.ToErrorMessage();
                                    resultJson.errors[n] = new { message = errorMessage.Message, helpLink = errorMessage.Helplink };
                                }


                            }

							return RESTUtility.JSON.CreateResponse(resultJson.ToString());
							
                            //database.Configuration.Save();
                            //resultJson.message = "Settings saved. The new settings will be used at the next start of the database";

                            //// Get new database with the new settings
                            //database = Master.ServerInterface.GetDatabaseByName(database.Name);
                            //if (database == null) {
                            //    // Database not found

                            //    dynamic errorJson = new DynamicJson();

                            //    errorJson.message = string.Format("Could not find the {0} database", name);
                            //    errorJson.code = (int)HttpStatusCode.NotFound;
                            //    errorJson.helpLink = "http://en.wikipedia.org/wiki/HTTP_404"; // TODO

                            //    return new Response() { Uncompressed = Starcounter.Internal.Web.HttpResponseBuilder.Slow.FromStatusHeadersAndStringContent((int)HttpStatusCode.NotFound, null, errorJson.ToString()) };
                            //}
                            //else {

                            //    // Return the database
                            //    resultJson.settings = new {
                            //        name = database.Name,
                            //        hostProcessId = database.Engine == null ? 0 : database.Engine.HostProcessId,
                            //        httpPort = database.Configuration.Runtime.DefaultUserHttpPort,
                            //        schedulerCount = database.Configuration.Runtime.SchedulerCount ?? Environment.ProcessorCount,
                            //        chunksNumber = database.Configuration.Runtime.ChunksNumber,
                            //        sqlAggregationSupport = database.Configuration.Runtime.SqlAggregationSupport
                            //    };

                            //    return new Response() { Uncompressed = Starcounter.Internal.Web.HttpResponseBuilder.Slow.FromStatusHeadersAndStringContent((int)HttpStatusCode.OK, null, resultJson.ToString()) };
                            //}
                        }
                        else if (response.StatusCode == (int)HttpStatusCode.Forbidden) {
                            String validationErrors = response.Body;
							return RESTUtility.JSON.CreateResponse(validationErrors.ToString(), (int)HttpStatusCode.Forbidden);
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
