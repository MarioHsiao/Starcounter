using System;
using Codeplex.Data;
using Starcounter;
using Starcounter.Advanced;
using Starcounter.Applications.UsageTrackerApp.Model;

namespace Starcounter.Applications.UsageTrackerApp.API.Backend {
    internal static partial class Administrator {

        public static void End_GET(ushort port) {

            Handle.GET(port, "/admin/installerend", (Request request) => {
                lock (LOCK) {
                    dynamic response = new DynamicJson();
                    try {
                        var result = Db.SlowSQL("SELECT o FROM InstallerEnd o");

                        response.end = new object[] { };
                        int i = 0;
                        foreach (InstallerEnd item in result) {
                            response.end[i++] = new {
                                id = item.GetObjectID(),
                                installationNo = (item.Installation == null) ? -1 : item.Installation.InstallationNo,
                                ip = item.IP,
                                mac = item.Mac,
                                version = item.Version,
                                date = item.Date,
                                linksClicked = item.LinksClicked
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
