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
        /// This will retrive information from Appstore
        /// </summary>
        public static void AppStore_GET(string appStoreHost) {

            // Get a list of apps on AppStore
            // Example response
            //{
            // "Items": [
            //      {
            //          "Namespace": "CompanyName.TestApp",
            //          "Channel" : "Stable",
            //          "Version" : "1.0.0.0",
            //          "DisplayName": "appname1",
            //          "Company": "a company",
            //          "Description" : "Description of the application",
            //          "VersionDate": "2014-11-11T00:00:00.000Z",
            //          "RelativeStartUri" : "\myapp\start",
            //          "Size" : 1234,
            //          "ImageUri": "http://www.polyjuice.com:8585/images/zhbf0eqx.png",
            //          "Uri": "http://www.polyjuice.com:8585/appstore/apps/CompanyName.TestApp"
            //      }
            //  ]
            //}
            Handle.GET("/api/admin/appstore/apps", (Request req) => {

                try {

                    if (string.IsNullOrEmpty(appStoreHost)) {
                        ErrorResponse errorResponse = new ErrorResponse();
                        errorResponse.Text = string.Format("Configuration error, Unknown App Store host");
                        return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.ServiceUnavailable, BodyBytes = errorResponse.ToJsonUtf8() };
                    }

                    string uri = appStoreHost + "/appstore/apps";

                    // Get App Store items (external source)
                    Response response;
                    HandlerOptions opt = new HandlerOptions() { CallExternalOnly = true };
                    X.GET(uri, out response, null, 10000, opt);
                    if (response.StatusCode != (ushort)System.Net.HttpStatusCode.OK) {

                        ErrorResponse errorResponse = new ErrorResponse();
                        errorResponse.Text = string.Format("At the moment The App Store Service is not avaiable. Try again later.");
                        errorResponse.Helplink = appStoreHost;
                        return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.ServiceUnavailable, BodyBytes = errorResponse.ToJsonUtf8() };
                    }
                    Representations.JSON.AppStoreItems appStoreItems = new Representations.JSON.AppStoreItems();
                    appStoreItems.PopulateFromJson(response.Body);


                    // Get Installed applications
                    Representations.JSON.Applications installedApplications = new Representations.JSON.Applications();
                    Response installedAppsResponse;
                    X.GET("/api/admin/installed/apps", out installedAppsResponse, null, 10000);
                    if (installedAppsResponse.StatusCode == (ushort)System.Net.HttpStatusCode.OK) {
                        installedApplications.PopulateFromJson(installedAppsResponse.Body);
                    }


                    // Create response
                    Representations.JSON.Applications appStoreApplications = new Representations.JSON.Applications();

                    foreach (var item in appStoreItems.Items) {

                        var returnedItem = appStoreApplications.Items.Add();
                        returnedItem.IsInstalled = false;
                        returnedItem.IsNewVersionAvailable = false;

                        // Get Installed application
                        foreach (var installedItem in installedApplications.Items) {
                            if (installedItem.ID == item.ID) {
                                returnedItem.IsInstalled = true;
                                returnedItem.RelativeStartUri = installedItem.RelativeStartUri;
                                returnedItem.IsNewVersionAvailable = (new Version(item.Version)>new Version(installedItem.Version));
                                break;
                            }
                        }

                        returnedItem.ID = item.ID;
                        returnedItem.Namespace = item.Namespace;
                        returnedItem.Channel = item.Channel;
                        returnedItem.Version = item.Version;
                        returnedItem.DisplayName = item.DisplayName;
                        returnedItem.Description = item.Description;
                        returnedItem.VersionDate = item.VersionDate;
                        returnedItem.Size = item.Size;
                        returnedItem.ImageUri = item.ImageUri;
                        returnedItem.Url = item.Url;
                    }

                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.OK, BodyBytes = appStoreApplications.ToJsonUtf8() };

                    //return response;
                }
                catch (Exception e) {
                    return RestUtils.CreateErrorResponse(e);
                }
            });

            // Get app info from AppStore
            // Example response
            //      {
            //          "Namespace": "CompanyName.TestApp",
            //          "Channel" : "Stable",
            //          "Version" : "1.0.0.0",
            //          "DisplayName": "appname1",
            //          "Company": "a company",
            //          "Description" : "Description of the application",
            //          "VersionDate": "2014-11-11T00:00:00.000Z",
            //          "RelativeStartUri" : "\myapp\start",
            //          "Size" : 1234,
            //          "ImageUri": "http://www.polyjuice.com:8585/images/zhbf0eqx.png",
            //          "Uri": "http://www.polyjuice.com:8585/appstore/apps/CompanyName.TestApp"
            //      }
            Handle.GET("/api/admin/appstore/apps/{?}", (string nameSpace, Request req) => {

                try {

                    if (string.IsNullOrEmpty(appStoreHost)) {
                        ErrorResponse errorResponse = new ErrorResponse();
                        errorResponse.Text = string.Format("Configuration error, Unknown App Store host");
                        return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.ServiceUnavailable, BodyBytes = errorResponse.ToJsonUtf8() };
                    }

                    Representations.JSON.Applications appStoreItems = new Representations.JSON.Applications();

                    string uri = appStoreHost + "/appstore/apps" + nameSpace;

                    Response response;
                    HandlerOptions opt = new HandlerOptions() { CallExternalOnly = true };
                    X.GET(uri, out response, null, 10000, opt);
                    return response;
                }
                catch (Exception e) {
                    return RestUtils.CreateErrorResponse(e);
                }
            });
        }
    }
}
