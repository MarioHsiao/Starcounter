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
        public static void InstalledApplication_GET(ushort port, string appsRootFolder) {

            // Get a list of all running Applications
            // Example response
            //{
            // "Items": [
            //      {
            //          "ID": "", 
            //          "Url" : "",
            //          "Namespace": "", 
            //          "Channel" : "",
            //          "Version" : "",
            //          "DisplayName": "", 
            //          "Description": "",
            //          "Company" : "",
            //          "VersionDate" : "",
            //          "Executable" : "",
            //          "ResourceFolder" : "",
            //          "Size" : 0,
            //          "ImageUri" : ""
            //      }
            //  ]
            //}
            Handle.GET(port, "/api/admin/installed/apps", (Request req) => {

                try {

                    Representations.JSON.InstalledApplications installedApplications = new Representations.JSON.InstalledApplications();

                    IList<AppConfig> apps = AppsContainer.GetInstallApps(appsRootFolder);
                    string relative = "/api/admin/installed/apps";
                    string url = new Uri(Starcounter.Administrator.API.Handlers.RootHandler.Host.BaseUri, relative).ToString();

                    string appBasefolder = appsRootFolder;

                    foreach (AppConfig appConfig in apps) {
                        Representations.JSON.InstalledApplication item;
                        BuildApplicationItem(appConfig, url, appBasefolder, out item);
                        installedApplications.Items.Add(item);
                    }

                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.OK, BodyBytes = installedApplications.ToJsonUtf8() };
                }
                catch (Exception e) {
                    return RestUtils.CreateErrorResponse(e);
                }
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="appConfig"></param>
        /// <param name="url"></param>
        /// <param name="appBasefolder"></param>
        /// <param name="item"></param>
        private static void BuildApplicationItem(AppConfig appConfig, string url, string appBasefolder, out Representations.JSON.InstalledApplication item) {

            item = new Representations.JSON.InstalledApplication();
            item.ID = appConfig.ID;
            item.Namespace = appConfig.Namespace;
            item.Channel = appConfig.Channel;
            item.Version = appConfig.Version;
            item.DisplayName = appConfig.DisplayName;
            item.Description = appConfig.Description;
            item.Company = appConfig.Company;
            item.VersionDate = appConfig.VersionDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'"); ;

            string appExe = Path.Combine(appBasefolder, appConfig.Namespace);
            appExe = Path.Combine(appExe, appConfig.Channel);
            appExe = Path.Combine(appExe, appConfig.Version);
            appExe = Path.Combine(appExe, appConfig.Executable);

            appExe = appExe.Replace('/', '\\'); // TODO: Fix this when config is verified
            item.Executable = appExe;

            string appResourcFolder = Path.Combine(appBasefolder, appConfig.Namespace);
            appResourcFolder = Path.Combine(appResourcFolder, appConfig.Channel);
            appResourcFolder = Path.Combine(appResourcFolder, appConfig.Version);
            appResourcFolder = Path.Combine(appResourcFolder, appConfig.ResourceFolder);
            appResourcFolder = appResourcFolder.Replace('/', '\\');  // TODO: Fix this when config is verified
            item.ResourceFolder = appResourcFolder;

            item.Size = 0;      // TODO: Collect the disk space size?
            item.ImageUri = appConfig.ImageUri;
            item.Url = url + "/" + appConfig.Namespace;
        }
    }
}
