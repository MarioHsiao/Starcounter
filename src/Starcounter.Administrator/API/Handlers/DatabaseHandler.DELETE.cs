
using Starcounter.Administrator.API.Utilities;
using Starcounter.Advanced;
using Starcounter.Server.PublicModel;
using Starcounter.Server.PublicModel.Commands;
using Starcounter.Server.Rest.Representations.JSON;
using System.Diagnostics;

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
			// was OK (i.e. everything deleted instantly).

            var delete = new DeleteDatabaseCommand(serverEngine, name, true);
            delete.EnableWaiting = true;

            var commandInfo = runtime.Execute(delete);
            if (delete.EnableWaiting) {
                commandInfo = runtime.Wait(commandInfo);
            }
            // Just to be sure we don't forget to change this some, once
            // we implement asynchronous requests.
            Trace.Assert(commandInfo.IsCompleted);
            
            if (commandInfo.HasError) {
                return ErrorToResponse(commandInfo);
            }

            return 204;
        }

        static Response ErrorToResponse(CommandInfo commandInfo) {
            ErrorInfo single;

            single = null;
            if (ErrorInfoExtensions.TryGetSingleReasonErrorBasedOnServerConvention(commandInfo.Errors, out single)) {
                uint code = single.GetErrorCode();
                if (code == Error.SCERRDATABASENOTFOUND) {
                    var msg = single.ToErrorMessage();
                    var errDetail = RESTUtility.JSON.CreateError(msg.Code, msg.Body, msg.Helplink);
                    return RESTUtility.JSON.CreateResponse(errDetail.ToJson(), 404);
                }

                if (code == Error.SCERRDATABASERUNNING) {
					// Conflicting state; map to HTTP 409.
                    var msg = single.ToErrorMessage();
                    var errDetail = RESTUtility.JSON.CreateError(msg.Code, msg.Body, msg.Helplink);
                    return RESTUtility.JSON.CreateResponse(errDetail.ToJson(), 409);
                }

                if (code == Error.SCERRDELETEDBFILESPOSTPONED) {
					// This, we map to a success, but to a 202 to indicate
					// the server still has outstanding work to do.
                    var msg = single.ToErrorMessage();
                    var errDetail = RESTUtility.JSON.CreateError(msg.Code, msg.Body, msg.Helplink);
                    return RESTUtility.JSON.CreateResponse(errDetail.ToJson(), 202);
                }
            }

            if (single == null)
                single = commandInfo.Errors[0];

            // Create a 500. Right now we do it the sloppy way, letting
            // the server craft it for us based on the exception. We should
            // switch to a non-exception-based approach.

            throw single.ToErrorMessage().ToException();
        }
    }
}