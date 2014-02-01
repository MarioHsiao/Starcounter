using System;
using Starcounter;
using Starcounter.Advanced;
using StarcounterApplicationWebSocket.VersionHandler.Model;
using Codeplex.Data;
using System.Net;
using StarcounterApplicationWebSocket.VersionHandler;
using System.IO;

namespace Starcounter.Applications.UsageTrackerApp.API.Versions {


    internal class Versions_Get {

        public static void BootStrap(ushort port) {


            // Get a list of all available versions grouped by channel
            // JSON Format: 
            // {
            //  editions:[{
            //        name:"oem",
            //        channels:[{
            //            name:"nightlybuild",
            //            latestVersion:"2.0.234.2",
            //            latestVersionDate:"2013-09-16 10:10:10Z",
            //            downloadUrl:"http://downloads.starcounter.com/nightlybuilds/2.0.567.3",
            //            versions: [{
            //                edition:"edition name",
            //                channel:"channel name",
            //                version:"2.0.234.2",
            //                versionDate:"2013-09-16 10:10:10Z",
            //                downloadUrl:"http://downloads.starcounter.com/nightlybuilds/2.0.567.3" 
            //            }]
            //        }]
            //  }]
            // }
            Handle.GET(port, "/api/versions", (Request request) => {

                try {

                    AllVersions allVersionItems = new AllVersions();

                    // Get all available editions
                    var editionsResult = Db.SlowSQL<string>("SELECT o.Edition FROM VersionSource o WHERE o.IsAvailable=? GROUP BY o.Edition ORDER BY o.Edition", true);
                    if (editionsResult != null) {

                        foreach (string edition in editionsResult) {

                            var jsonEdition = allVersionItems.editions.Add();
                            jsonEdition.name = edition;

                            // Get all channels in one edition
                            var channelsResult = Db.SlowSQL<string>("SELECT o.Channel FROM VersionSource o WHERE o.Edition=? AND o.IsAvailable=? GROUP BY o.Edition, o.Channel ORDER BY o.Channel", edition, true);
                            if (channelsResult != null) {
                                foreach (string channel in channelsResult) {
                                    var jsonChannel = jsonEdition.channels.Add();
                                    jsonChannel.name = channel;

                                    VersionSource latestVersion = VersionSource.GetLatestVersion(edition, channel);
                                    if (latestVersion != null) {
                                        jsonChannel.latestVersion = latestVersion.Version;
                                        jsonChannel.latestVersionDate = latestVersion.VersionDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
                                        jsonChannel.downloadUrl = string.Format("http://{0}/download/{1}/{2}/{3}", VersionHandlerApp.StarcounterTrackerUrl, latestVersion.Edition, latestVersion.Channel, latestVersion.Version);
                                    }

                                    var versionSourceResult = Db.SlowSQL<VersionSource>("SELECT o FROM VersionSource o WHERE o.Edition=? AND o.Channel=? AND o.IsAvailable=? ORDER BY o.VersionDate DESC", edition, channel, true);

                                    if (versionSourceResult != null) {
                                        foreach (VersionSource versionSource in versionSourceResult) {
                                            var jsonVersion = jsonChannel.versions.Add();
                                            jsonVersion.edition = versionSource.Edition;
                                            jsonVersion.channel = versionSource.Channel;
                                            jsonVersion.version = versionSource.Version;
                                            jsonVersion.versionDate = versionSource.VersionDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
                                            jsonVersion.downloadUrl = string.Format("http://{0}/download/{1}/{2}/{3}", VersionHandlerApp.StarcounterTrackerUrl, versionSource.Edition, versionSource.Channel, versionSource.Version);
                                        }
                                    }


                                }
                            }


                        }
                    }


                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.OK, BodyBytes = allVersionItems.ToJsonUtf8() };

                }
                catch (Exception e) {
                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.InternalServerError, Body = e.ToString() };
                }

            });


