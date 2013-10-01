using System;
using Starcounter;
using Starcounter.Advanced;
using StarcounterApplicationWebSocket.VersionHandler.Model;
using Codeplex.Data;
using System.Net;
using StarcounterApplicationWebSocket.VersionHandler;
using System.IO;
using StarcounterApplicationWebSocket;
using Starcounter.Applications.UsageTrackerApp.Model;
using System.Collections.Generic;

namespace Starcounter.Applications.UsageTrackerApp.API.Versions {


    internal class Statistics {

        public static void BootStrap(ushort port) {

            // Download latest version from channel 'NightlyBuilds'
            // This link is used by www.starcounter.com
            Handle.GET(port, "/statistics/total", (Request request) => {

                //; { downloads: 12, installations:12, uninstallation:0, upgrades:0, downgrades:0, failures:0, aborted:0 }

                string channel = "NightlyBuilds";

                dynamic response = new DynamicJson();
                try {
                    Int64 downloads = GetDownloads(channel);

                    Int64 installations;
                    Int64 uninstallations;

                    GetMachineInstallations(channel, out installations, out uninstallations);

                    response.statistics = new {
                        totalDownloads = downloads,                     // Total downloads
                        installations = installations,                  // Total starcounter installations
                        uninstallation = uninstallations,               // Total starcounter uninstallations
                        successInstallations = -1,
                        upgrades = -1,
                        downgrades = -1,
                        failures = -1,
                        aborted = -1
                    };

                    return new Response() { Body = response.ToString(), StatusCode = (ushort)System.Net.HttpStatusCode.OK };

                }
                catch (Exception e) {
                    return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.InternalServerError, Body = e.ToString() };
                }


            });


        }

        /// <summary>
        /// Get number of total downloads
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        static Int64 GetDownloads(string channel) {
            return Db.SlowSQL<Int64>("SELECT count(o) FROM VersionBuild o WHERE o.HasBeenDownloaded=? AND o.Channel=?", true, channel).First;
        }

        /// <summary>
        /// Get number of total "unique" installations
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="numberOfInstallations"></param>
        /// <param name="numberOfUnInstallations"></param>
        static void GetMachineInstallations(string channel, out Int64 numberOfInstallations, out Int64 numberOfUnInstallations) {

            List<Installation> firstInstallations = Installation.GetAllFirstNodes();
            numberOfInstallations = 0;
            numberOfUnInstallations = 0;

            DateTime from = DateTime.Parse("2013-09-02 00:00:00");
            DateTime to = DateTime.Parse("2013-09-03 00:00:00");

            from = DateTime.MinValue;
            to = DateTime.MaxValue;

       

            foreach (Installation installation in firstInstallations) {

                Installation i = Installation.GetFirstNode(installation);
                if (i != installation) {
                    // Error
                }

                Installation installInstallation = Installation.GetWhenInstallationWasInstalled(installation, from, to);
                if (installInstallation != null) {
                    numberOfInstallations++;
                }

                Installation uninstallInstallation = Installation.GetWhenInstallationWasUninstalled(installation, from, to);
                if (uninstallInstallation != null) {
                    numberOfUnInstallations++;
                }

          

            }



        }


    }
}
