using Starcounter.Advanced;
using Starcounter.Server.PublicModel;
using Starcounter.Server.Rest.Representations.JSON;
using Starcounter.Administrator.API.Utilities;

namespace Starcounter.Administrator.API.Handlers {
    /// <summary>
    /// Provides the handlers for the admin server database
    /// engine resource.
    /// </summary>
    internal static partial class EngineHandler {
        static string uriTemplate;
        static string uriTemplateHostProcess;
        static string uriTemplateDbProcess;
        /// <summary>
        /// Provides a set of utility methods for working on strongly typed
        /// Engine JSON representations.
        /// </summary>
        internal static class JSON {
            /// <summary>
            /// Creates a JSON representation of the given application level
            /// database state object.
            /// </summary>
            /// <param name="state">The database whose semantics the JSON
            /// representation represents.</param>
            /// <returns>A strongly types JSON <see cref="Engine"/> instance.
            /// </returns>
            internal static Engine CreateRepresentation(DatabaseInfo state) {
                var admin = RootHandler.API;
                var engine = new Engine();
                var name = state.Name;

                engine.Uri = uriTemplate.ToAbsoluteUri(name);
                engine.Database.Name = name;
                engine.Database.Uri = admin.Uris.Database.ToAbsoluteUri(name);
                engine.CodeHostProcess.Uri = uriTemplateHostProcess.ToAbsoluteUri(name);
                engine.CodeHostProcess.PID = state.HostProcessId;
                engine.DatabaseProcess.Uri = uriTemplateDbProcess.ToAbsoluteUri(name);
                engine.DatabaseProcess.Running = state.DatabaseProcessRunning;
                engine.NoDb = state.HasNoDbSwitch();
                engine.LogSteps = state.HasLogStepsSwitch();

                return engine;
            }
        }

        /// <summary>
        /// Install handlers for the resource represented by this class and
        /// performs custom setup.
        /// </summary>
        internal static void Setup() {
            uriTemplate = RootHandler.API.Uris.Engine;
            uriTemplateHostProcess = uriTemplate + "/host";
            uriTemplateDbProcess = uriTemplate + "/db";

            // Handlers for the larger engine resource/abstraction
            Handle.GET<string, Request>(uriTemplate, OnGET);
            Handle.DELETE<string, Request>(uriTemplate, OnDELETE);
            RootHandler.Register405OnAllUnsupported(uriTemplate, new string[] { "GET", "DELETE" });

            // Handlers for referenced primary processes
            Handle.GET<string, Request>(uriTemplateHostProcess, OnHostGET);
            Handle.DELETE<string, Request>(uriTemplateHostProcess, OnHostDELETE);
            RootHandler.Register405OnAllUnsupported(uriTemplateHostProcess, new string[] { "GET", "DELETE" });
            Handle.GET<string, Request>(uriTemplateDbProcess, OnDatabaseProcessGET);
            RootHandler.Register405OnAllUnsupported(uriTemplateDbProcess, new string[] { "GET" });
        }
    }
}