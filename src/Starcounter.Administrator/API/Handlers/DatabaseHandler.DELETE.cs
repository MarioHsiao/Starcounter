
using Starcounter.Administrator.API.Utilities;
using Starcounter.Advanced;
using Starcounter.Server.PublicModel;
using Starcounter.Server.PublicModel.Commands;
using Starcounter.Server.Rest.Representations.JSON;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Administrator.API.Handlers {

    internal static partial class DatabaseHandler {
        /// <summary>
        /// Handles a DELETE for this resource.
        /// </summary>
        /// <param name="name">
        /// The name of the database delete.</param>
        /// <param name="request">
        /// The REST request.</param>
        /// <returns>The response to be sent back to the client.</returns>
        static object OnDELETE(string name, Request request) {
            var serverEngine = RootHandler.Host.Engine;
            var runtime = RootHandler.Host.Runtime;
            ErrorDetail errDetail;

            var applicationDatabase = runtime.GetDatabaseByName(name);
            if (applicationDatabase == null) {
                errDetail = RESTUtility.JSON.CreateError(Error.SCERRDATABASENOTFOUND);
                return RESTUtility.JSON.CreateResponse(errDetail.ToJson(), 404);
            }

			// From HTTP 1.1, RFC 2616:
            //
            // "A successful response SHOULD be 200 (OK) if the response includes
            // an entity describing the status, 202 (Accepted) if the action has not
            // yet been enacted, or 204 (No Content) if the action has been enacted
            // but the response does not include an entity."
			//
			// We will use 202 (if pending deleting database files) and 204 if all
			// was OK, deleted instantly.

            var stop = new DeleteDatabaseCommand(serverEngine, name, true);
            stop.EnableWaiting = true;

            var commandInfo = runtime.Execute(stop);
            if (stop.EnableWaiting) {
                commandInfo = runtime.Wait(commandInfo);
            }
            // Just to be sure we don't forget to change this some, once
            // we implement asynchronous requests.
            Trace.Assert(commandInfo.IsCompleted);
            
            if (commandInfo.HasError) {
                // Fix a good error handling response.
                // TODO:
                return 500;
            }

            return 204;
        }
    }
}