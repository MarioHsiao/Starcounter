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

                GenerateStatistics(channel);


                dynamic response = new DynamicJson();
                try {

                    DateTime from = DateTime.Parse("2013-09-02 00:00:00");
                    DateTime to = DateTime.Parse("2013-09-03 00:00:00");

                    from = DateTime.MinValue;
                    to = DateTime.MaxValue;

                    Int64 downloads = GetDownloads(channel, from, to);

                    Int64 installations;
                    Int64 uninstallations;


                    GetMachineInstallations(channel, from, to, out installations, out uninstallations);

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

        static void GenerateStatistics(string channel) {

            // Clear all statistics
            Db.Transact(() => {
                QueryResultRows<StatisticDay> result = Db.SlowSQL<StatisticDay>("SELECT o FROM StatisticDay o");

                foreach (StatisticDay statisticDay in result) {
                    statisticDay.Delete();
                }

            });


            // Get first date
            DateTime firstDateOccurence = GetFirstOccurence();
            if (firstDateOccurence == DateTime.MaxValue) return;

            DateTime firstDateOccurenceStart = new DateTime(firstDateOccurence.Year, firstDateOccurence.Month, firstDateOccurence.Day, 0, 0, 0, DateTimeKind.Utc);
            DateTime startDate = firstDateOccurenceStart;

            DateTime currentDate = firstDateOccurenceStart;

            Int64 installations;
            Int64 uninstallations;

            Int64 prevInstallations = 0;
            Int64 prevUninstallations = 0;

            while (currentDate <= DateTime.UtcNow) {

                GetMachineInstallations(channel, startDate, currentDate.AddDays(1), out installations, out uninstallations);

                Int64 downloads = GetDownloads(channel, currentDate, currentDate.AddDays(1));

                StatisticDay statisticDay = Db.SlowSQL<StatisticDay>("SELECT o FROM StatisticDay o WHERE \"Date\"=?", currentDate).First;
                if (statisticDay == null) {
                    Db.Transact(() => {
                        statisticDay = new StatisticDay();
                        statisticDay.Date = currentDate;
                        statisticDay.Downloads = downloads;
                        statisticDay.Installations = installations - prevInstallations;
                        statisticDay.Uninstallations = uninstallations - prevUninstallations;
                    });
                }

                Console.WriteLine("{0} Downloads {1} Installations {2} Uninstallations {3}", statisticDay.Date, statisticDay.Downloads, statisticDay.Installations, statisticDay.Uninstallations);

                prevInstallations = installations;
                prevUninstallations = uninstallations;

                currentDate = currentDate.AddDays(1);
            }

        }


        static DateTime GetFirstOccurence() {

            DateTime dateTime = DateTime.MaxValue;

            Installation installation = Db.SlowSQL<Installation>("SELECT o FROM Installation o ORDER BY \"Date\"").First;
            dateTime = installation != null && installation.Date != DateTime.MinValue && installation.Date < dateTime ? installation.Date : dateTime;

            InstallerAbort installerAbort = Db.SlowSQL<InstallerAbort>("SELECT o FROM InstallerAbort o ORDER BY \"Date\"").First;
            dateTime = installerAbort != null && installerAbort.Date != DateTime.MinValue && installerAbort.Date < dateTime ? installerAbort.Date : dateTime;

            InstallerEnd InstallerEnd = Db.SlowSQL<InstallerEnd>("SELECT o FROM InstallerEnd o ORDER BY \"Date\"").First;
            dateTime = InstallerEnd != null && InstallerEnd.Date != DateTime.MinValue && InstallerEnd.Date < dateTime ? InstallerEnd.Date : dateTime;

            InstallerException InstallerException = Db.SlowSQL<InstallerException>("SELECT o FROM InstallerException o ORDER BY \"Date\"").First;
            dateTime = InstallerException != null && InstallerException.Date != DateTime.MinValue && InstallerException.Date < dateTime ? InstallerException.Date : dateTime;

            InstallerExecuting InstallerExecuting = Db.SlowSQL<InstallerExecuting>("SELECT o FROM InstallerExecuting o ORDER BY \"Date\"").First;
            dateTime = InstallerExecuting != null && InstallerExecuting.Date != DateTime.MinValue && InstallerExecuting.Date < dateTime ? InstallerExecuting.Date : dateTime;

            InstallerFinish InstallerFinish = Db.SlowSQL<InstallerFinish>("SELECT o FROM InstallerFinish o ORDER BY \"Date\"").First;
            dateTime = InstallerFinish != null && InstallerFinish.Date != DateTime.MinValue && InstallerFinish.Date < dateTime ? InstallerFinish.Date : dateTime;

            InstallerStart InstallerStart = Db.SlowSQL<InstallerStart>("SELECT o FROM InstallerStart o ORDER BY \"Date\"").First;
            dateTime = InstallerStart != null && InstallerStart.Date != DateTime.MinValue && InstallerStart.Date < dateTime ? InstallerStart.Date : dateTime;

            StarcounterGeneral StarcounterGeneral = Db.SlowSQL<StarcounterGeneral>("SELECT o FROM StarcounterGeneral o ORDER BY \"Date\"").First;
            dateTime = StarcounterGeneral != null && StarcounterGeneral.Date != DateTime.MinValue && StarcounterGeneral.Date < dateTime ? StarcounterGeneral.Date : dateTime;

            StarcounterUsage StarcounterUsage = Db.SlowSQL<StarcounterUsage>("SELECT o FROM StarcounterUsage o ORDER BY \"Date\"").First;
            dateTime = StarcounterUsage != null && StarcounterUsage.Date != DateTime.MinValue && StarcounterUsage.Date < dateTime ? StarcounterUsage.Date : dateTime;

            ErrorReportItem ErrorReportItem = Db.SlowSQL<ErrorReportItem>("SELECT o FROM ErrorReportItem o ORDER BY \"Date\"").First;
            dateTime = ErrorReportItem != null && ErrorReportItem.Date != DateTime.MinValue && ErrorReportItem.Date < dateTime ? ErrorReportItem.Date : dateTime;

            VersionBuild versionBuild = Db.SlowSQL<VersionBuild>("SELECT o FROM VersionBuild o WHERE o.HasBeenDownloaded=? ORDER BY o.DownloadDate", true).First;
            dateTime = versionBuild != null && versionBuild.DownloadDate != DateTime.MinValue && versionBuild.DownloadDate < dateTime ? versionBuild.DownloadDate : dateTime;

            return dateTime;

        }



        /// <summary>
        /// Get number of total downloads
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        static Int64 GetDownloads(string channel, DateTime from, DateTime to) {

            Int64 counter = 0;
            QueryResultRows<VersionBuild> versionBuilds = Db.SlowSQL<VersionBuild>("SELECT o FROM VersionBuild o WHERE o.HasBeenDownloaded=? AND o.Channel=? ORDER BY o.DownloadDate", true, channel);


            foreach (VersionBuild versionBuild in versionBuilds) {

                if (versionBuild.DownloadDate >= from && versionBuild.DownloadDate < to) {
                    counter++;
                }

            }

            return counter;

            //return Db.SlowSQL<Int64>("SELECT count(o) FROM VersionBuild o WHERE o.HasBeenDownloaded=? AND o.Channel=?", true, channel).First;
        }


        /// <summary>
        /// Get number of total "unique" installations
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="numberOfInstallations"></param>
        /// <param name="numberOfUnInstallations"></param>
        static void GetMachineInstallations(string channel, DateTime from, DateTime to, out Int64 numberOfInstallations, out Int64 numberOfUnInstallations) {

            List<Installation> firstInstallations = Installation.GetAllFirstNodes();
            numberOfInstallations = 0;
            numberOfUnInstallations = 0;

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
