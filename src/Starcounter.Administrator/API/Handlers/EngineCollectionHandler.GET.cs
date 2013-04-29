
using Starcounter.Administrator.API.Utilities;
using Starcounter.Advanced;
using Starcounter.Server.Rest.Representations.JSON;

namespace Starcounter.Administrator.API.Handlers {

    internal static partial class EngineCollectionHandler {
        /// <summary>
        /// Handles a GET for this resource.
        /// </summary>
        /// <param name="request">The HTTP request.</param>
        /// <returns>The response to be sent back to the client.</returns>
        static object OnGET(Request request) {
            var server = RootHandler.Host.Runtime;
            var applicationDatabases = server.GetDatabases();
            
            var result = new EngineCollection();
            foreach (var db in applicationDatabases) {
                if (db.HostProcessId != 0) {
                    var engine = result.Engines.Add();
                    JSON.PopulateRefRepresentation(engine, db);
                }
            }

            return RESTUtility.CreateJSONResponse(result.ToJson());
        }
    }
}