using Starcounter.Advanced;
using Starcounter.Server.PublicModel;
using System;
using System.Diagnostics;

namespace Starcounter.Administrator.API.Handlers {
    using Server = Starcounter.Server.Rest.Representations.JSON.Server;

    internal static partial class ServerHandler {
        /// <summary>
        /// Implemenets the endpoint for GET requests made for the
        /// server domain object.
        /// </summary>
        /// <param name="request">The request being made.</param>
        /// <returns>A response corresponding to the request.</returns>
        static Response OnGET(Request request) {
            var server = RootHandler.Host.Runtime;
            var info = server.GetServerInfo();
            return ToJSONServer(info);
        }

        static Server ToJSONServer(ServerInfo serverInfo) {
            var admin = RootHandler.API;
            var server = new Server();
            var process = Process.GetCurrentProcess();

            server.Uri = admin.Uris.Server.ToAbsoluteUri();
            server.Configuration.FilePath = serverInfo.ServerConfigurationPath;
            server.StartTime = process.StartTime.ToString("s", System.Globalization.CultureInfo.InvariantCulture);
            try {
                server.Context = string.Format("{0}@{1}",
                    Environment.UserName.ToLowerInvariant(),
                    Environment.MachineName.ToLowerInvariant()
                    );
            } catch {
                server.Context = string.Empty;
            }

            return server;
        }
    }
}
