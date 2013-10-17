using System;
using Starcounter;
using Starcounter.Advanced;
using StarcounterApplicationWebSocket.VersionHandler.Model;
using Codeplex.Data;
using System.Net;
using StarcounterApplicationWebSocket.VersionHandler;
using System.IO;

namespace Starcounter.Applications.UsageTrackerApp.API.Versions {


    internal class Channels_Get {

        public static void BootStrap(ushort port) {


            // Resturns all available channels
            // JSON Format: 
            // {
            //      "channels":[{
            //          "name":"NightlyBuilds",
            //          "latestVersion":"2.0.801.3"
            //      }]
            // }
            Handle.GET(port, "/api/channels", (Request request) => {

                try {

                    var result = Db.SlowSQL("SELECT o.Channel FROM VersionSource o GROUP BY o.Channel");

                    Channels channels = new Channels();

                    foreach (string channel in result) {

                        string latestVersion = VersionSource.GetLatestVersion(channel);
                        if (latestVersion == null) continue;

                        var item = channels.channels.Add();
                        item.name = channel;
                        item.latestVersion = latestVersion.ToString();
                    }

                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.OK, BodyBytes = channels.ToJsonUtf8() };

                }
                catch (Exception e) {
                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.InternalServerError, Body = e.ToString() };
                }

            });


            // Resturn channel information
            // JSON Format: 
            // {
            //      "name":"NightlyBuilds",
            //      "latestVersion":"2.0.801.3",
            //      "downloadUrl":"http://downloads.starcounter.com/archive/NightlyBuilds/2.0.801.3"
            // }
            Handle.GET(port, "/api/channels/{?}", (string channel, Request request) => {

                try {

                    VersionSource versionSource = Db.SlowSQL<VersionSource>("SELECT o FROM VersionSource o WHERE o.Channel=?", channel).First;
                    if (versionSource == null) {
                        return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NotFound, Body = string.Format("Channel {0} not found", channel) };
                    }

                    string latestVersion = VersionSource.GetLatestVersion(channel);
                    if (latestVersion == null) {
                        return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NotFound, Body = string.Format("There is no available versions in the channel {0}", channel) };
                    }

                    Channel channelItem = new Channel();
                    channelItem.name = versionSource.Channel;
                    channelItem.latestVersion = latestVersion.ToString();
                    channelItem.downloadUrl = string.Format("http://downloads.starcounter.com/archive/{0}/{1}", channel, latestVersion.ToString());

                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.OK, BodyBytes = channelItem.ToJsonUtf8() };

                }
                catch (Exception e) {
                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.InternalServerError, Body = e.ToString() };
                }

            });


        }
    }

}
