using System;
using Codeplex.Data;
using Starcounter;
using Starcounter.Advanced;
using Starcounter.Programs.UsageTrackerApp.Model;

namespace Starcounter.Programs.UsageTrackerApp.API.Backend {
    internal static partial class Administrator {

        public static void Executing_GET(ushort port) {

            Handle.GET(port,"/admin/installerexecuting", (Request request) => {
                lock (LOCK) {
                    dynamic response = new DynamicJson();
                    try {
                        var result = Db.SlowSQL("SELECT o FROM InstallerExecuting o");

                        response.executing = new object[] { };
                        int i = 0;
                        foreach (InstallerExecuting item in result) {
                            response.executing[i++] = new {
                                id = item.GetObjectID(),
                                installationNo = (item.Installation == null) ? -1 : item.Installation.InstallationNo,
                                ip = item.IP,
                                mac = item.Mac,
                                version = item.Version,
                                date = item.Date,
                                mode = item.Mode,
                                personalServer = item.PersonalServer,
                                vs2012Extention = item.VS2012Extention
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
