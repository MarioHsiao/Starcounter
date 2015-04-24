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
using System.Globalization;
using Starcounter.Administrator.Server.Utilities;

namespace Starcounter.Administrator.Server.Handlers {
    internal static partial class StarcounterAdminAPI {

        /// <summary>
        /// Register Version Check GET
        /// </summary>
        public static void VersionCheck_GET(ushort port, IServerRuntime server) {

            // Get the Current and latest version
            //{
            //  "VersionCheck"
            //      {
            //          "currentEdition":"oem",
            //          "currentChannel":"beta",
            //          "currentVersion":"2.0.0.0",
            //          "currentVersionDate":"2012-04-23T18:25:43.511Z",
            //          "latestVersion":"2.0.1438.3",
            //          "latestVersionDate":"2012-04-23T18:25:43.511Z",
            //          "latestVersionDownloadUri":"http://downloads.starcounter.com/download/oem/NightlyBuilds/2.0.1439.3"
            //      }
            //}
            Handle.GET("/api/admin/versioncheck", (Request req) => {

                try {

                    // Retrive the latest available version for a specific edition and version
                    Response response = Http.GET(
                        "http://downloads.starcounter.com:80/api/versions/" + CurrentVersion.EditionName + "/" + CurrentVersion.ChannelName + "/latest", null, 5000);

                    if (!response.IsSuccessStatusCode) {
                        // TODO: Add "Retry-After" header
                        return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.ServiceUnavailable };
                    }

                    dynamic incomingJson = DynamicJson.Parse(response.Body);

                    var result = new versioncheck();

                    // Current version
                    result.VersionCheck.currentEdition = CurrentVersion.EditionName;
                    result.VersionCheck.currentChannel = CurrentVersion.ChannelName;
                    result.VersionCheck.currentVersion = CurrentVersion.Version;
                    result.VersionCheck.currentVersionDate = CurrentVersion.VersionDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");

                    // Latest version
                    result.VersionCheck.latestVersion = incomingJson.version;
                    DateTime latestVersionDate = DateTime.Parse(incomingJson.versionDate, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
                    result.VersionCheck.latestVersionDate = latestVersionDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
                    result.VersionCheck.latestVersionDownloadUri = incomingJson.downloadUrl;

                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.OK, BodyBytes = result.ToJsonUtf8() };
                }
                catch (Exception e) {
                    return RestUtils.CreateErrorResponse(e);
                }
            });
        }
    }
}
