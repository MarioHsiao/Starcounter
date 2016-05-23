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
using Starcounter.Administrator.API.Handlers;
using Starcounter.Server.Rest.Representations.JSON;
using Starcounter.Server.Rest;
using Starcounter.CommandLine;
using System.IO;
using Starcounter.Rest.ExtensionMethods;
using System.Collections.Generic;
using Starcounter.Administrator.Server.Utilities;
using Administrator.Server.Model;
using Administrator.Server.Managers;

namespace Starcounter.Administrator.Server.Handlers {
    internal static partial class StarcounterAdminAPI {

        /// <summary>
        /// Register Database GET
        /// </summary>
        public static void Database_GET(ushort port, IServerRuntime server) {

            Handle.GET("/api/admin/databases", (Request req) => {

                lock (ServerManager.ServerInstance) {

                    try {
                        DatabasesJson databasesJson = new DatabasesJson();

                        databasesJson.Items.Data = ServerManager.ServerInstance.Databases;

                        return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.OK, BodyBytes = databasesJson.ToJsonUtf8() };
                    }
                    catch (Exception e) {
                        return RestUtils.CreateErrorResponse(e);
                    }
                }
            });

            Handle.GET("/api/admin/databases/{?}", (string id, Request req) => {

                lock (ServerManager.ServerInstance) {

                    try {

                        DatabaseJson databaseJson = new DatabaseJson();

                        databaseJson.Data = ServerManager.ServerInstance.GetDatabase(id);
                        if (databaseJson.Data == null) {
                            return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NotFound };
                        }
                        return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.OK, BodyBytes = databaseJson.ToJsonUtf8() };
                    }
                    catch (Exception e) {
                        return RestUtils.CreateErrorResponse(e);
                    }
                }
            });
        }
    }
}
