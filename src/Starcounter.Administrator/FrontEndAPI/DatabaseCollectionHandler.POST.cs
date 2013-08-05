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

namespace Starcounter.Administrator.FrontEndAPI {
    internal static partial class FrontEndAPI {

        public static void Database_POST(ushort port, IServerRuntime server) {

            Handle.POST("/api/admin/databases/{?}/createdatabase", (string name, Request req) => {

                lock (LOCK) {

                    try {

                        String content = req.GetBodyStringUtf8_Slow();

                        Response response = Node.LocalhostSystemPortNode.POST("/api/admin/verify/databaseproperties", content, null, null);

                        if (response.StatusCode >= 200 && response.StatusCode < 300) {

                            dynamic incomingJson = DynamicJson.Parse(content);

                            DatabaseInfo database = Master.ServerInterface.GetDatabaseByName(name);


                            dynamic resultJson = new DynamicJson();

                            var command = new CreateDatabaseCommand(Master.ServerEngine, incomingJson.name) {
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

                            // ..

                            // maxImageSize
                            long maxImageSize = 0;
                            if (incomingJson.IsDefined("maxImageSize") == true && long.TryParse(incomingJson.maxImageSize.ToString(), out maxImageSize)) {
                                command.SetupProperties.StorageConfiguration.MaxImageSize = maxImageSize;
                            }

                            // supportReplication
                            //bool supportReplication = false;
                            //if (incomingJson.IsDefined("supportReplication") == true && bool.TryParse(incomingJson.supportReplication.ToString(), out supportReplication)) {
                            //    command.SetupProperties.StorageConfiguration.SupportReplication = supportReplication;
                            //}

                            // transactionLogSize
                            //long transactionLogSize = 0;
                            //if (incomingJson.IsDefined("transactionLogSize") == true && long.TryParse(incomingJson.transactionLogSize.ToString(), out transactionLogSize)) {
                            //    command.SetupProperties.StorageConfiguration.TransactionLogSize = transactionLogSize;
                            //}

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

                            return new Response() { Uncompressed = HttpResponseBuilder.Slow.FromStatusHeadersAndStringContent((int)HttpStatusCode.OK, null, resultJson.ToString()) };





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


//        try {
//            dynamic resultJson = new DynamicJson();
//            resultJson.commandId = null;
//            resultJson.validationErrors = new object[] { };

//            // Validate settings
//            int validationErrors = 0;

//            // Getting POST contents.
//            String content = req.GetBodyStringUtf8_Slow();

//            var incomingJson = DynamicJson.Parse(content);

//            #region Validate incoming json data
//            // Database name
//            if (string.IsNullOrEmpty(incomingJson.name)) {
//                resultJson.validationErrors[validationErrors++] = new { property = "name", message = "invalid database name" };
//            }

//            // Port number
//            ushort port;
//            if (!ushort.TryParse(incomingJson.httpPort.ToString(), out port) || port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort) {
//                resultJson.validationErrors[validationErrors++] = new { property = "httpPort", message = "invalid port number" };
//            }

//            // Scheduler Count
//            int schedulerCount;
//            if (!int.TryParse(incomingJson.schedulerCount.ToString(), out schedulerCount)) {
//                resultJson.validationErrors[validationErrors++] = new { property = "schedulerCount", message = "invalid scheduler count" };
//            }

//            // Chunks Number
//            int chunksNumber;
//            if (!int.TryParse(incomingJson.chunksNumber.ToString(), out chunksNumber)) {
//                resultJson.validationErrors[validationErrors++] = new { property = "chunksNumber", message = "invalid chunks number" };
//            }

//            // Dump Directory
//            if (string.IsNullOrEmpty(incomingJson.dumpDirectory)) {
//                resultJson.validationErrors[validationErrors++] = new { property = "dumpDirectory", message = "invalid dump directory" };
//            }

//            // Temp Directory
//            if (string.IsNullOrEmpty(incomingJson.tempDirectory)) {
//                resultJson.validationErrors[validationErrors++] = new { property = "tempDirectory", message = "invalid temp directory" };
//            }

//            // Image Directory
//            if (string.IsNullOrEmpty(incomingJson.imageDirectory)) {
//                resultJson.validationErrors[validationErrors++] = new { property = "imageDirectory", message = "invalid image directory" };
//            }

//            // Log Directory
//            if (string.IsNullOrEmpty(incomingJson.transactionLogDirectory)) {
//                resultJson.validationErrors[validationErrors++] = new { property = "transactionLogDirectory", message = "invalid transaction log directory" };
//            }

//            // SQL Aggregation support
//            bool sqlAggregationSupport;
//            if (!bool.TryParse(incomingJson.sqlAggregationSupport.ToString(), out sqlAggregationSupport)) {
//                resultJson.validationErrors[validationErrors++] = new { property = "sqlAggregationSupport", message = "invalid SQL Aggregation support" };
//            }

//            // sqlProcessPort
//            //ushort sqlProcessPort;
//            //if (!ushort.TryParse(incomingJson.sqlProcessPort.ToString(), out sqlProcessPort) || port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort) {
//            //    resultJson.validationErrors[validationErrors++] = new { property = "sqlProcessPort", message = "invalid port number" };
//            //}

//            // Collation File
//            if (string.IsNullOrEmpty(incomingJson.collationFile)) {
//                resultJson.validationErrors[validationErrors++] = new { property = "collationFile", message = "invalid collation file" };
//            }

//            // maxImageSize
//            long maxImageSize;
//            if (!long.TryParse(incomingJson.maxImageSize.ToString(), out maxImageSize)) {
//                resultJson.validationErrors[validationErrors++] = new { property = "maxImageSize", message = "invalid max image size" };
//            }

//            // supportReplication
//            bool supportReplication;
//            if (!bool.TryParse(incomingJson.supportReplication.ToString(), out supportReplication)) {
//                resultJson.validationErrors[validationErrors++] = new { property = "supportReplication", message = "invalid support replication" };
//            }

//            // transactionLogSize
//            long transactionLogSize;
//            if (!long.TryParse(incomingJson.transactionLogSize.ToString(), out transactionLogSize)) {
//                resultJson.validationErrors[validationErrors++] = new { property = "transactionLogSize", message = "invalid transaction log size" };
//            }

//            #endregion

//            if (validationErrors == 0) {
//                resultJson.Delete("validationErrors"); // Cleanup, remove the validationErrors property from the resultJson (it's empty = no need for it)

//                var command = new CreateDatabaseCommand(Master.ServerEngine, incomingJson.name);
//                command.SetupProperties.Configuration.Runtime.DefaultUserHttpPort = port;
//                command.SetupProperties.Configuration.Runtime.SchedulerCount = schedulerCount;
//                command.SetupProperties.Configuration.Runtime.ChunksNumber = chunksNumber;

//                command.SetupProperties.Configuration.Runtime.DumpDirectory = incomingJson.dumpDirectory;
//                command.SetupProperties.Configuration.Runtime.TempDirectory = incomingJson.tempDirectory;
//                command.SetupProperties.Configuration.Runtime.ImageDirectory = incomingJson.imageDirectory;
//                command.SetupProperties.Configuration.Runtime.TransactionLogDirectory = incomingJson.transactionLogDirectory;

//                command.SetupProperties.Configuration.Runtime.SqlAggregationSupport = sqlAggregationSupport;
//                //command.SetupProperties.Configuration.Runtime.SQLProcessPort = sqlProcessPort;

//                command.SetupProperties.StorageConfiguration.CollationFile = incomingJson.collationFile;

//                command.SetupProperties.StorageConfiguration.MaxImageSize = maxImageSize;
//                command.SetupProperties.StorageConfiguration.SupportReplication = supportReplication;
//                command.SetupProperties.StorageConfiguration.TransactionLogSize = transactionLogSize;

//                CommandInfo commandInfo = Master.ServerInterface.Execute(command);
//                resultJson.commandId = commandInfo.Id.Value;

//                return new Response() { Uncompressed = Starcounter.Internal.Web.HttpResponseBuilder.Slow.FromStatusHeadersAndStringContent((int)HttpStatusCode.Accepted, null, resultJson.ToString()) };
//            }
//            else {
//                // Validation errors
//                return new Response() { Uncompressed = Starcounter.Internal.Web.HttpResponseBuilder.Slow.FromStatusHeadersAndStringContent((int)HttpStatusCode.Forbidden, null, resultJson.ToString()) };
//            }

//        }
//        catch (Exception e) {
//            dynamic exceptionJson = new DynamicJson();

//            exceptionJson.message = e.Message;
//            exceptionJson.helpLink = e.HelpLink;
//            exceptionJson.stackTrace = e.StackTrace;
//            exceptionJson.code = (int)HttpStatusCode.InternalServerError;

//            return new Response() { Uncompressed = Starcounter.Internal.Web.HttpResponseBuilder.Slow.FromStatusHeadersAndStringContent((int)HttpStatusCode.InternalServerError, null, exceptionJson.ToString()) };
//        }
