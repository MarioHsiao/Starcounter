using System;
using Starcounter;
using Starcounter.Advanced;
using StarcounterApplicationWebSocket.VersionHandler.Model;
using Codeplex.Data;
using Starcounter.Internal.Web;
using System.Net;
using StarcounterApplicationWebSocket.VersionHandler;
using System.IO;

namespace Starcounter.Applications.UsageTrackerApp.API.Versions {


    internal class Download {

        public static void BootStrap(ushort port) {

            Handle.GET(port, "/download", (Request request) => {

                try {

                    string channel = "NightlyBuilds";   // TODO:

                    VersionBuild latestBuild = VersionBuild.GetLatestAvailableBuild(channel);
                    if (latestBuild == null) {
                        // TODO: Redirect to a information page
                        string message = string.Format("The {0} channel is not available at the moment. Please try again later", channel);
                        return new Response() { Uncompressed = HttpResponseBuilder.Slow.FromStatusHeadersAndStringContent((int)HttpStatusCode.ServiceUnavailable, null, message) };

                    }

                    byte[] fileBytes = File.ReadAllBytes(latestBuild.File);

                    Db.Transaction(() => {
                        latestBuild.DownloadDate = DateTime.UtcNow;
                        latestBuild.IPAdress = request.GetClientIpAddress().ToString();
                    });

                    VersionHandlerApp.BuildkWorker.Trigger();
                    // response.setHeader("content-disposition", "inline; filename=\"My.pdf\"");
                    // header('Content-Disposition: attachment; filename="downloaded.pdf"');
                    Console.WriteLine("NOTICE: Uploading version {0} to {1}", latestBuild.Source.Version, request.GetClientIpAddress().ToString());

                    string fileName = Path.GetFileName(latestBuild.File);

                    return new Response() { BodyBytes = fileBytes, Headers = "Content-Disposition: attachment; filename=\"" + fileName + "\"\r\n", StatusCode = (ushort)System.Net.HttpStatusCode.OK };


                }
                catch (Exception e) {
                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.InternalServerError, Body = e.ToString() };
                }

            });

            Handle.GET(port, "/download/{?}", (string version, Request request) => {

                try {

                    // Check if version exists.
                    var result = Db.SlowSQL("SELECT o FROM VersionSource o WHERE o.Version=?", version).First;
                    if (result == null) {
                        // TODO: Redirect to a information page
                        string message = string.Format("The version {0} is not available", version);
                        return new Response() { Uncompressed = HttpResponseBuilder.Slow.FromStatusHeadersAndStringContent((int)HttpStatusCode.BadRequest, null, message) };
                    }

                    VersionBuild build = VersionBuild.GetAvilableBuild(version);

                    if (build == null) {
                        // TODO: Redirect to a information page
                        string message = string.Format("The version {0} is not available at the moment. Please try again later", version);
                        return new Response() { Uncompressed = HttpResponseBuilder.Slow.FromStatusHeadersAndStringContent((int)HttpStatusCode.ServiceUnavailable, null, message) };

                    }

                    Console.WriteLine("Uploading build:" + build.Source.Version);

                    byte[] fileBytes = File.ReadAllBytes(build.File);

                    Db.Transaction(() => {
                        build.DownloadDate = DateTime.UtcNow;
                        build.IPAdress = request.GetClientIpAddress().ToString();
                    });

                    VersionHandlerApp.BuildkWorker.Trigger();

                    string fileName = Path.GetFileName(build.File);

                    return new Response() { BodyBytes = fileBytes, Headers = "Content-Disposition: attachment; filename=\"" + fileName + "\"\r\n", StatusCode = (ushort)System.Net.HttpStatusCode.OK };

                }
                catch (Exception e) {
                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.InternalServerError, Body = e.ToString() };
                }

            });

        }
    }
}
