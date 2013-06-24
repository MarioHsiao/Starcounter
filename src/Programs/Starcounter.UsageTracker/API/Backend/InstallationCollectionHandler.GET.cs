using System;
using Codeplex.Data;
using Starcounter;
using Starcounter.Advanced;
using Starcounter.Applications.UsageTrackerApp.Model;

namespace Starcounter.Applications.UsageTrackerApp.API.Backend {
    internal static partial class Administrator {

        public static void Installation_GET(ushort port) {

            Handle.GET(port,"/admin/installations", (Request request) => {
                lock (LOCK) {
                    dynamic response = new DynamicJson();
                    try {
                        var result = Db.SlowSQL("SELECT o FROM Installation o");

                        response.installation = new object[] { };
                        int i = 0;
                        foreach (Installation item in result) {
                            response.installation[i++] = new {
                                id = item.GetObjectID(),
                                date = item.Date,
                                installationNo = item.InstallationNo,
                                previousInstallationNo = item.PreviousInstallationNo,
                                downloadId = item.DownloadID
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
