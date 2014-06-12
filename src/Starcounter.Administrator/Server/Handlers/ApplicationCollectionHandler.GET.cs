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
        /// Register Application GET
        /// </summary>
        public static void Application_GET() {

            // Get a list of all running Applications
            // Example response
            //{
            // "Items": [
            //      {
            //          "Uri": "http://example.com/api/executables/foo/foo.exe-123456789",
            //          "Path": "C:\\path\to\\the\\exe\\foo.exe",
            //          "ApplicationFilePath" : "C:\\optional\\path\to\\the\\input\\file.cs",
            //          "Name" : "Name of the application",
            //          "Description": "Implements the Foo module",
            //          "Arguments": [{"dummy":""}],
            //          "DefaultUserPort": 1,
            //          "ResourceDirectories": [{"dummy":""}],
            //          "WorkingDirectory": "C:\\path\\to\\default\\resource\\directory",
            //          "IsTool":false,
            //          "StartedBy": "Per Samuelsson, per@starcounter.com",
            //          "Engine": {
            //              "Uri": "http://example.com/api/executables/foo"
            //          },
            //          "RuntimeInfo": {
            //              "LoadPath": "\\relative\\path\\to\\weaved\\server\\foo.exe",
            //              "Started": "ISO-8601, e.g. 2013-04-25T06.24:32",
            //              "LastRestart": "ISO-8601, e.g. 2013-04-25T06.49:01"
            //          },
            //          "dummy": "bla"
            //      }
            //  ]
            //}
            Handle.GET("/api/admin/applications", (Request req) => {

                try {

                    IServerRuntime serverRuntime = RootHandler.Host.Runtime;
                    DatabaseInfo[] applicationDatabases = serverRuntime.GetDatabases();
                    var admin = RootHandler.API;

                    var result = new Executables();

                    foreach (DatabaseInfo databaseInfo in applicationDatabases) {

                        EngineInfo engineInfo = databaseInfo.Engine;

                        if (engineInfo != null && engineInfo.HostProcessId != 0) {

                            if (engineInfo.HostedApps != null) {
                                foreach (AppInfo appInfo in engineInfo.HostedApps) {
                                    var headers = new Dictionary<string, string>(1);
                                    var executable = ExecutableHandler.JSON.CreateRepresentationForArray(databaseInfo, appInfo, headers);

                                    if (appInfo.Arguments != null) {
                                        foreach (string arg in appInfo.Arguments) {
                                            var item = executable.Arguments.Add();
                                            item.dummy = arg;
                                        }
                                    }
                                    result.Items.Add(executable);
                                }
                            }
                        }

                    }

                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.OK, BodyBytes = result.ToJsonUtf8() };
                }
                catch (Exception e) {
                    return RestUtils.CreateErrorResponse(e);
                }
            });
        }
    }
}
