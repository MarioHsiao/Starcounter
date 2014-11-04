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
        /// Register GET Handler for retriving Remote App Store Applications
        /// </summary>
        public static void AppStore_GET(ushort port, string appStoreHost) {

            // Get Remote App Store Applications
            // Example response
            // {
            //  "Items": [
            //      {
            //          "ID": "DDD80897", 
            //          "Url" : "http://192.168.60.104:8181/api/admin/appstore/apps/DDD80897",
            //          "SourceID" :"DDD80897",
            //          "SourceUrl" : "http://127.0.0.1:8585/appstore/apps/DDD80897",
            //          "Namespace": "CompanyName.TestApp", 
            //          "Channel" : "Stable",
            //          "Version" : "1.0.0.0",
            //          "DisplayName": "TestApp", 
            //          "Description": "Description of the application",
            //          "Company" : "Test Company",
            //          "VersionDate" : "2014-11-11T00:00:00.000Z",
            //          "Executable" : "",
            //          "ResourceFolder" : "",
            //          "Size" : 0,
            //          "ImageUri" : "http://192.168.60.104:8585/images/ovxzb3ze.png",
            //          "IsInstalled" : false,
            //          "NewVersionAvailable" : true,
            //          "LatestVersion": {
            //              "ID" : "F2E43896",
            //              "Url" : "http://127.0.0.1:8585/appstore/apps/F2E43896"
            //          }
            //      }
            //  ]
            // }
            Handle.GET(port, "/api/admin/appstore/apps", (Request req) => {

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
                    Representations.JSON.RemoteAppStoreItems remoteAppStoreItems = new Representations.JSON.RemoteAppStoreItems();
                    remoteAppStoreItems.PopulateFromJson(response.Body);


                    // Get Installed applications
                    Representations.JSON.InstalledApplications installedApplications = new Representations.JSON.InstalledApplications();
                    Response installedAppsResponse;
                    X.GET("http://127.0.0.1:"+port+"/api/admin/installed/apps", out installedAppsResponse, null, 10000);
                    if (installedAppsResponse.StatusCode == (ushort)System.Net.HttpStatusCode.OK) {
                        installedApplications.PopulateFromJson(installedAppsResponse.Body);
                    }


                    // Create response
                    Representations.JSON.AppStoreApplications appStoreApplications = new Representations.JSON.AppStoreApplications();

                    foreach (var remoteStore in remoteAppStoreItems.Stores) {

                        if (remoteStore.Items.Count == 0) continue;

                        var appStore = appStoreApplications.Stores.Add();
                        appStore.ID = remoteStore.ID; // NOTE: This is the remote appstore id
                        appStore.DisplayName = remoteStore.DisplayName;

                        foreach (var remoteItem in remoteStore.Items) {

                            Representations.JSON.AppStoreApplication appStoreItem = appStore.Items.Add();
                            Representations.JSON.InstalledApplication installedItem;
                            GetInstalledApplication(remoteItem, installedApplications, out installedItem);

                            if (installedItem != null) {
                                appStoreItem.IsInstalled = true;
                                appStoreItem.ID = installedItem.ID;
                                //appStoreItem.ID = installedItem.ID;
                                //string ip = Utilities.RestUtils.GetMachineIp(); // TTODO: User "Host" header?
                                //appStoreItem.Url = string.Format("http://{0}:{1}/{2}/{3}", ip, port, "api/admin/appstore/apps", appStoreItem.InstalledID);
                            }
                            else {
                                // TEMP ID!
                                appStoreItem.ID = string.Format("{0:X8}", (appStoreHost + remoteStore.ID + remoteItem.ID).GetHashCode());
                            }

                            // TODO: Generate the ID, if we used multiple appstores we need to make sure the ID is unique
                            //appStoreItem.ID = string.Format("{0:X8}", (appStoreHost + remoteStore.ID + remoteItem.ID).GetHashCode());

                            if (remoteItem.NewVersionAvailable) {
                                appStoreItem.LatestVersion.ID = remoteItem.LatestVersion.ID;    // NOTE: This is a remote appstore id
                                appStoreItem.LatestVersion.Url = remoteItem.LatestVersion.Url;
                                appStoreItem.NewVersionAvailable = remoteItem.NewVersionAvailable;
                                if (appStoreItem.IsInstalled) {
                                    appStoreApplications.Updates++;
                                    appStore.Updates++;
                                }
                            }

                            appStoreItem.SourceID = remoteItem.ID;
                            appStoreItem.SourceUrl = remoteItem.Url;

                            appStoreItem.Namespace = remoteItem.Namespace;
                            appStoreItem.Channel = remoteItem.Channel;
                            appStoreItem.Version = remoteItem.Version;
                            appStoreItem.DisplayName = remoteItem.DisplayName;
                            appStoreItem.Description = remoteItem.Description;
                            appStoreItem.VersionDate = remoteItem.VersionDate;
                            appStoreItem.Size = remoteItem.Size;
                            appStoreItem.ImageUrl = remoteItem.ImageUrl;
                        }
                    }
                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.OK, BodyBytes = appStoreApplications.ToJsonUtf8() };
                }
                catch (Exception e) {
                    return RestUtils.CreateErrorResponse(e);
                }
            });

            // Get app info from AppStore
            // Example response
            //      {
            //          "ID": "", 
            //          "Url" : "",
            //          "SourceID" :"",
            //          "SourceUrl" : "www.polyjuice.com:8585/appstore/apps/CompanyName.TestApp",
            //          "Namespace": "CompanyName.TestApp", 
            //          "Channel" : "Stable",
            //          "Version" : "1.0.0.0",
            //          "DisplayName": "TestApp", 
            //          "Description": "Description of the application",
            //          "Company" : "",
            //          "VersionDate" : "2014-11-11T00:00:00.000Z",
            //          "Executable" : "",
            //          "ResourceFolder" : "",
            //          "Size" : 0,
            //          "ImageUri" : "www.polyjuice.com:8585/images/zhbf0eqx.png",
            //          "IsInstalled" : false,
            //          "NewVersionAvailable" : false,
            //          "LatestVersion": {
            //              "ID" : "",
            //              "Url" : ""
            //          }
            //      }
            //Handle.GET(port, "/api/admin/appstore/apps/{?}", (string id, Request req) => {

            //    try {

            //        if (string.IsNullOrEmpty(appStoreHost)) {
            //            ErrorResponse errorResponse = new ErrorResponse();
            //            errorResponse.Text = string.Format("Configuration error, Unknown App Store host");
            //            return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.ServiceUnavailable, BodyBytes = errorResponse.ToJsonUtf8() };
            //        }

            //        // Get items
            //        string uri = "http://127.0.0.1:" + port + "/api/admin/appstore/apps";
            //        Response response;
            //        X.GET(uri, out response, null, 10000);
            //        if (response.StatusCode != (ushort)System.Net.HttpStatusCode.OK) {
            //            return new Response() { StatusCode = response.StatusCode, BodyBytes = response.BodyBytes };
            //        }

            //        Representations.JSON.AppStoreApplications appStoreItems = new Representations.JSON.AppStoreApplications();
            //        appStoreItems.PopulateFromJson(response.Body);

            //        foreach (var appStore in appStoreItems.Stores) {

            //            foreach (var appStoreItem in appStore.Items) {

            //                if (appStoreItem.ID == id) {
            //                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.OK, BodyBytes = appStoreItem.ToJsonUtf8() };
            //                }
            //            }
            //        }

            //        return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NotFound };
            //    }
            //    catch (Exception e) {
            //        return RestUtils.CreateErrorResponse(e);
            //    }
            //});
        }

        /// <summary>
        /// Get the installed applications from a remote application
        /// If remote applications is not installed, the outcome will be null
        /// otherwice the installed applications will be returned
        /// </summary>
        /// <param name="remoteItem"></param>
        /// <param name="installedApplications"></param>
        /// <param name="item"></param>
        private static void GetInstalledApplication(Representations.JSON.RemoteAppStoreItem remoteItem, Representations.JSON.InstalledApplications installedApplications, out Representations.JSON.InstalledApplication item) {

            item = null;

            foreach (Representations.JSON.InstalledApplication installedItem in installedApplications.Items) {

                if (remoteItem.ID == installedItem.SourceID) {
                    item = installedItem;
                    break;
                }

                //if (remoteItem.Namespace == installedItem.Namespace && remoteItem.VersionDate == installedItem.VersionDate) {
                //    item = installedItem;
                //    break;
                //}
            }
        }
    }
}
