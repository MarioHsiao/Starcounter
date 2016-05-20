using Administrator.Server.Managers;
using Administrator.Server.Model;
using Starcounter.Administrator.Server.Utilities;
using System;
using System.Collections.Generic;

namespace Starcounter.Administrator.Server.Handlers {
    internal static partial class StarcounterAdminAPI {

        /// <summary>
        /// Register GET Handler for retriving Remote App Store Applications
        /// </summary>
        public static void AppStore_GET(ushort port) {

            // Get stores
            Handle.GET(port, "/api/admin/databases/{?}/appstore/stores", (string databaseName, Request req) => {

                lock (ServerManager.ServerInstance) {
                    try {
                        Database database = ServerManager.ServerInstance.GetDatabase(databaseName);
                        if (database == null) {
                            return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NotFound };
                        }

                        string msg;
                        IList<AppStoreStore> stores = AppStoreManager.GetStores(out msg);
                        if (stores == null) {
                            ErrorResponse errorResponse = new ErrorResponse();
                            errorResponse.Text = msg;
                            errorResponse.Helplink = string.Empty;
                            return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.ServiceUnavailable, BodyBytes = errorResponse.ToJsonUtf8() };
                        }

                        AppStoreStoresJson storesJson = new AppStoreStoresJson();
                        foreach (AppStoreStore store in stores) {
                            Representations.JSON.AppStoreStoreJson storeJson = new Representations.JSON.AppStoreStoreJson();
                            storeJson.ID = store.ID;
                            storeJson.DisplayName = store.DisplayName;
                            storeJson.Description = store.Description;
                            storeJson.Uri = req.Uri + "/" + store.ID;
                            storesJson.Items.Add(storeJson);
                        }

                        return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.OK, BodyBytes = storesJson.ToJsonUtf8() };
                    }
                    catch (Exception e) {
                        return RestUtils.CreateErrorResponse(e);
                    }
                }
            });

            // Get Store
            Handle.GET(port, "/api/admin/databases/{?}/appstore/stores/{?}", (string databaseName, string storeId, Request req) => {

                lock (ServerManager.ServerInstance) {
                    // Get database
                    Database database = ServerManager.ServerInstance.GetDatabase(databaseName);
                    if (database == null) {
                        return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NotFound };
                    }

                    // Find store
                    string msg;
                    IList<AppStoreStore> stores = AppStoreManager.GetStores(out msg);
                    if (stores == null) {

                        ErrorResponse errorResponse = new ErrorResponse();
                        errorResponse.Text = msg;
                        errorResponse.Helplink = string.Empty;
                        return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.ServiceUnavailable, BodyBytes = errorResponse.ToJsonUtf8() };
                    }

                    foreach (AppStoreStore store in stores) {
                        if (store.ID == storeId) {

                            // Found
                            Representations.JSON.AppStoreStoreJson storeJson = new Representations.JSON.AppStoreStoreJson();
                            storeJson.ID = store.ID;
                            storeJson.DisplayName = store.DisplayName;
                            storeJson.Description = store.Description;
                            storeJson.Uri = req.Uri;
                            return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.OK, BodyBytes = storeJson.ToJsonUtf8() };
                        }
                    }
                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NotFound };
                }
            });

            // Get store applications
            Handle.GET(port, "/api/admin/databases/{?}/appstore/stores/{?}/applications", (string databaseName, string storeId, Request req) => {

                lock (ServerManager.ServerInstance) {
                    // Get database
                    Database database = ServerManager.ServerInstance.GetDatabase(databaseName);
                    if (database == null) {

                        return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NotFound };
                    }

                    // Find store
                    string msg;
                    IList<AppStoreStore> stores = AppStoreManager.GetStores(out msg);
                    if (stores == null) {

                        ErrorResponse errorResponse = new ErrorResponse();
                        errorResponse.Text = msg;
                        errorResponse.Helplink = string.Empty;
                        return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.ServiceUnavailable, BodyBytes = errorResponse.ToJsonUtf8() };
                    }

                    foreach (AppStoreStore store in stores) {
                        if (store.ID == storeId) {

                            // Get applications
                            IList<AppStoreApplication> applications = AppStoreManager.GetApplications(store, out msg);
                            if (applications == null) {

                                ErrorResponse errorResponse = new ErrorResponse();
                                errorResponse.Text = msg;
                                errorResponse.Helplink = string.Empty;
                                return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.ServiceUnavailable, BodyBytes = errorResponse.ToJsonUtf8() };
                            }

                            Representations.JSON.AppStoreApplications result = new Representations.JSON.AppStoreApplications();

                            foreach (AppStoreApplication appStoreApplication in applications) {

                                Representations.JSON.AppStoreApplication app = new Representations.JSON.AppStoreApplication();

                                app.ID = appStoreApplication.ID;
                                app.SourceID = appStoreApplication.SourceID;
                                app.SourceUrl = appStoreApplication.SourceUrl;
                                app.StoreID = appStoreApplication.StoreID;
                                app.StoreUrl = appStoreApplication.StoreUrl;

                                app.Namespace = appStoreApplication.Namespace;
                                app.Channel = appStoreApplication.Channel;
                                app.Version = appStoreApplication.Version;
                                app.DisplayName = appStoreApplication.DisplayName;
                                app.Description = appStoreApplication.Description;
                                app.Company = appStoreApplication.Company;
                                app.Heading = appStoreApplication.Heading;
                                app.Rating = appStoreApplication.Rating;
                                app.VersionDate = appStoreApplication.VersionDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");

                                app.ImageUrl = appStoreApplication.ImageUri;
                                app.IsInstalled = appStoreApplication.IsDeployed;

                                // TODO:
                                app.Executable = appStoreApplication.Executable;
                                app.ResourceFolder = appStoreApplication.ResourceFolder;
                                app.Size = 0;

                                result.Items.Add(app);
                            }

                            return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.OK, BodyBytes = result.ToJsonUtf8() };
                        }
                    }

                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NotFound };
                }
            });

            Handle.GET(port, "/api/admin/databases/{?}/appstore/stores/{?}/applications/{?}", (string databaseName, string storeId, string appId, Request req) => {
                // TODO: Get store application
                return System.Net.HttpStatusCode.NotImplemented;
            });
        }
    }
}
