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

namespace Starcounter.Administrator.Server.Handlers {
    internal static partial class StarcounterAdminAPI {

        /// <summary>
        /// Register Database Settings GET
        /// </summary>
        public static void DatabaseSettings_GET(ushort port, IServerRuntime server) {

            // Get a database settings
            Handle.GET("/api/admin/databases/{?}/settings", (string name, Request req) => {

                try {

                    DatabaseInfo database = Program.ServerInterface.GetDatabaseByName(name);

                    if (database == null) {
                        // Database not found
                        ErrorResponse errorResponse = new ErrorResponse();
                        errorResponse.Text = string.Format("Could not find the {0} database", name);
                        errorResponse.Helplink = "http://en.wikipedia.org/wiki/HTTP_404"; // TODO
                        return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NotFound, BodyBytes = errorResponse.ToJsonUtf8() };
                    }

                    DatabaseSettings databaseSettings = RestUtils.CreateSettings(database);

                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.OK, BodyBytes = databaseSettings.ToJsonUtf8() };
                }
                catch (Exception e) {
                    return RestUtils.CreateErrorResponse(e);
                }
            });
        }
    }
}
