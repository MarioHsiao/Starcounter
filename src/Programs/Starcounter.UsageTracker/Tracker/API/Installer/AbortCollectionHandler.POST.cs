﻿using System;
using System.Net;
using Codeplex.Data;
using Starcounter;
using Starcounter.Advanced;
using Starcounter.Applications.UsageTrackerApp.Model;
using Starcounter.Applications.UsageTrackerApp.API.Starcounter;

namespace Starcounter.Applications.UsageTrackerApp.API.Installer {

    internal static class AbortCollectionHandler {

        private static Object LOCK = new Object();

        public static void Setup_POST(ushort port) {

            Handle.POST(port, "/api/usage/installer/abort", (Request request) => {
                lock (LOCK) {

                    try {
                        String content = request.Body;
                        IPAddress clientIP = request.ClientIpAddress;

                        int protocolVersion = Utils.GetRequestProtocolVersion(request);

                        dynamic incomingJson = DynamicJson.Parse(content);
                        dynamic response = new DynamicJson();

                        bool bValid = incomingJson.IsDefined("abort");
                        if (bValid == false) {
                            throw new FormatException("Invalid content format ");
                        }

                        dynamic data = incomingJson.abort;


                        Db.Transaction(() => {

                            string serial = data.downloadId;
                            Int64 installationNo = (Int64)data.installationNo;

                            Installation installation = StarcounterCollectionHandler.AssureInstallation(installationNo, serial);
                            InstallerAbort item = new InstallerAbort();
                            item.Installation = installation;


                            // Header
                            item.Date = DateTime.UtcNow;
                            item.IP = clientIP.ToString();
                            item.Mac = data.mac;
                            if (protocolVersion > 1) {
                                item.Version = data.version;
                            }

                            item.Mode = int.Parse(data.mode.ToString());
                            item.Message = data.message;

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
