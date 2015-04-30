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

                    var store = result.Stores.Add();
                    store.DisplayName = "Fake Store";
                    store.ID = "1234";
                    store.Updates = 0;


                    foreach (AppStoreApplication appStoreApplication in database.AppStoreApplications) {

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



                        store.Items.Add(app);


                    }

                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.OK, BodyBytes = result.ToJsonUtf8() };


                    //IList<AppStoreApplication> applications;
                    //try {
                    //    AppStoreManager.GetApplications(database, out applications);
                    //}
                    //catch (InvalidOperationException e) {

                    //    ErrorResponse errorResponse = new ErrorResponse();
                    //    errorResponse.Text = e.Message;
                    //    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.ServiceUnavailable, BodyBytes = errorResponse.ToJsonUtf8() };
                    //}



                    //DatabaseApplicationsJson result = new DatabaseApplicationsJson();
                    ////Representations.JSON.AppStoreApplications result = new Representations.JSON.AppStoreApplications();
                    //foreach (var remoteApplication in applications) {

                    //    //foreach( var 

                    //    //Representations.JSON.AppStoreApplication appItem = new Representations.JSON.AppStoreApplication();

                    //    //appItem

                    //    DatabaseApplicationJson appItem = remoteApplication.ToDatabaseApplication();
                    //    result.Items.Add(appItem);
                    //}
                    //return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.OK, BodyBytes = result.ToJsonUtf8() };
                }
                catch (Exception e) {
                    return RestUtils.CreateErrorResponse(e);
                }
                //try {

                //    IList<AppStoreApplication> applications;

                //    Database database = ServerManager.ServerInstance.GetDatabase(databaseName);

                //    try {
                //        AppStoreManager.GetApplications(database, out applications);
                //    }
                //    catch (InvalidOperationException e) {

                //        ErrorResponse errorResponse = new ErrorResponse();
                //        errorResponse.Text = e.Message;
                //        return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.ServiceUnavailable, BodyBytes = errorResponse.ToJsonUtf8() };
                //    }


                //    //Response response;
                //    //if (AppStoreManager.GetApplications(appStoreHost, out applications, out response) == false) {
                //    //    return response;
                //    //}

                //    DatabaseApplicationsJson result = new DatabaseApplicationsJson();
                //    //Representations.JSON.AppStoreApplications result = new Representations.JSON.AppStoreApplications();
                //    foreach (var remoteApplication in applications) {

                //        //foreach( var 

                //        //Representations.JSON.AppStoreApplication appItem = new Representations.JSON.AppStoreApplication();

                //        //appItem

                //        DatabaseApplicationJson appItem = remoteApplication.ToDatabaseApplication();
                //        result.Items.Add(appItem);
                //    }
                //    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.OK, BodyBytes = result.ToJsonUtf8() };
                //}
                //catch (Exception e) {
                //    return RestUtils.CreateErrorResponse(e);
                //}
            });
        }
    }
}
