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

            // Download latest version from channel 'NightlyBuilds'
            // This link is used by www.starcounter.com
            Handle.GET(port, "/beta", (string downloadkey, Request request) => {

                try {

                    VersionBuild latestBuild = VersionBuild.GetLatestAvailableBuild("NightlyBuilds");
                    if (latestBuild == null) {
                        // TODO: Redirect to a information page?
                        string message = string.Format("The download is not available at the moment. Please try again later.");
                        return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.ServiceUnavailable, Body = message };
                    }

                    byte[] fileBytes = File.ReadAllBytes(latestBuild.File);

                    Db.Transaction(() => {
                        latestBuild.DownloadDate = DateTime.UtcNow;
                        latestBuild.DownloadKey = downloadkey;
                        latestBuild.IPAdress = request.GetClientIpAddress().ToString();
                    });

                    VersionHandlerApp.BuildkWorker.Trigger();

                    LogWriter.WriteLine(string.Format("NOTICE: Sending version {0} to ip {1}", latestBuild.Version, request.GetClientIpAddress().ToString()));

                    string fileName = Path.GetFileName(latestBuild.File);

                    return new Response() { BodyBytes = fileBytes, Headers = "Content-Disposition: attachment; filename=\"" + fileName + "\"\r\n", StatusCode = (ushort)System.Net.HttpStatusCode.OK };

                }
                catch (Exception e) {
                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.InternalServerError, Body = e.ToString() };
                }

            });

            // Download latest version from channel 'NightlyBuilds' with key
            // This ise used by users how got an email with a personal download link
            Handle.GET(port, "/beta/{?}", (string downloadkey, Request request) => {

                try {

                    // Find a valid sombody
                    Somebody somebody = Db.SlowSQL<Somebody>("SELECT o FROM Somebody o WHERE o.DownloadKey=?", downloadkey).First;
                    if (somebody == null) {
                        return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NotFound };
                    }

                    VersionBuild latestBuild = VersionBuild.GetLatestAvailableBuild("NightlyBuilds");
                    if (latestBuild == null) {
                        // TODO: Redirect to a information page?
                        string message = string.Format("The download is not available at the moment. Please try again later.");
                        return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.ServiceUnavailable, Body = message };
                    }

                    byte[] fileBytes = File.ReadAllBytes(latestBuild.File);

                    Db.Transaction(() => {
                        latestBuild.DownloadDate = DateTime.UtcNow;
                        latestBuild.DownloadKey = downloadkey;
                        latestBuild.IPAdress = request.GetClientIpAddress().ToString();
                    });

                    VersionHandlerApp.BuildkWorker.Trigger();

                    LogWriter.WriteLine(string.Format("NOTICE: Sending version {0} to ip {1} with Key {2}", latestBuild.Version, request.GetClientIpAddress().ToString(), downloadkey));

                    string fileName = Path.GetFileName(latestBuild.File);

                    return new Response() { BodyBytes = fileBytes, Headers = "Content-Disposition: attachment; filename=\"" + fileName + "\"\r\n", StatusCode = (ushort)System.Net.HttpStatusCode.OK };

                }
                catch (Exception e) {
                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.InternalServerError, Body = e.ToString() };
                }

            });

            // Download latest version from channel 'NightlyBuilds'
            // This link is given to ppl that wanted a direct download link wihtout a key.
            // NOTE: deprecated, it's replaces by the /beta link
            Handle.GET(port, "/hiddenarea/latest", (Request request) => {
                return new Response() { Headers = "Location: /beta\r\n", StatusCode = (ushort)System.Net.HttpStatusCode.MovedPermanently };
            });

        }
    }
}