            // Version information in JSON 
            // JSON Format: 
            // {
            //      "version":"2.0.801.3",
            //      "versionDate":"2013-09-16 10:10:10Z",
            //      "downloadUrl":"http://downloads.starcounter.com/archive/NightlyBuilds/2.0.801.3"
            // }
            Handle.GET(port, "/api/versions/{?}/{?}/{?}", (string edition, string channel, string version, Request request) => {

                try {

                    VersionSource versionSource;
                    if ("latest" == version.ToLower()) {
                        // Get latest version
                        versionSource = VersionSource.GetLatestVersion(edition, channel);
                    }
                    else {
                        versionSource = Db.SlowSQL<VersionSource>("SELECT o FROM VersionSource o WHERE o.Edition=? AND o.Channel=? AND o.Version=? AND o.IsAvailable=?", edition, channel, version, true).First;
                    }

                    if (versionSource == null) {
                        return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NotFound, Body = string.Format("The {0} edition version {1} in channel {2} was not found", edition, version, channel) };
                    }

                    Version versionItem = new Version();
                    versionItem.edition = versionSource.Edition;
                    versionItem.channel = versionSource.Channel;
                    versionItem.version = versionSource.Version;
                    versionItem.versionDate = versionSource.VersionDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
                    versionItem.downloadUrl = string.Format("http://{0}/download/{1}/{2}/{3}", VersionHandlerApp.StarcounterTrackerUrl, versionSource.Edition, versionSource.Channel, versionSource.Version);

                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.OK, BodyBytes = versionItem.ToJsonUtf8() };

                }
                catch (Exception e) {
                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.InternalServerError, Body = e.ToString() };
                }

            });



            // Get a list of all available versions in a specific channel
            // JSON Format: 
            // {
            //      "versions":[{
            //          "channel":"NightlyBuilds",
            //          "version":"2.0.801.3",
            //          "buildDate":"",
            //          "downloadUrl":"http://downloads.starcounter.com/archive/nightlybuilds/2.0.801.3"
            //      }]
            // }
            //Handle.GET(port, "/api/versions/{?}", (string channel, Request request) => {

            //    try {

            //        var result = Db.SlowSQL<VersionSource>("SELECT o FROM VersionSource o WHERE o.IsAvailable=? AND o.Channel=?", true, channel);

            //        if (result == null) {
            //            return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NotFound, Body = string.Format("Channel {0} not found", channel) };
            //        }

            //        Versions versions = new Versions();

            //        foreach (VersionSource versionSource in result) {
            //            var item = versions.versions.Add();
            //            item.channel = versionSource.Channel;
            //            item.version = versionSource.Version;
            //            item.versionDate = versionSource.VersionDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
            //            item.downloadUrl = string.Format("http://{0}/archive/{1}/{2}", VersionHandlerApp.StarcounterTrackerUrl, channel, versionSource.Version);
            //            //item.buildDate =
            //        }

            //        return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.OK, BodyBytes = versions.ToJsonUtf8() };

            //    }
            //    catch (Exception e) {
            //        return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.InternalServerError, Body = e.ToString() };
            //    }

            //});


            // Version information in JSON 
            // JSON Format: 
            // {
            //      "version":"2.0.801.3",
            //      "downloadUrl":"http://downloads.starcounter.com/archive/NightlyBuilds/2.0.801.3"
            // }
            //Handle.GET(port, "/api/versions/{?}/{?}", (string channel, string version, Request request) => {

            //    try {

            //        VersionSource versionSource = Db.SlowSQL<VersionSource>("SELECT o FROM VersionSource o WHERE o.IsAvailable=? AND o.Channel=? AND o.Version=?", true, channel, version).First;
            //        if (versionSource == null) {
            //            return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NotFound, Body = string.Format("Version {0} in channel {1} was not found", version, channel) };
            //        }

            //        Version versionItem = new Version();
            //        versionItem.version = versionSource.Version;
            //        versionItem.versionDate = versionSource.VersionDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
            //        versionItem.downloadUrl = string.Format("http://{0}/archive/{1}/{2}", VersionHandlerApp.StarcounterTrackerUrl, versionSource.Channel, versionSource.Version);

            //        return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.OK, BodyBytes = versionItem.ToJsonUtf8() };

            //    }
            //    catch (Exception e) {
            //        return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.InternalServerError, Body = e.ToString() };
            //    }

            //});

        }
    }

}
