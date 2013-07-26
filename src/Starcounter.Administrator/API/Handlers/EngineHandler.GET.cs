
using Starcounter.Administrator.API.Utilities;
using Starcounter.Advanced;
using System.Collections.Generic;

namespace Starcounter.Administrator.API.Handlers {

    internal static partial class EngineHandler {
        /// <summary>
        /// Handles a GET for this resource.
        /// </summary>
        /// <param name="name">
        /// The name of the database whose engine the client request a
        /// representation of.</param>
        /// <param name="request">
        /// The REST request.</param>
        /// <returns>The response to be sent back to the client.</returns>
        static Response OnGET(string name, Request request) {
            var server = RootHandler.Host.Runtime;
            var applicationDatabase = server.GetDatabaseByName(name);
            if (applicationDatabase == null) {
                var errDetail = RESTUtility.JSON.CreateError(Error.SCERRDATABASENOTFOUND);
                return RESTUtility.JSON.CreateResponse(errDetail.ToJson(), 404);
            }

            var engineState = applicationDatabase.Engine;
            if (engineState == null) {
                var errDetail = RESTUtility.JSON.CreateError(Error.SCERRDATABASEENGINENOTRUNNING);
                return RESTUtility.JSON.CreateResponse(errDetail.ToJson(), 404);
            }

            var conditionFailed = JSON.CreateConditionBasedResponse(request, engineState, true);
            if (conditionFailed != null) return conditionFailed;

            var headers = new Dictionary<string, string>(1);
            var engine = EngineHandler.JSON.CreateRepresentation(applicationDatabase, headers);
            
            return RESTUtility.JSON.CreateResponse(engine.ToJson(), 200, headers);
        }
    }
}