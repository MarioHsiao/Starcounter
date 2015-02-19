using System;
using System.Net;
using Codeplex.Data;
using Starcounter;
using Starcounter.Advanced;
using Starcounter.Applications.UsageTrackerApp.Model;

namespace Starcounter.Applications.UsageTrackerApp.API.Installer {
    internal static class StartCollectionHandler {

        private static Object LOCK = new Object();

        public static void Setup_POST(ushort port) {

            Handle.POST(port, "/api/usage/installer/start", (Request request) => {

                lock (LOCK) {

                    try {
                        String content = request.Body;
                        IPAddress clientIP = request.ClientIpAddress;

                        int protocolVersion = Utils.GetRequestProtocolVersion(request);

                        dynamic incomingJson = DynamicJson.Parse(content);
                        dynamic response = new DynamicJson();

                        bool bValid = incomingJson.IsDefined("start");
                        if (bValid == false) {
                            throw new FormatException("Invalid content format ");
                        }

                        dynamic data = incomingJson.start;

                        Db.Transact(() => {

                            string serial = data.downloadId;
                            Int64 previousInstallationNo = (Int64)data.installationNo;

                            //Installation installation = new Installation(serial, previousInstallationNo);
                            Installation installation = new Installation();
                            installation.Serial = serial;
                            installation.Date = DateTime.UtcNow;

                            DateTime d = new DateTime(2000, 1, 1);

                            installation.InstallationNo = DateTime.UtcNow.Ticks - d.Ticks;
                            installation.PreviousInstallationNo = previousInstallationNo;

                            InstallerStart item = new InstallerStart();
                            item.Installation = installation;

                            // Header
                            item.Date = DateTime.UtcNow;
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
