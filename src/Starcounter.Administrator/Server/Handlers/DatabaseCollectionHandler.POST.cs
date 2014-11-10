﻿using System;
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

namespace Starcounter.Administrator.Server.Handlers {
    internal static partial class StarcounterAdminAPI {

        /// <summary>
        /// Register Database POST
        /// </summary>
        public static void Database_POST(ushort port, IServerRuntime server) {

            // Create database
            Handle.POST("/api/admin/databases", (DatabaseSettings settings, Request req) => {

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

                    command.SetupProperties.Configuration.Runtime.DumpDirectory = settings.DumpDirectory;
                    command.SetupProperties.Configuration.Runtime.TempDirectory = settings.TempDirectory;
                    command.SetupProperties.Configuration.Runtime.ImageDirectory = settings.ImageDirectory;
                    command.SetupProperties.Configuration.Runtime.TransactionLogDirectory = settings.TransactionLogDirectory;

                    command.EnableWaiting = true;

                    var info = server.Execute(command);
                    info = server.Wait(info);
                    if (info.HasError) {
                        return DatabaseCollectionHandler.ToErrorResponse(info);
                    }

                    // TODO: Return the new Created Database
                    // At the moment we only return the database name

                    dynamic resultJson = new DynamicJson();
                    resultJson.name = settings.Name.ToLower();
                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.Created, Body = resultJson.ToString() };

                }
                catch (Exception e) {
                    return RestUtils.CreateErrorResponse(e);
                }
            });
        }
    }
}
