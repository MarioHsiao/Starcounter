using System;
using System.Net;
using Codeplex.Data;
using Starcounter;
using Starcounter.Advanced;
using Starcounter.Applications.UsageTrackerApp.Model;
using Starcounter.Applications.UsageTrackerApp.API.Starcounter;

namespace Starcounter.Applications.UsageTrackerApp.API.Installer {
    internal static class ExecutingCollectionHandler {

        private static Object LOCK = new Object();

        public static void Setup_POST(ushort port) {

            Handle.POST(port, "/api/usage/installer/executing", (Request request) => {
                lock (LOCK) {

                    try {
                        String content = request.Body;
                        IPAddress clientIP = request.GetClientIpAddress();

                        int protocolVersion = Utils.GetRequestProtocolVersion(request);

                        dynamic incomingJson = DynamicJson.Parse(content);
                        dynamic response = new DynamicJson();

                        bool bValid = incomingJson.IsDefined("executing");
                        if (bValid == false) {
                            throw new FormatException("Invalid content format ");
                        }

                        dynamic data = incomingJson.executing;

                        Db.Transaction(() => {

                            string downloadId = data.downloadId;
                            int installationNo = int.Parse(data.installationNo.ToString());

                            Installation installation = StarcounterCollectionHandler.AssureInstallation(installationNo, downloadId);
                            InstallerExecuting item = new InstallerExecuting(installation);

                            // Header
                            item.Date = DateTime.Parse(data.date);
                            item.IP = clientIP.ToString();
                            item.Mac = data.mac;
                            if (protocolVersion > 1) {
                                item.Version = data.version;
                            }

                            item.Mode = int.Parse(data.mode.ToString());
                            item.PersonalServer = data.personalServer;
                            item.VS2012Extention = data.vs2012Extention;

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
