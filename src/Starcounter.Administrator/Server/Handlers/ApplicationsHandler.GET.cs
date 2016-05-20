using Administrator.Server.Managers;
using Administrator.Server.Model;
using Starcounter.Administrator.Server.Utilities;
using Starcounter.Server.PublicModel;
using System;
using System.Collections.Generic;

namespace Starcounter.Administrator.Server.Handlers {
    internal static partial class StarcounterAdminAPI {

        /// <summary>
        /// Register Applications GET
        /// </summary>
        public static void Applications_GET(ushort port, string appImagesSubFolder) {

            // Get a list with all available local applications  (Deployed,Installed,Running)
            // Note: Remote AppStore applications is not included
            Handle.GET("/api/admin/databases/{?}/applications", (string databaseName, Request req) => {

                lock (ServerManager.ServerInstance) {

                    try {

                        Database database = ServerManager.ServerInstance.GetDatabase(databaseName);
                        if (database == null) {
                            // Database not found
                            ErrorResponse errorResponse = new ErrorResponse();
                            errorResponse.Text = string.Format("Could not find the {0} database", databaseName);
                            errorResponse.Helplink = "http://en.wikipedia.org/wiki/HTTP_404"; // TODO
                            return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NotFound, BodyBytes = errorResponse.ToJsonUtf8() };
                        }

                        DatabaseApplicationsJson result = new DatabaseApplicationsJson();
                        foreach (DatabaseApplication application in database.Applications) {
                            DatabaseApplicationJson appItem = application.ToDatabaseApplication();
                            result.Items.Add(appItem);

                        }

                        return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.OK, BodyBytes = result.ToJsonUtf8() };
                    }
                    catch (Exception e) {
                        return RestUtils.CreateErrorResponse(e);
                    }
                }
            });


            // Get application
            Handle.GET("/api/admin/databases/{?}/applications/{?}", (string databaseName, string id, Request req) => {

                lock (ServerManager.ServerInstance) {

                    try {

                        //DatabaseInfo database = Program.ServerInterface.GetDatabaseByName(databaseName);
                        Database database = ServerManager.ServerInstance.GetDatabase(databaseName);
                        if (database == null) {
                            // Database not found
                            ErrorResponse errorResponse = new ErrorResponse();
                            errorResponse.Text = string.Format("Failed to find the {0} database", databaseName);
                            errorResponse.Helplink = "http://en.wikipedia.org/wiki/HTTP_404"; // TODO
                            return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NotFound, BodyBytes = errorResponse.ToJsonUtf8() };
                        }

                        DatabaseApplication application = database.GetApplication(id);
                        if (application == null) {
                            // Application not found
                            ErrorResponse errorResponse = new ErrorResponse();
                            errorResponse.Text = string.Format("Failed to find the application, {0}", id);
                            errorResponse.Helplink = "http://en.wikipedia.org/wiki/HTTP_404"; // TODO
                            return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NotFound, BodyBytes = errorResponse.ToJsonUtf8() };
                        }
                        DatabaseApplicationJson result = new DatabaseApplicationJson();
                        result.Data = application;

                        return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.OK, BodyBytes = result.ToJsonUtf8() };
                    }
                    catch (Exception e) {
                        return RestUtils.CreateErrorResponse(e);
                    }
                }
            });

            // Get application <namespace/channel/version
            Handle.GET("/api/admin/databases/{?}/applications/{?}/{?}/{?}", (string databaseName, string nameSpace, string channel, string version, Request req) => {

                lock (ServerManager.ServerInstance) {
                    try {

                        Database database = ServerManager.ServerInstance.GetDatabase(databaseName);
                        if (database == null) {
                            // Database not found
                            ErrorResponse errorResponse = new ErrorResponse();
                            errorResponse.Text = string.Format("Failed to find the {0} database", databaseName);
                            errorResponse.Helplink = "http://en.wikipedia.org/wiki/HTTP_404"; // TODO
                            return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NotFound, BodyBytes = errorResponse.ToJsonUtf8() };
                        }

                        DatabaseApplication application = database.GetApplication(nameSpace, channel, version);
                        if (application == null) {
                            // Application not found
                            ErrorResponse errorResponse = new ErrorResponse();
                            errorResponse.Text = string.Format("Failed to find the application");
                            errorResponse.Helplink = "http://en.wikipedia.org/wiki/HTTP_404"; // TODO
                            return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NotFound, BodyBytes = errorResponse.ToJsonUtf8() };
                        }
                        DatabaseApplicationJson result = new DatabaseApplicationJson();
                        result.Data = application;

                        return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.OK, BodyBytes = result.ToJsonUtf8() };
                    }
                    catch (Exception e) {
                        return RestUtils.CreateErrorResponse(e);
                    }
                }
            });

            // Get applications <namespace/channel> (All version)
            Handle.GET("/api/admin/databases/{?}/applications/{?}/{?}", (string databaseName, string nameSpace, string channel, Request req) => {

                lock (ServerManager.ServerInstance) {
                    try {

                        Database database = ServerManager.ServerInstance.GetDatabase(databaseName);
                        if (database == null) {
                            // Database not found
                            ErrorResponse errorResponse = new ErrorResponse();
                            errorResponse.Text = string.Format("Failed to find the {0} database", databaseName);
                            errorResponse.Helplink = "http://en.wikipedia.org/wiki/HTTP_404"; // TODO
                            return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NotFound, BodyBytes = errorResponse.ToJsonUtf8() };
                        }

                        IList<DatabaseApplication> applications = database.GetApplications(nameSpace, channel);

                        DatabaseApplicationsJson result = new DatabaseApplicationsJson();

                        foreach (DatabaseApplication application in applications) {
                            DatabaseApplicationJson appItem = application.ToDatabaseApplication();
                            result.Items.Add(appItem);
                        }

                        return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.OK, BodyBytes = result.ToJsonUtf8() };
                    }
                    catch (Exception e) {
                        return RestUtils.CreateErrorResponse(e);
                    }
                }
            });
        }
    }
}
