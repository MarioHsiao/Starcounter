using System;
using Codeplex.Data;
using Starcounter;
using Starcounter.Advanced;
using Starcounter.Server.PublicModel;
using System.Net;
using System.Diagnostics;
using Starcounter.Internal.Web;
using Starcounter.Administrator.API.Utilities;
using Starcounter.Administrator.Server.Utilities;
using Starcounter.Advanced.Configuration;

namespace Starcounter.Administrator.Server.Handlers {
    internal static partial class StarcounterAdminAPI {

        /// <summary>
        /// Register Database PUT
        /// </summary>
        public static void Database_PUT(ushort port, IServerRuntime server) {

            // Update database settings
            Handle.PUT("/api/admin/databases/{?}/settings", (string name, DatabaseSettings settings, Request req) => {

                lock (LOCK) {

                    try {

                        // Validate
                        ValidationErrors validationErrors = RestUtils.GetValidationErrors(settings);
                        if (validationErrors.Items.Count > 0) {
                            return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.Forbidden, BodyBytes = validationErrors.ToJsonUtf8() };
                        }

                        // Get original database config
                        DatabaseConfiguration config = Program.ServerInterface.GetDatabaseConfiguration(name);
                        if (config == null) {
                            // Database not found
                            ErrorResponse errorResponse = new ErrorResponse();
                            errorResponse.Text = string.Format("Could not find the {0} database configuration", name);
                            errorResponse.Helplink = "http://en.wikipedia.org/wiki/HTTP_404"; // TODO
                            return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NotFound, BodyBytes = errorResponse.ToJsonUtf8() };
                        }

                        config.Runtime.DefaultUserHttpPort = (ushort)settings.DefaultUserHttpPort;
                        config.Runtime.SchedulerCount = (int)settings.SchedulerCount;
                        config.Runtime.ChunksNumber = (int)settings.ChunksNumber;
                        config.Runtime.PolyjuiceDatabaseFlag = settings.PolyjuiceDatabaseFlag;
                        config.Save();

                        // Get cloned config from public model and update it with original values
                        DatabaseInfo database = Program.ServerInterface.GetDatabaseByName(name);

                        if (database == null) {
                            // Database not found
                            ErrorResponse errorResponse = new ErrorResponse();
                            errorResponse.Text = string.Format("Could not find the {0} database", name);
                            errorResponse.Helplink = "http://en.wikipedia.org/wiki/HTTP_404"; // TODO
                            return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NotFound, BodyBytes = errorResponse.ToJsonUtf8() };
                        }

                        database.Configuration.Runtime.DefaultUserHttpPort = config.Runtime.DefaultUserHttpPort;
                        database.Configuration.Runtime.SchedulerCount = config.Runtime.SchedulerCount;
                        database.Configuration.Runtime.ChunksNumber = config.Runtime.ChunksNumber;
                        database.Configuration.Runtime.PolyjuiceDatabaseFlag = config.Runtime.PolyjuiceDatabaseFlag;
                        //database.Configuration.Save();

                        // Return new settings
                        database = Program.ServerInterface.GetDatabaseByName(name);
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
                }
            });
        }
    }
}
