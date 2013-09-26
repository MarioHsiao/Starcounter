
using Starcounter.Administrator.API.Utilities;
using Starcounter.Advanced;
using Starcounter.Server.PublicModel;
using Starcounter.Server.PublicModel.Commands;
using System.Diagnostics;

namespace Starcounter.Administrator.API.Handlers {

    internal static partial class ExecutableHandler {
        /// <summary>
        /// Handles a DELETE of this resource.
        /// </summary>
        /// <param name="database">
        /// The name of the database hosting the executable collection
        /// represented by this resource.</param>
        /// <param name="executable">The identity of the executable to
        /// delete from the collection of executables (i.e. semantically,
        /// the executable to unload from the code host).
        /// </param>
        /// <param name="request">
        /// The REST request.</param>
        /// <returns>The response to be sent back to the client.</returns>
        static Response OnDELETE(string database, string executable, Request request) {
            var serverEngine = RootHandler.Host.Engine;
            var runtime = RootHandler.Host.Runtime;
            
            var stop = new StopExecutableCommand(serverEngine, database, executable);
            stop.EnableWaiting = true;
            
            var commandInfo = runtime.Execute(stop);
            if (stop.EnableWaiting) {
                commandInfo = runtime.Wait(commandInfo);
            }
            if (commandInfo.HasError) {
                return ToErrorResponse(commandInfo);
            }

            // Just to be sure we don't forget to change this some, once
            // we implement asynchronous requests.
            Trace.Assert(commandInfo.IsCompleted);

            return 204;
        }

        static Response ToErrorResponse(CommandInfo commandInfo) {
            var single = commandInfo.Errors.PickSingleServerError();
            int statusCode;

            switch (single.GetErrorCode()) {
                case Error.SCERRDATABASENOTFOUND:
                    statusCode = 404;
                    break;
                case Error.SCERREXECUTABLENOTRUNNING:
                case Error.SCERRDATABASEENGINENOTRUNNING:
                    // Conflicts with the state expected; we map to HTTP 409.
                    // An alternative would be to return 404 for these too.
                    statusCode = 409;
                    break;
                default:
                    statusCode = 500;
                    break;
            }

            var msg = single.ToErrorMessage();
            var errDetail = RESTUtility.JSON.CreateError(msg.Code, msg.Body, msg.Helplink);
            return RESTUtility.JSON.CreateResponse(errDetail.ToJson(), statusCode);
        }
    }
}
