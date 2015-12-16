using System;
using Codeplex.Data;
using Starcounter;
using Starcounter.Advanced.Configuration;
using Starcounter.Server.PublicModel;
using System.Net;
using System.Diagnostics;
using Starcounter.Internal;
using Starcounter.Advanced;
using Starcounter.Administrator.Server.Utilities;

namespace Starcounter.Administrator.Server.Handlers {
    internal static partial class StarcounterAdminAPI {

        /// <summary>
        /// Register Database Default Settings GET
        /// </summary>
        public static void DatabaseDefaultSettings_GET(ushort port, IServerRuntime server) {

            // Get default settings for database
            Handle.GET("/api/admin/settings/database", (Request req) => {

                try {

                    DatabaseConfiguration d = new DatabaseConfiguration();

                    DatabaseSettings databaseSettings = new DatabaseSettings();

                    ServerInfo serverInfo = Program.ServerInterface.GetServerInfo();

                    databaseSettings.Name = "myDatabase"; // TODO: Generate a unique default database name
                    databaseSettings.DefaultUserHttpPort = serverInfo.Configuration.DefaultDatabaseConfiguration.Runtime.DefaultUserHttpPort;
                    databaseSettings.SchedulerCount = serverInfo.Configuration.DefaultDatabaseConfiguration.Runtime.SchedulerCount ?? Environment.ProcessorCount;
                    databaseSettings.ChunksNumber = serverInfo.Configuration.DefaultDatabaseConfiguration.Runtime.ChunksNumber;

                    // TODO: this is a workaround to get the default dumpdirectory path (fix this in the public model api)
                    if (string.IsNullOrEmpty(serverInfo.Configuration.DefaultDatabaseConfiguration.Runtime.DumpDirectory)) {
                        //  By default, dump files are stored in ImageDirectory
                        databaseSettings.DumpDirectory = serverInfo.Configuration.DefaultDatabaseConfiguration.Runtime.ImageDirectory;
                    }
                    else {
                        databaseSettings.DumpDirectory = serverInfo.Configuration.DefaultDatabaseConfiguration.Runtime.DumpDirectory;
                    }

                    databaseSettings.TempDirectory = serverInfo.Configuration.DefaultDatabaseConfiguration.Runtime.TempDirectory;
                    databaseSettings.ImageDirectory = serverInfo.Configuration.DefaultDatabaseConfiguration.Runtime.ImageDirectory;
                    databaseSettings.TransactionLogDirectory = serverInfo.Configuration.DefaultDatabaseConfiguration.Runtime.TransactionLogDirectory;
                    databaseSettings.CollationFile = serverInfo.Configuration.DefaultDatabaseStorageConfiguration.CollationFile;
                    databaseSettings.FirstObjectID = (long) serverInfo.Configuration.DefaultDatabaseStorageConfiguration.FirstObjectID;
                    databaseSettings.LastObjectID = (long) serverInfo.Configuration.DefaultDatabaseStorageConfiguration.LastObjectID;

                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.OK, BodyBytes = databaseSettings.ToJsonUtf8() };
                }
                catch (Exception e) {
                    return RestUtils.CreateErrorResponse(e);
                }
            });
        }
    }
}
