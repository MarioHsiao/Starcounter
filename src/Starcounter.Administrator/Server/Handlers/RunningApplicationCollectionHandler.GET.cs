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
using System.Windows.Forms;
using System.Text;
using System.Reflection;
using System.ComponentModel;

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
                                            item.StringValue = arg;
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

            Handle.GET("/api/admin/openappdialog", (Request req) => {

                bool isLocal = IsLocalIpAddress(req.ClientIpAddress.ToString());
                if (isLocal == false) {
                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.Forbidden };
                }

                string selectedFiles;

                try {

                    // Execute an external program that will open a filedialog and lets the user pick a file.
                    // The picked file is written to the console window by the external program.
                    string rootPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                    string pickFileExecutable = Path.Combine(rootPath, "scadmin\\PickFileDialog.exe");

                    var process = new Process();
                    process.StartInfo.FileName = pickFileExecutable;
                    process.StartInfo.Arguments = "\"Select a Starcounter Application\"";
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.Start();

                    // Pick up the picked file from the console output.
                    selectedFiles = process.StandardOutput.ReadToEnd();

                    process.WaitForExit();
                }
                catch (Win32Exception) {
                    selectedFiles = "[]";
                }
                Response response = new Response();
                response.Body = selectedFiles;
                return response;
            });
        }

        /// <summary>
        /// Checks if a ip is on localhost
        /// </summary>
        /// <param name="ip"></param>
        /// <returns>true if ip is on local host</returns>
        public static bool IsLocalIpAddress(string ip) {
            try { // get host IP addresses
                IPAddress[] hostIPs = Dns.GetHostAddresses(ip);
                // get local IP addresses
                IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());

                // test if any host IP equals to any local IP or to localhost
                foreach (IPAddress hostIP in hostIPs) {
                    // is localhost
                    if (IPAddress.IsLoopback(hostIP)) return true;
                    // is local address
                    foreach (IPAddress localIP in localIPs) {
                        if (hostIP.Equals(localIP)) return true;
                    }
                }
            }
            catch { }
            return false;
        }
    }
}
