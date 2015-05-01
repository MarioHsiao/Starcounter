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

       
            // Get remote appostore Applications
            // "/api/admin/appstore/apps"
            Handle.GET(port, "/api/admin/database/{?}/appstore/applications", (string databaseName, Request req) => {


                try {

                    Database database = ServerManager.ServerInstance.GetDatabase(databaseName);

                    Representations.JSON.AppStoreApplications result = new Representations.JSON.AppStoreApplications();

                    result.Updates = 0;

                    foreach (AppStoreStore store in database.AppStoreStores) {

                        var storeJson = result.Stores.Add();
                        storeJson.DisplayName = store.DisplayName;
                        storeJson.ID = store.ID;
                        storeJson.Updates = 0;

                        foreach (AppStoreApplication appStoreApplication in store.Applications) {

                            Representations.JSON.AppStoreApplication app = new Representations.JSON.AppStoreApplication();

                            app.ID = appStoreApplication.ID;
                            app.SourceID = appStoreApplication.SourceID;
                            app.SourceUrl = appStoreApplication.SourceUrl;
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
                            app.NewVersionAvailable = false;

                            app.LatestVersion.ID = "0";
                            app.LatestVersion.Url = "todo";

                            storeJson.Items.Add(app);
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
