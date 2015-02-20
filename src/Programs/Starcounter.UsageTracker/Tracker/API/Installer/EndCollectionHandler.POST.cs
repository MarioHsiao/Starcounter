using System;
using System.Net;
using Codeplex.Data;
using Starcounter;
using Starcounter.Advanced;
using Starcounter.Applications.UsageTrackerApp.Model;
using Starcounter.Applications.UsageTrackerApp.API.Starcounter;

namespace Starcounter.Applications.UsageTrackerApp.API.Installer {
    internal static class EndCollectionHandler {

        private static Object LOCK = new Object();

        public static void Setup_POST(ushort port) {

            Handle.POST(port, "/api/usage/installer/end", (Request request) => {
                lock (LOCK) {

                    try {
                        String content = request.Body;
                        IPAddress clientIP = request.ClientIpAddress;

                        int protocolVersion = Utils.GetRequestProtocolVersion(request);

                        dynamic incomingJson = DynamicJson.Parse(content);
                        dynamic response = new DynamicJson();

                        bool bValid = incomingJson.IsDefined("end");
                        if (bValid == false) {
                            throw new FormatException("Invalid content format ");
                        }

                        dynamic data = incomingJson.end;

                        Db.Transact(() => {

                            string serial = data.downloadId;
                            Int64 installationNo = (Int64)data.installationNo;

                            Installation installation = StarcounterCollectionHandler.AssureInstallation(installationNo, serial);
                            InstallerEnd item = new InstallerEnd();
                            item.Installation = installation;

                            // Header
                            item.Date = DateTime.UtcNow;
                            item.IP = clientIP.ToString();
                            item.Mac = data.mac;
                            if (protocolVersion > 1) {
                                item.Version = data.version;
                            }

                            item.LinksClicked = data.linksClicked;

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
