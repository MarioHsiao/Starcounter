//using System;
//using Codeplex.Data;
//using Starcounter;
//using Starcounter.Advanced;
//using Starcounter.Server.PublicModel;
//using System.Net;
//using System.Diagnostics;
//using Starcounter.Internal;
//using Starcounter.Internal.Web;
//using Starcounter.Administrator.API.Utilities;
//using Starcounter.Administrator.API.Handlers;
//using Starcounter.Server.Rest.Representations.JSON;
//using Starcounter.Server.Rest;
//using Starcounter.CommandLine;
//using System.IO;
//using Starcounter.Rest.ExtensionMethods;
//using System.Collections.Generic;
//using Starcounter.Administrator.Server.Utilities;

//namespace Starcounter.Administrator.Server.Handlers {
//    internal static partial class StarcounterAdminAPI {

//        /// <summary>
//        /// Register Database GET
//        /// </summary>
//        public static void Database_GET(ushort port, IServerRuntime server) {

//            // Get a list of all databases with running status
//            //{
//            //  "Databases":[
//            //      {
//            //          "name":"tracker",
//            //          "uri":"http://headsutv19:8181/api/databases/tracker",
//            //          "HostUri":"http://headsutv19:8181/api/engines/tracker/db",
//            //          "running":true
//            //      }
//            //  ]
//            //}
//            Handle.GET("/api/admin/databases", (Request req) => {

//                try {

//                    var serverRuntime = RootHandler.Host.Runtime;
//                    var applicationDatabases = serverRuntime.GetDatabases();
//                    var admin = RootHandler.API;

//                    var result = new databases();

//                    //var result = new DatabaseCollection();
//                    foreach (DatabaseInfo databaseInfo in applicationDatabases) {
//                        var db = result.Databases.Add();
//                        db.name = databaseInfo.Name;
//                        db.uri = admin.Uris.Database.ToAbsoluteUri(databaseInfo.Name);
//                        db.engineUri = admin.Uris.Engine.ToAbsoluteUri(databaseInfo.Name);

//                        string uriTemplateDbProcess = RootHandler.API.Uris.Engine + "/db";
//                        db.databaseProcessUri = uriTemplateDbProcess.ToAbsoluteUri(databaseInfo.Name);

//                        string uriTemplateHostProcess = RootHandler.API.Uris.Engine + "/host";
//                        db.codeHostProcessUri = uriTemplateHostProcess.ToAbsoluteUri(databaseInfo.Name);

//                        // Get Database status
//                        EngineInfo engineInfo = databaseInfo.Engine;
//                        if (engineInfo != null && engineInfo.DatabaseProcessRunning) {
//                            db.running = true;
//                        }
//                    }

//                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.OK, BodyBytes = result.ToJsonUtf8() };
//                }
//                catch (Exception e) {
//                    return RestUtils.CreateErrorResponse(e);
//                }
//            });
//        }
//    }
//}
