using System;
using System.Net;
using Codeplex.Data;
using Starcounter;
using Starcounter.Advanced;
using Starcounter.Programs.UsageTrackerApp.Model;

namespace Starcounter.Programs.UsageTrackerApp.API.Installer {
    internal static class StartCollectionHandler {

        private static Object LOCK = new Object();

        public static void Setup_POST(ushort port) {

            Handle.POST(port, "/api/usage/installer/start", (Request request) => {

                lock (LOCK) {

                    try {
                        String content = request.GetBodyStringUtf8_Slow();
                        IPAddress clientIP = request.GetClientIpAddress();

                        int protocolVersion = Utils.GetRequestProtocolVersion(request);

                        dynamic incomingJson = DynamicJson.Parse(content);
                        dynamic response = new DynamicJson();

                        bool bValid = incomingJson.IsDefined("start");
                        if (bValid == false) {
                            throw new FormatException("Invalid content format ");
                        }

                        dynamic data = incomingJson.start;

                        Db.Transaction(() => {

                            string downloadId = data.downloadId;
                            int previousInstallationNo = int.Parse(data.installationNo.ToString());

                            Installation installation = new Installation(downloadId, previousInstallationNo);
                            InstallerStart item = new InstallerStart(installation);

                            // Header
                            item.Date = DateTime.Parse(data.date);
                            item.IP = clientIP.ToString();
                            item.Mac = data.mac;
                            if (protocolVersion > 1) {
                                item.Version = data.version;
                            }

                            // Environment
                            item.InstalledRam = long.Parse(data.installedRam.ToString());
                            item.AvailableRam = long.Parse(data.availableRam.ToString());
                            item.Cpu = data.cpu;
                            item.Os = data.os;

                            // Create response Response
                            response.installation = new { };
                            if (protocolVersion == 1) {
                                response.installation.sequenceNo = installation.InstallationNo;
                            }
                            else {
                                response.installation.installationNo = installation.InstallationNo;
                            }

                        });


                        return new Response() { Body = response.ToString(), StatusCode = (ushort)System.Net.HttpStatusCode.Created };


                    }
                    catch (Exception e) {
                        return Utils.CreateErrorResponse(e);
                    }

                }

            });

        }

    }
}
