using System;
using Starcounter;
using Starcounter.Advanced;
using StarcounterApplicationWebSocket.VersionHandler.Model;
using Codeplex.Data;
using System.Net;
using StarcounterApplicationWebSocket.VersionHandler;
using System.IO;
using Starcounter.Applications.UsageTrackerApp.VersionHandler;

namespace Starcounter.Applications.UsageTrackerApp.API.Versions {


    internal class Download {

        public static void BootStrap(ushort port) {

            // Show download page
            Handle.GET(port, "/download", (Request request) => {

                bool authorized = Login.IsAuthorized(request.Cookies);

                if (authorized) {
                    Node node = new Node("127.0.0.1", port);
                    Response response = node.GET("/downloads.html", null);

                    Response newResponse = new Response();
                    newResponse.BodyBytes = response.BodyBytes;
                    newResponse.ContentType = response.ContentType;

                    return newResponse;
                }
                else {
                    return GetLoginPageResponse(port);
                }
            });


            // TODO: Show a list of all avaliable versions in a specific edition per channel
            Handle.GET(port, "/download/{?}", (string edition, Request request) => {

                // TODO: Show a list of all avaliable versions in a specific edition per channel

                Response response = new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.Redirect };
                response["Location"] = "/download";
                return response;
            });


            // TODO: Show a list of all avaliable versions in a specific edition and channel
            Handle.GET(port, "/download/{?}/{?}", (string edition, string channel, Request request) => {

                Response response = new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.Redirect };
                response["Location"] = "/download";
                return response;

            });


            // Download a specific version
            Handle.GET(port, "/download/{?}/{?}/{?}", (string edition, string channel, string version, Request request) => {

                bool authorized = Login.IsAuthorized(request.Cookies);

                if (authorized) {

                    // If version is "latest" then deliver the latest version on a specific edition and channel
                    if ("latest" == version.ToLower()) {
                        return GetVersionResponse(request, edition, channel);
                    }

                    return GetVersionResponse(request, edition, channel, version);

                }
                else {
                    return GetLoginPageResponse(port);
                }

            });


            // Backward compatibility
            // This is used from the www.starcounter.com web page
            Handle.GET(port, "/beta", (Request request) => {

                Response response = new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.Redirect };
                response["Location"] = "/download/Starcounter/NightlyBuilds/latest";
                return response;

            });

            // No need for downloadkey's anymore
            Handle.GET(port, "/beta/{?}", (string downloadkey, Request request) => {
                Response response = new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.Redirect };
                response["Location"] = "/beta";
                return response;
            });

        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        private static Response GetLoginPageResponse(ushort port) {

            Node node = new Node("127.0.0.1", port);

            Response response = node.GET("/login.html", null);

            Response newResponse = new Response();
            newResponse.BodyBytes = response.BodyBytes;
            newResponse.ContentType = response.ContentType;

            return newResponse;

        }


        /// <summary>
        /// Get Latest version for a specific edition and channel
        /// </summary>
        /// <param name="request"></param>
        /// <param name="edition"></param>
        /// <param name="channel"></param>
        /// <returns></returns>
        private static Response GetVersionResponse(Request request, string edition, string channel) {

            VersionSource versionSource = VersionSource.GetLatestVersion(edition, channel);
            if (versionSource == null) {
                string message = string.Format("The download is unavailable due to invalid edition or channel name.");
                return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.ServiceUnavailable, Body = message };
            }

            return GetVersionResponse(request, edition, channel, versionSource.Version);
        }


        /// <summary>
        /// Get a specific version with a specified edition and channel.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="edition"></param>
        /// <param name="channel"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        private static Response GetVersionResponse(Request request, string edition, string channel, string version) {

            try {

                string ipAddress = request.ClientIpAddress.ToString();
                if (Utils.IsBlacklisted(ipAddress)) {
                    LogWriter.WriteLine(string.Format("WARNING: IP Address {0} is blacklisted", ipAddress));
                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.Forbidden };
                }

                // Check if source exist for specified channel and version.
                VersionSource versionSource = Db.SlowSQL<VersionSource>("SELECT o FROM VersionSource o WHERE o.Edition=? AND o.Channel=? AND o.Version=? AND o.IsAvailable=?", edition, channel, version, true).First;
                if (versionSource == null) {
                    string message = string.Format("The {0} edition version {1} from channel {2} is not available.", edition, version, channel);
                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NotFound, Body = message };
                }

                VersionBuild versionBuild = VersionBuild.GetAvailableBuild(edition, channel, version);
                if (versionBuild == null) {
                    string message = string.Format("The download is not available at the moment. Please try again later.");
                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.ServiceUnavailable, Body = message };
                }

                byte[] fileBytes = File.ReadAllBytes(versionBuild.File);

                Db.Transact(() => {
                    versionBuild.DownloadDate = DateTime.UtcNow;
                    versionBuild.IPAdress = ipAddress;
                });

                VersionHandlerApp.BuildkWorker.Trigger();

                LogWriter.WriteLine(string.Format("NOTICE: Sending {0}-{1}-{2} to ip {3}", versionBuild.Edition, versionBuild.Version, versionBuild.Channel, request.ClientIpAddress.ToString()));

                string fileName = Path.GetFileName(versionBuild.File);

                // Delete version build file
                VersionBuild.DeleteVersionBuildFile(versionBuild);

                // Assure IP Location
                Utils.AssureIPLocation(versionBuild.IPAdress);

                Response response = new Response() { BodyBytes = fileBytes, StatusCode = (ushort)System.Net.HttpStatusCode.OK };
                response["Content-Disposition"] = "attachment; filename=\"" + fileName + "\"";
                return response;

            }
            catch (Exception e) {
                return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.InternalServerError, Body = e.ToString() };
            }

        }

    }
}
