using Starcounter.Advanced;
using Starcounter.Server.PublicModel;
using Starcounter.Server.Rest.Representations.JSON;
using Starcounter.Administrator.API.Utilities;
using System.Collections.Generic;
using System.Diagnostics;

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
            /// <param name="headers">Dictionary with headers that possibly
            /// will be added to if given and the state indicates we should
            /// do so.</param>
            /// <returns>A strongly types JSON <see cref="Engine"/> instance.
            /// </returns>
            internal static Engine CreateRepresentation(DatabaseInfo state, Dictionary<string, string> headers = null) {
                var admin = RootHandler.API;
                var engine = new Engine();
                var name = state.Name;
                var engineState = state.Engine;
                Trace.Assert(
                    engineState != null, "We should never hand out engine instances with no backing state.");

                engine.Uri = uriTemplate.ToAbsoluteUri(name);
                engine.Database.Name = name;
                engine.Database.Uri = admin.Uris.Database.ToAbsoluteUri(name);
                engine.CodeHostProcess.Uri = uriTemplateHostProcess.ToAbsoluteUri(name);
                engine.DatabaseProcess.Uri = uriTemplateDbProcess.ToAbsoluteUri(name);
                engine.CodeHostProcess.PID = engineState.HostProcessId;
                engine.DatabaseProcess.Running = engineState.DatabaseProcessRunning;
                engine.NoDb = engineState.HasNoDbSwitch();
                engine.LogSteps = engineState.HasLogStepsSwitch();
                engine.Executables.Uri = admin.Uris.Executables.ToAbsoluteUri(name);
                foreach (var executable in engineState.HostedApps) {
                    // Populate all executables
                    // TODO:
                }

                if (headers != null) {
                    headers.Add("ETag", engineState.Fingerprint);
                }

                return engine;
            }

            internal static Response CreateConditionBasedResponse(Request request, DatabaseInfo databaseInfo) {
                return CreateConditionBasedResponse(request, databaseInfo.Engine);
            }

            internal static Response CreateConditionBasedResponse(Request request, EngineInfo engineInfo) {
                // From RFC2616:
                //
                // "If the request would, without the If-Match header field, result
                // in anything other than a 2xx or 412 status, then the If-Match
                // header MUST be ignored".
                //
                // and
                //
                // The meaning of "If-Match: *" is that the method SHOULD be performed
                // if the representation selected by the origin server [...] exists,
                // and MUST NOT be performed if the representation does not exist".

                if (request["If-None-Match"] != null || request["If-Range"] != null)
                    return RESTUtility.JSON.CreateResponse(null, 501);

                var etag = request["If-Match"];
                if (etag != null) {
                    if (etag.Equals("*")) {
                        return RESTUtility.JSON.CreateResponse(null, 501);
                    }

                    if (engineInfo == null || !engineInfo.Fingerprint.Equals(etag)) {
                        var errDetail = RESTUtility.JSON.CreateError(Error.SCERRCOMMANDPRECONDITIONFAILED);
                        return RESTUtility.JSON.CreateResponse(errDetail.ToJson(), 412);
                    }
                }

                return null;
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