using System;
using Codeplex.Data;
using Starcounter;
using Starcounter.Advanced;
using Starcounter.Programs.UsageTrackerApp.Model;

namespace Starcounter.Programs.UsageTrackerApp.API.Backend {
    internal static partial class Administrator {

        public static void Finish_GET(ushort port) {

            Handle.GET(port, "/admin/installerfinish", (Request request) => {
                lock (LOCK) {
                    dynamic response = new DynamicJson();
                    try {
                        var result = Db.SlowSQL("SELECT o FROM InstallerFinish o");

                        response.finish = new object[] { };
                        int i = 0;
                        foreach (InstallerFinish item in result) {
                            response.finish[i++] = new {
                                id = item.GetObjectID(),
                                installationNo = (item.Installation == null) ? -1 : item.Installation.InstallationNo,
                                ip = item.IP,
                                mac = item.Mac,
                                version = item.Version,
                                date = item.Date,
                                mode = item.Mode,
                                success = item.Success
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
