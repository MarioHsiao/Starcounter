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
using Starcounter.Administrator.API.Handlers;
using Administrator.Server.Managers;
using Administrator.Server.Model;

namespace Starcounter.Administrator.Server.Handlers {
    internal static partial class StarcounterAdminAPI {

        /// <summary>
        /// Register Database POST
        /// </summary>
        public static void Database_POST(ushort port, IServerRuntime server) {

            // Create database
            Handle.POST("/api/admin/databases", (DatabaseSettings settings, Request req) => {

                lock (ServerManager.ServerInstance) {

                    try {

                        ValidationErrors validationErrors = RestUtils.GetValidationErrors(settings);

                        if (validationErrors.Items.Count > 0) {
                            return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.Forbidden, BodyBytes = validationErrors.ToJsonUtf8() };
                        }

                        var command = new CreateDatabaseCommand(Program.ServerEngine, settings.Name) {
                            EnableWaiting = true
                        };

                        command.SetupProperties.Configuration.Runtime.DefaultUserHttpPort = (ushort)settings.DefaultUserHttpPort;
                        command.SetupProperties.Configuration.Runtime.SchedulerCount = (int)settings.SchedulerCount;
                        command.SetupProperties.Configuration.Runtime.ChunksNumber = (int)settings.ChunksNumber;
                        command.SetupProperties.StorageConfiguration.CollationFile = settings.CollationFile;
                    command.SetupProperties.StorageConfiguration.FirstObjectID = settings.FirstObjectID;
                    command.SetupProperties.StorageConfiguration.LastObjectID = settings.LastObjectID;

                        command.SetupProperties.Configuration.Runtime.DumpDirectory = settings.DumpDirectory;
                        command.SetupProperties.Configuration.Runtime.TempDirectory = settings.TempDirectory;
                        command.SetupProperties.Configuration.Runtime.ImageDirectory = settings.ImageDirectory;
                        command.SetupProperties.Configuration.Runtime.TransactionLogDirectory = settings.TransactionLogDirectory;

                        command.EnableWaiting = true;

                        var info = server.Execute(command);
                        info = server.Wait(info);
                        if (info.HasError) {

                            ErrorInfo single = info.Errors.PickSingleServerError();
                            var msg = single.ToErrorMessage();

                            ErrorResponse errorResponse = new ErrorResponse();
                            errorResponse.Text = msg.Brief;
                            errorResponse.StackTrace = string.Empty;
                            errorResponse.Helplink = msg.Helplink;
                            errorResponse.ServerCode = single.GetErrorCode();

                            if (single.GetErrorCode() == Error.SCERRDATABASEALREADYEXISTS) {
                                return new Response() { StatusCode = (ushort)422, Body = errorResponse.ToJson() };
                            }

                            return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.InternalServerError, Body = errorResponse.ToJson() };
                        }

                        ServerManager.ServerInstance.InvalidateDatabases();

                        Database database = ServerManager.ServerInstance.GetDatabase(settings.Name);
                        if (database != null) {
                            DatabaseJson databaseJson = new DatabaseJson();
                            databaseJson.Data = database;
                            return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.Created, Body = databaseJson.ToJson() };
                        }

                        ErrorResponse errorResponse2 = new ErrorResponse();
                        errorResponse2.Text = "Failed to find the created database";

                        return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.InternalServerError, Body = errorResponse2.ToJson() };
                    }
                    catch (Exception e) {
                        return RestUtils.CreateErrorResponse(e);
                    }
                }
            });
        }
    }
}
