﻿
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
        static Response OnGET(Request request) {
            var server = RootHandler.Host.Runtime;
            var applicationDatabases = server.GetDatabases();
            
            var result = new EngineCollection();
            foreach (var db in applicationDatabases) {
                var appEngine = db.Engine;
                if (appEngine != null) {
                    var engine = result.Engines.Add();
                    JSON.PopulateRefRepresentation(engine, db);
                }
            }

            return RESTUtility.JSON.CreateResponse(result.ToJson());
        }
    }
}