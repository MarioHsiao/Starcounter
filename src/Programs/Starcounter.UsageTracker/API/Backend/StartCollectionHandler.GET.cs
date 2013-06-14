using System;
using Codeplex.Data;
using Starcounter;
using Starcounter.Advanced;
using Starcounter.Programs.UsageTrackerApp.Model;

namespace Starcounter.Programs.UsageTrackerApp.API.Backend {
    internal static partial class Administrator {

        public static void Start_GET(ushort port) {

            Handle.GET(port,"/admin/installerstart", (Request request) => {
                lock (LOCK) {
                    dynamic response = new DynamicJson();
                    try {
                        var result = Db.SlowSQL("SELECT o FROM InstallerStart o");

                        response.start = new object[] { };
                        int i = 0;
                        foreach (InstallerStart item in result) {
                            response.start[i++] = new {
                                id = item.GetObjectID(),
                                installationNo = (item.Installation == null) ? -1 : item.Installation.InstallationNo,
                                ip = item.IP,
                                mac = item.Mac,
                                version = item.Version,
                                date = item.Date,
                                cpu = item.Cpu,
                                os = item.Os,
                                availableRam = item.AvailableRam,
                                installedRam = item.InstalledRam
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
