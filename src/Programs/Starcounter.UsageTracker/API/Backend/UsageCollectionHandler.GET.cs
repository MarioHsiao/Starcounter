using System;
using Codeplex.Data;
using Starcounter;
using Starcounter.Advanced;
using Starcounter.Programs.UsageTrackerApp.Model;

namespace Starcounter.Programs.UsageTrackerApp.API.Backend {
    internal static partial class Administrator {

        public static void Usage_GET(ushort port) {

            Handle.GET(port,"/admin/usage", (Request request) => {
                lock (LOCK) {
                    dynamic response = new DynamicJson();
                    try {
                        var result = Db.SlowSQL("SELECT o FROM StarcounterUsage o");

                        response.usage = new object[] { };
                        int i = 0;
                        foreach (StarcounterUsage item in result) {
                            response.usage[i++] = new {
                                id = item.GetObjectID(),
                                installationNo = (item.Installation == null) ? -1 : item.Installation.InstallationNo,
                                ip = item.IP,
                                mac = item.Mac,
                                version = item.Version,
                                date = item.Date,
                                availableRam = item.AvailableRam,
                                installedRam = item.InstalledRam,
                                cpu = item.Cpu,
                                os = item.Os,
                                transactions = item.Transactions,
                                databases = item.Databases,
                                runningDatabases = item.RunningDatabases,
                                runningExecutables = item.RunningExecutables
                            };
                        }

                        return new Response() { Body = response.ToString(), StatusCode = (ushort)System.Net.HttpStatusCode.OK };

                    }
                    catch (Exception e) {
                        return Utils.CreateErrorResponse(e);
                    }

                }
            });


        }

    }
}
