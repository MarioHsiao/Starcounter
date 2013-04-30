
using Starcounter.Administrator.API.Utilities;
using Starcounter.Advanced;
using Starcounter.Server.PublicModel;
using Starcounter.Server.PublicModel.Commands;
using Starcounter.Server.Rest.Representations.JSON;
using System.Diagnostics;

namespace Starcounter.Administrator.API.Handlers {

    internal static partial class EngineHandler {
        /// <summary>
        /// Handles a DELETE for this resource.
        /// </summary>
        /// <param name="name">
        /// The name of the database whose engine the client issues a
        /// DELETE on.</param>
        /// <param name="request">The REST request.</param>
        /// <returns>The response to be sent back to the client.</returns>
        static object OnHostDELETE(string name, Request request) {
            var serverEngine = RootHandler.Host.Engine;
            var runtime = RootHandler.Host.Runtime;
            ErrorDetail errDetail;

            var applicationDatabase = runtime.GetDatabaseByName(name);
            if (applicationDatabase == null) {
                errDetail = RESTUtility.JSON.CreateError(Error.SCERRDATABASENOTFOUND);
                return RESTUtility.JSON.CreateResponse(errDetail.ToJson(), 404);
            }

            var stop = new StopDatabaseCommand(serverEngine, name, false);
            stop.EnableWaiting = true;

            var commandInfo = runtime.Execute(stop);
            Trace.Assert(commandInfo.ProcessorToken == StopDatabaseCommand.DefaultProcessor.Token);
            if (stop.EnableWaiting) {
                commandInfo = runtime.Wait(commandInfo);
            }

            if (commandInfo.HasError) {
                ErrorInfo single;

                single = null;
                if (ErrorInfoExtensions.TryGetSingleReasonErrorBasedOnServerConvention(commandInfo.Errors, out single)) {
                    if (single.GetErrorCode() == Error.SCERRDATABASENOTFOUND) {
                        var msg = single.ToErrorMessage();
                        errDetail = RESTUtility.JSON.CreateError(msg.Code, msg.Body, msg.Helplink);
                        return RESTUtility.JSON.CreateResponse(errDetail.ToJson(), 404);
                    }
                }

                if (single == null)
                    single = commandInfo.Errors[0];

                // Create a 500. Right now we do it the sloppy way, letting
                // the server craft it for us based on the exception. We should
                // switch to a non-exception-based approach.

                throw single.ToErrorMessage().ToException();
            }

            // Check if the process was actually stopped and decide on
            // the status code to use based on that.
            var stoppedHost = !commandInfo.GetProgressOf(
                StopDatabaseCommand.DefaultProcessor.Tasks.StopCodeHostProcess).WasCancelled;
            
            if (!stoppedHost) {
                // The process was already stopped. This implies an attempt
                // to stop an engine that was not running. We distinguish this
                // from the normal result by returning 204.
                return 204;
            }

            // Return a representation that indicates the host is stopped. We
            // can and should NOT query the application object model for the
            // engine, since some other client or server mechanism might already
            // be busy restarting the engine, and the semantics we offer is that
            // we have succeded stopping the engine, so the result must reflect
            // that. The important aspect is that right after the completion of
            // the stop command, the host WAS in fact stopped, and thats what
            // we need to tell the client or agent.

            // Not doable right now, and we must consider etagging.
            // Will be adressed shortly.
            // TODO:
            
            // applicationDatabase.HostedApps = new AppInfo[0];
            // applicationDatabase.HostProcessId = 0;

            var stoppedCodeHost = EngineHandler.JSON.CreateRepresentation(applicationDatabase);
            return RESTUtility.JSON.CreateResponse(stoppedCodeHost.ToJson());
        }
    }
}