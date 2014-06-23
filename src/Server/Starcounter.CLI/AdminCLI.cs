using Starcounter.CommandLine;
using Starcounter.Internal;
using Starcounter.Rest.ExtensionMethods;
using Starcounter.Server.Rest;
using Starcounter.Server.Rest.Representations.JSON;
using System;

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
        /// Display a list of running application in the console.
        /// </summary>
        public int ListApplications() {
            var admin = adminAPI;
            var serverHost = serverReference.Host;
            var serverName = serverReference.Name;
            var serverPort = serverReference.Port;

            try {
                
                var response = node.GET(admin.Uris.Engines);
                response.FailIfNotSuccessOr(503);

                if (response.StatusCode == 503) {
                    SharedCLI.ShowInformationAndSetExitCode(
                        "No applications running (server not running)", 
                        0, 
                        showStandardHints: true, 
                        color: ConsoleColor.DarkGray
                        );
                    return 0;
                }

                // Iterate all engines (i.e. databases that are connected to at
                // least one running database process). Take the lightweight reference
                // and fetch the full engine from it. Then display the most basic
                // info about each recorded running application.

                int count = 0;
                var engines = new EngineCollection();
                engines.PopulateFromJson(response.Body);
                foreach (EngineCollection.EnginesElementJson engineRef in engines.Engines) {
                    response = node.GET(node.ToLocal(engineRef.Uri));
                    response.FailIfNotSuccessOr(404);
                    if (response.IsSuccessStatusCode) {
                        var engine = new Engine();
                        engine.PopulateFromJson(response.Body);

                        foreach (Engine.ExecutablesJson.ExecutingElementJson application in engine.Executables.Executing) {
                            var id = application.Name;
                            if (!engine.Database.Name.Equals(StarcounterConstants.DefaultDatabaseName, 
                                StringComparison.InvariantCultureIgnoreCase)) {
                                id = engine.Database.Name + "\\" + id;
                            }

                            ConsoleUtil.ToConsoleWithColor(string.Format("[{0}] {1}", ++count, id), ConsoleColor.DarkYellow);
                            ConsoleUtil.ToConsoleWithColor(string.Format("Path: {0}", application.Path), ConsoleColor.White);
                            Console.WriteLine();
                        }
                    }
                }

                if (count == 0) {
                    SharedCLI.ShowInformationAndSetExitCode(
                        "No applications running",
                        0,
                        showStandardHints: true,
                        color: ConsoleColor.DarkGray
                        );
                }

                return count;

            } catch (Exception e) {
                Console.WriteLine(e.ToString());
                return -1;
            }
        }
    }
}
