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

        public static void VerifyDatabaseProperties_POST(ushort port, IServerRuntime server) {


            #region Verify database properties (/adminapi/verify/databaseproperties)
            Handle.POST("/api/admin/verify/databaseproperties", (Request req) => {

                lock (LOCK) {

                    try {
                        dynamic resultJson = new DynamicJson();
                        resultJson.commandId = null;
                        resultJson.validationErrors = new object[] { };

                        // Validate settings
                        int validationErrors = 0;

                        // Getting POST contents.
                        String content = req.Body;

                        var incomingJson = DynamicJson.Parse(content);

                        #region Validate incoming json data
                        // Database name
                        if (incomingJson.IsDefined("name") == true && string.IsNullOrEmpty(incomingJson.name)) {
                            resultJson.validationErrors[validationErrors++] = new { property = "name", message = "invalid database name" };
                        }

                        // Port number
                        ushort httpPort;
                        if (incomingJson.IsDefined("httpPort") == true && (!ushort.TryParse(incomingJson.httpPort.ToString(), out httpPort) || httpPort < IPEndPoint.MinPort || httpPort > IPEndPoint.MaxPort)) {
                            resultJson.validationErrors[validationErrors++] = new { property = "httpPort", message = "invalid port number" };
                        }

                        // Scheduler Count
                        int schedulerCount;
                        if (incomingJson.IsDefined("schedulerCount") == true && !int.TryParse(incomingJson.schedulerCount.ToString(), out schedulerCount)) {
                            resultJson.validationErrors[validationErrors++] = new { property = "schedulerCount", message = "invalid scheduler count" };
                        }

                        // Chunks Number
                        int chunksNumber;
                        if (incomingJson.IsDefined("chunksNumber") == true && !int.TryParse(incomingJson.chunksNumber.ToString(), out chunksNumber)) {
                            resultJson.validationErrors[validationErrors++] = new { property = "chunksNumber", message = "invalid chunks number" };
                        }

                        // Dump Directory
                        //if (incomingJson.IsDefined("dumpDirectory") == true && string.IsNullOrEmpty(incomingJson.dumpDirectory)) {
                        //    resultJson.validationErrors[validationErrors++] = new { property = "dumpDirectory", message = "invalid dump directory" };
                        //}

                        // Temp Directory
                        if (incomingJson.IsDefined("tempDirectory") == true && string.IsNullOrEmpty(incomingJson.tempDirectory)) {
                            resultJson.validationErrors[validationErrors++] = new { property = "tempDirectory", message = "invalid temp directory" };
                        }

                        // Image Directory
                        if (incomingJson.IsDefined("imageDirectory") == true && string.IsNullOrEmpty(incomingJson.imageDirectory)) {
                            resultJson.validationErrors[validationErrors++] = new { property = "imageDirectory", message = "invalid image directory" };
                        }

                        // Log Directory
                        if (incomingJson.IsDefined("transactionLogDirectory") == true && string.IsNullOrEmpty(incomingJson.transactionLogDirectory)) {
                            resultJson.validationErrors[validationErrors++] = new { property = "transactionLogDirectory", message = "invalid transaction log directory" };
                        }

                        // SQL Aggregation support
                        bool sqlAggregationSupport;
                        if (incomingJson.IsDefined("sqlAggregationSupport") == true && !bool.TryParse(incomingJson.sqlAggregationSupport.ToString(), out sqlAggregationSupport)) {
                            resultJson.validationErrors[validationErrors++] = new { property = "sqlAggregationSupport", message = "invalid SQL Aggregation support" };
                        }

                        // Collation File
                        if (incomingJson.IsDefined("collationFile") == true && string.IsNullOrEmpty(incomingJson.collationFile)) {
                            resultJson.validationErrors[validationErrors++] = new { property = "collationFile", message = "invalid collation file" };
                        }

                        // maxImageSize
                        long maxImageSize;
                        if (incomingJson.IsDefined("maxImageSize") ==  true && !long.TryParse(incomingJson.maxImageSize.ToString(), out maxImageSize)) {
                            resultJson.validationErrors[validationErrors++] = new { property = "maxImageSize", message = "invalid max image size" };
                        }

                        // supportReplication
                        //bool supportReplication;
                        //if (incomingJson.IsDefined("supportReplication") == true && !bool.TryParse(incomingJson.supportReplication.ToString(), out supportReplication)) {
                        //    resultJson.validationErrors[validationErrors++] = new { property = "supportReplication", message = "invalid support replication" };
                        //}

                        // transactionLogSize
                        //long transactionLogSize;
                        //if (incomingJson.IsDefined("transactionLogSize") == true && !long.TryParse(incomingJson.transactionLogSize.ToString(), out transactionLogSize)) {
                        //    resultJson.validationErrors[validationErrors++] = new { property = "transactionLogSize", message = "invalid transaction log size" };
                        //}

                        #endregion

                        return new Response() { Uncompressed = HttpResponseBuilder.Slow.FromStatusHeadersAndStringContent((int)HttpStatusCode.OK, null, resultJson.ToString()) };

                    }
                    catch (Exception e) {
                        return RestUtils.CreateErrorResponse(e);
                    }

                }

            });
            #endregion


        }

    }
}
