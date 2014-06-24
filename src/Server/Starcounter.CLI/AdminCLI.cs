using Starcounter.CommandLine;
using Starcounter.Internal;
using Starcounter.Rest.ExtensionMethods;
using Starcounter.Server.Rest;
using Starcounter.Server.Rest.Representations.JSON;
using System;
using System.Collections.Generic;

namespace Starcounter.CLI {
    /// <summary>
    /// Defines an API to for certain administrative tasks that we
    /// support from the CLI, such as listing running applications.
    /// </summary>
    public class AdminCLI {
        readonly ServerReference serverReference;
        readonly AdminAPI adminAPI;
        readonly Node node;

        /// <summary>
        /// Initialize a new <see cref="AdminCLI"/> instance, given
        /// a reference to a server.
        /// </summary>
        /// <param name="server">The server to bind to.</param>
        /// <param name="admin">Optional admin API to use.</param>
        public AdminCLI(ServerReference server, AdminAPI admin = null) {
            serverReference = server;
            adminAPI = admin ?? new AdminAPI();
            node = server.CreateNode();
        }

        /// <summary>
        /// Fetches the set of running applications found on the
        /// target admin server, optionally scoped by a specified
        /// database.
        /// </summary>
        /// <param name="database">Optional database to scope the
        /// request to.</param>
        /// <returns>A list of all applications matching the
        /// criteria, grouped by their database.
        /// </returns>
        public Dictionary<Engine, Executable[]> GetApplications(string database = null) {
            var admin = adminAPI;

            var result = new Dictionary<Engine, Executable[]>();
            var hosts = GetEngines(database);
            foreach (var host in hosts) {
                var apps = new List<Executable>();
                foreach (Engine.ExecutablesJson.ExecutingElementJson application in host.Executables.Executing) {
                    var response = node.GET(node.ToLocal(application.Uri));
                    response.FailIfNotSuccessOr(404);
                    if (response.IsSuccessStatusCode) {
                        var app = new Executable();
                        app.PopulateFromJson(response.Body);
                        apps.Add(app);
                    }
                }
                result.Add(host, apps.ToArray());
            }
            
            return result;
        }

        /// <summary>
        /// Gets the set of running database engines found on the target
        /// admin server, optionally limited to a single database.
        /// </summary>
        /// <param name="database">Optional database to scope the
        /// request to.</param>
        /// <returns>A list of all engines matching the criteria.
        /// </returns>
        public Engine[] GetEngines(string database = null) {
            var admin = adminAPI;

            var response = node.GET(admin.Uris.Engines);
            response.FailIfNotSuccessOr(503);
            if (response.StatusCode == 503) {
                throw ErrorCode.ToException(Error.SCERRSERVERNOTRUNNING);
            }

            // Iterate all engines (i.e. databases that are connected to at
            // least one running database process). Take the lightweight reference
            // and fetch the full engine from it.

            var engines = new EngineCollection();
            engines.PopulateFromJson(response.Body);
            var result = new List<Engine>(engines.Engines.Count);
            foreach (EngineCollection.EnginesElementJson engineRef in engines.Engines) {
                if (database != null && !engineRef.Name.Equals(database, StringComparison.InvariantCultureIgnoreCase)) {
                    continue;
                }

                response = node.GET(node.ToLocal(engineRef.Uri));
                response.FailIfNotSuccessOr(404);
                if (response.IsSuccessStatusCode) {
                    var engine = new Engine();
                    engine.PopulateFromJson(response.Body);
                }
            }

            return result.ToArray();
        }
    }
}