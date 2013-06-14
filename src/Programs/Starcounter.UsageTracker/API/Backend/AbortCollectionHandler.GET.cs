using System;
using Codeplex.Data;
using Starcounter;
using Starcounter.Advanced;
using Starcounter.Programs.UsageTrackerApp.Model;

namespace Starcounter.Programs.UsageTrackerApp.API.Backend {
    internal static partial class Administrator {

        public static void Abort_GET(ushort port) {

            Handle.GET(port, "/admin/installerabort", (Request request) => {
                lock (LOCK) {
                    dynamic response = new DynamicJson();
                    try {
                        var result = Db.SlowSQL("SELECT o FROM InstallerAbort o");

                        response.abort = new object[] { };
                        int i = 0;
                        foreach (InstallerAbort item in result) {
                            response.abort[i++] = new {
                                id = item.GetObjectID(),
                                installationNo = (item.Installation == null) ? -1 : item.Installation.InstallationNo,
                                ip = item.IP,
                                mac = item.Mac,
                                version = item.Version,
                                date = item.Date,
                                mode = item.Mode,
                                message = item.Message
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
