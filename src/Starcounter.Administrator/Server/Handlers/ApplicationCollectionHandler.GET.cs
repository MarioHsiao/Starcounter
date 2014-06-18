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

            Handle.GET("/api/admin/openappdialog", (Request req) => {

                bool isLocal = IsLocalIpAddress(req.ClientIpAddress.ToString());
                if (isLocal == false) {
                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.Forbidden };
                }

                OpenFileDialog openFileDialog = new OpenFileDialog();

                openFileDialog.Filter = "Application files (*.exe)|*.exe|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;
                openFileDialog.CheckFileExists = true;
                openFileDialog.CheckPathExists = true;
                openFileDialog.Multiselect = false;

                Response response = new Response();

                string responsebody = "[";

                if (openFileDialog.ShowDialog() == DialogResult.OK) {

                    int cnt = 0;
                    foreach (string filename in openFileDialog.FileNames) {
                        if (cnt != 0) {
                            responsebody += ",";
                        }
                        responsebody += string.Format("{{\"file\":\"{0}\"}}", EscapeStringValue(filename));
                        cnt++;
                    }
                    response.StatusCode = (ushort)System.Net.HttpStatusCode.OK;
                }
                else {

                    response.StatusCode = (ushort)System.Net.HttpStatusCode.NotFound;
                }

                responsebody += "]";
                response.Body = responsebody;
 
                return response;
            });
        }

        public static bool IsLocalIpAddress(string host) {
            try { // get host IP addresses
                IPAddress[] hostIPs = Dns.GetHostAddresses(host);
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

        public static string EscapeStringValue(string value) {
            const char BACK_SLASH = '\\';
            const char SLASH = '/';
            const char DBL_QUOTE = '"';

            var output = new StringBuilder(value.Length);
            foreach (char c in value) {
                switch (c) {
                    case SLASH:
                        output.AppendFormat("{0}{1}", BACK_SLASH, SLASH);
                        break;

                    case BACK_SLASH:
                        output.AppendFormat("{0}{0}", BACK_SLASH);
                        break;

                    case DBL_QUOTE:
                        output.AppendFormat("{0}{1}", BACK_SLASH, DBL_QUOTE);
                        break;

                    default:
                        output.Append(c);
                        break;
                }
            }

            return output.ToString();
        }

    }
}
