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
        /// Register Installed Application DELETE
        /// </summary>
        public static void InstalledApplication_DELETE(string appsRootFolder) {

            // Delete installed application
            Handle.DELETE("/api/admin/installed/apps/{?}", (string id, Request req) => {

                try {

                    AppConfig appConfig = null;

                    // GET APP
                    IList<AppConfig> apps = AppsContainer.GetInstallApps(appsRootFolder);
                    foreach (AppConfig app in apps) {
                        if (string.Compare(app.ID, id, true) == 0) {
                            appConfig = app;
                            break;
                        }
                    }

                    if (appConfig == null) {
                        ErrorResponse errorResponse = new ErrorResponse();
                        errorResponse.Text = string.Format("Failed to uninstall application, application was not found.");
                        return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NotFound, BodyBytes = errorResponse.ToJsonUtf8() };
                    }

                    AppsContainer.UnInstall(appConfig);

                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NoContent };
                }
                catch (Exception e) {
                    return RestUtils.CreateErrorResponse(e);
                }
            });
        }
    }
}
