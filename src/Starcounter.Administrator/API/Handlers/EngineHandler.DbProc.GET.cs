﻿
using Starcounter.Administrator.API.Utilities;
using Starcounter.Advanced;
using Starcounter.Server.Rest.Representations.JSON;

namespace Starcounter.Administrator.API.Handlers {

    internal static partial class EngineHandler {
        /// <summary>
        /// Handles a GET for the referenced database process resource.
        /// </summary>
        /// <param name="name">
        /// The name of the database whose engine the client is asking
        /// to GET the database process for.</param>
        /// <param name="request">
        /// The REST request.</param>
        /// <returns>The response to be sent back to the client.</returns>
        static Response OnDatabaseProcessGET(string name, Request request) {
            var server = RootHandler.Host.Runtime;
            var applicationDatabase = server.GetDatabaseByName(name);
            if (applicationDatabase == null) {
                var errDetail = RESTUtility.JSON.CreateError(Error.SCERRDATABASENOTFOUND);
                return RESTUtility.JSON.CreateResponse(errDetail.ToJson(), 404);
            }

            var engineInfo = applicationDatabase.Engine;
            if (engineInfo != null && engineInfo.DatabaseProcessRunning) {
                var host = new Engine.DatabaseProcessJson();
                host.Uri = uriTemplateDbProcess.ToAbsoluteUri(name);
                host.Running = true;
                return RESTUtility.JSON.CreateResponse(host.ToJson());
            }

            // We return an empty 404 to indicate we didn't find the process,
            // semantically equivalent to it is not running.
            return 404;
        }
    }
}