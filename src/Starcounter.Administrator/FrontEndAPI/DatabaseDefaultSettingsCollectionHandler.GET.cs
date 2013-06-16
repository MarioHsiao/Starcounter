using System;
using Codeplex.Data;
using Starcounter;
using Starcounter.Advanced;
using Starcounter.Server.PublicModel;
using System.Net;
using System.Diagnostics;
using Starcounter.Internal;

namespace Starcounter.Administrator.FrontEndAPI {
    internal static partial class FrontEndAPI {

        public static void DatabaseDefaultSettings_GET(ushort port, IServerRuntime server) {


            Handle.GET("/api/admin/settings/database", (Request req) => {

                lock (LOCK) {

                    try {

                        Starcounter.Configuration.DatabaseConfiguration d = new Starcounter.Configuration.DatabaseConfiguration();

                        ServerInfo serverInfo = Master.ServerInterface.GetServerInfo();

                        dynamic json = new DynamicJson();

                        json.settings = new { };

                        json.settings.name = "myDatabase"; // TODO: Generate a unique default database name
                        json.settings.httpPort = serverInfo.Configuration.DefaultDatabaseConfiguration.Runtime.DefaultUserHttpPort;
                        json.settings.schedulerCount = serverInfo.Configuration.DefaultDatabaseConfiguration.Runtime.SchedulerCount ?? Environment.ProcessorCount;

                        json.settings.chunksNumber = serverInfo.Configuration.DefaultDatabaseConfiguration.Runtime.ChunksNumber;

                        // TODO: this is a workaround to get the default dumpdirectory path (fix this in the public model api)
                        if (string.IsNullOrEmpty(serverInfo.Configuration.DefaultDatabaseConfiguration.Runtime.DumpDirectory)) {
                            //  By default, dump files are stored in ImageDirectory
                            json.settings.dumpDirectory = serverInfo.Configuration.DefaultDatabaseConfiguration.Runtime.ImageDirectory;
                        }
                        else {
                            json.settings.dumpDirectory = serverInfo.Configuration.DefaultDatabaseConfiguration.Runtime.DumpDirectory;
                        }

                        json.settings.tempDirectory = serverInfo.Configuration.DefaultDatabaseConfiguration.Runtime.TempDirectory;
                        json.settings.imageDirectory = serverInfo.Configuration.DefaultDatabaseConfiguration.Runtime.ImageDirectory;
                        json.settings.transactionLogDirectory = serverInfo.Configuration.DefaultDatabaseConfiguration.Runtime.TransactionLogDirectory;

                        json.settings.sqlAggregationSupport = serverInfo.Configuration.DefaultDatabaseConfiguration.Runtime.SqlAggregationSupport;
                        //json.sqlProcessPort = serverInfo.Configuration.DefaultDatabaseConfiguration.Runtime.SQLProcessPort;
                        json.settings.collationFile = serverInfo.Configuration.DefaultDatabaseStorageConfiguration.CollationFile;

                        json.settings.collationFiles = new object[] { };

                        // TODO: Extend the Public model api to be able to retrive a list of all available collation files
                        json.settings.collationFiles[0] = new { name = Starcounter.Internal.StarcounterEnvironment.FileNames.CollationFileNamePrefix + "_en-GB_3.dll", description = "English" };
                        json.settings.collationFiles[1] = new { name = Starcounter.Internal.StarcounterEnvironment.FileNames.CollationFileNamePrefix + "_sv-SE_3.dll", description = "Swedish" };
                        json.settings.collationFiles[2] = new { name = Starcounter.Internal.StarcounterEnvironment.FileNames.CollationFileNamePrefix + "_nb-NO_3.dll", description = "Norwegian" };

                        json.settings.maxImageSize = serverInfo.Configuration.DefaultDatabaseStorageConfiguration.MaxImageSize ?? -1;
                        json.settings.supportReplication = serverInfo.Configuration.DefaultDatabaseStorageConfiguration.SupportReplication;
                        json.settings.transactionLogSize = serverInfo.Configuration.DefaultDatabaseStorageConfiguration.TransactionLogSize ?? -1;

                        return json.ToString();

                    }
                    catch (Exception e) {
                        return RestUtils.CreateErrorResponse(e);
                    }
                }

            });





        }
    }
}
