using System;
using Codeplex.Data;
using Starcounter;
using Starcounter.Advanced;
using Starcounter.Server.PublicModel;
using System.Net;
using System.Diagnostics;
using Starcounter.Internal;
using Starcounter.Internal.Web;
using Starcounter.Administrator.API.Utilities;
using Starcounter.Administrator.Server.Utilities;
using System.IO;
using Administrator.Server.Utilities;

namespace Starcounter.Administrator.Server.Handlers {
    internal static partial class StarcounterAdminAPI {

        /// <summary>
        /// Register Server Settings PUT
        /// </summary>
        public static void ServerSettings_PUT(ushort port, IServerRuntime server) {

            // Save server settings
            Handle.PUT("/api/admin/servers/{?}/settings", (string name, ServerSettings settings, Request req) => {
                lock (LOCK) {

                    try {

                        // Validate
                        ValidationErrors validationErrors = RestUtils.GetValidationErrors(settings);
                        if (validationErrors.Items.Count > 0) {
                            return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.Forbidden, BodyBytes = validationErrors.ToJsonUtf8() };
                        }

                        ServerInfo serverInfo = Program.ServerInterface.GetServerInfo();
                        if (serverInfo == null) {
                            // Database not found
                            ErrorResponse errorResponse = new ErrorResponse();
                            errorResponse.Text = string.Format("Could not find the {0} server", name);
                            errorResponse.Helplink = "http://en.wikipedia.org/wiki/HTTP_404"; // TODO
                            return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NotFound, BodyBytes = errorResponse.ToJsonUtf8() };
                        }

                        serverInfo.Configuration.SystemHttpPort = (ushort)settings.SystemHttpPort;
                        serverInfo.Configuration.Save(serverInfo.ServerConfigurationPath);

                        string serverDir = Path.GetDirectoryName(serverInfo.Configuration.ConfigurationFilePath);


                        // Overwriting server config values.
                        Utils.ReplaceXMLParameterInFile(
                            Path.Combine(serverDir, StarcounterEnvironment.FileNames.GatewayConfigFileName),
                            MixedCodeConstants.GatewayInternalSystemPortSettingName,
                            settings.SystemHttpPort.ToString());

                        Utils.ReplaceXMLParameterInFile(
                            Path.Combine(serverDir, StarcounterEnvironment.FileNames.GatewayConfigFileName),
                            MixedCodeConstants.GatewayAggregationPortSettingName,
                            settings.AggregationPort.ToString());

                        // Return new settings
                        serverInfo = Program.ServerInterface.GetServerInfo();
                        if (serverInfo == null) {
                            // Database not found
                            ErrorResponse errorResponse = new ErrorResponse();
                            errorResponse.Text = string.Format("Could not find the {0} server", name);
                            errorResponse.Helplink = "http://en.wikipedia.org/wiki/HTTP_404"; // TODO
                            return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.NotFound, BodyBytes = errorResponse.ToJsonUtf8() };
                        }

                        ServerSettings serverSettings = RestUtils.CreateSettings(serverInfo);

                        return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.OK, BodyBytes = serverSettings.ToJsonUtf8() };
                    }
                    catch (Exception e) {
                        return RestUtils.CreateErrorResponse(e);
                    }
                }
            });
        }
    }



  
}
