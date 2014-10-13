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
using Administrator.Server.ApplicationContainer;

namespace Starcounter.Administrator.Server.Handlers {
    internal static partial class StarcounterAdminAPI {

        /// <summary>
        /// Register Application GET
        /// </summary>
        public static void InstalledApplication_POST(string appsRootFolder, string appStoreHost) {

            //
            // Install Application 
            //
            Handle.POST("/api/admin/installed/apps", (Request request) => {

                try {
                    string host = request["Host"];

                    if (string.IsNullOrEmpty(appStoreHost)) {
                        ErrorResponse errorResponse = new ErrorResponse();
                        errorResponse.Text = string.Format("Configuration error, Unknown App Store host");
                        return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.ServiceUnavailable, BodyBytes = errorResponse.ToJsonUtf8() };
                    }

                    Representations.JSON.Application appStoreApplication = new Representations.JSON.Application();
                    appStoreApplication.PopulateFromJson(request.Body);

                    // Download Application from AppStore host

                    string url = appStoreHost + "/appstore/apps/" + appStoreApplication.ID;

                    Response response;
                    HandlerOptions opt = new HandlerOptions() { CallExternalOnly = true };

                    // Get package from host
                    string headers = "Accept: application/octet-stream\r\n";
                    X.GET(url, out response, headers, 0, opt);
                    if (response.StatusCode >= 200 && response.StatusCode < 300) {

                        // Success
                        using (MemoryStream packageZip = new MemoryStream(response.BodyBytes)) {
                            Package.Install(url, packageZip, appsRootFolder, false);
                        }
                        return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.Created };
                    }
                    else {
                        // Error

                        ErrorResponse errorResponse = new ErrorResponse();
                        errorResponse.Text = string.Format("Failed to install application");

                        if (response.StatusCode == (ushort)System.Net.HttpStatusCode.ServiceUnavailable) {
                            errorResponse.Text += ", " + "Service Unavailable";
                        }
                        return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.ServiceUnavailable, BodyBytes = errorResponse.ToJsonUtf8() };
                    }
                }
                catch (Exception e) {
                    return RestUtils.CreateErrorResponse(e);
                }
            });
        }
    }
}
