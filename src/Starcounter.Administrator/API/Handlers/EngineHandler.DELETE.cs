
using Starcounter.Administrator.API.Utilities;
using Starcounter.Advanced;
using Starcounter.Server.PublicModel;
using Starcounter.Server.PublicModel.Commands;
using Starcounter.Server.Rest.Representations.JSON;
using System.Collections.Generic;
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
        static object OnDELETE(string name, Request request) {
            var serverEngine = RootHandler.Host.Engine;
            var runtime = RootHandler.Host.Runtime;
            ErrorDetail errDetail;

            var applicationDatabase = runtime.GetDatabaseByName(name);
            if (applicationDatabase == null) {
                errDetail = RESTUtility.JSON.CreateError(Error.SCERRDATABASENOTFOUND);
                return RESTUtility.JSON.CreateResponse(errDetail.ToJson(), 404);
            }
            var applicationEngine = applicationDatabase.Engine;

            // Do entry-level lookup before we pump the command through
            // the server loop, to check if the condition is already not
            // met. This offloads work from the server, but does not
            // prevent the check to be done again in a safe context during
            // the execution of the command. This is doable because we
            // know that fingerprints will never be reused so as soon as
            // the change, we can be 100% sure they will not get back to
            // how they once were (i.e. the fingerprint we have) on entering.
            var conditionFailed = JSON.CreateConditionBasedResponse(request, applicationEngine);
            if (conditionFailed != null) return conditionFailed;
            var etag = request["If-Match"];

            var stop = new StopDatabaseCommand(serverEngine, name, true);
            stop.EnableWaiting = true;
            stop.Fingerprint = etag;

            var commandInfo = runtime.Execute(stop);
            Trace.Assert(commandInfo.ProcessorToken == StopDatabaseCommand.DefaultProcessor.Token);
            if (stop.EnableWaiting) {
                commandInfo = runtime.Wait(commandInfo);
            }
            if (commandInfo.HasError) {
                return ToErrorResponse(commandInfo);
            }

            // Just to be sure we don't forget to change this all once
            // we implement asynchronous requests.
            Trace.Assert(commandInfo.IsCompleted);

            // Check if there is a specific exit code and it indicates a failing
            // precondition.
            if (commandInfo.ExitCode.HasValue &&
                commandInfo.ExitCode.Value == Error.SCERRCOMMANDPRECONDITIONFAILED) {
                errDetail = RESTUtility.JSON.CreateError(Error.SCERRCOMMANDPRECONDITIONFAILED);
                return RESTUtility.JSON.CreateResponse(errDetail.ToJson(), 412);
            }

            // Check what processes were actually stopped and decide on
            // the status code to use based on that.
            var stoppedHost = !commandInfo.GetProgressOf(
                StopDatabaseCommand.DefaultProcessor.Tasks.StopCodeHostProcess).WasCancelled;
            var stoppedDatabase = !commandInfo.GetProgressOf(
                StopDatabaseCommand.DefaultProcessor.Tasks.StopDatabaseProcess).WasCancelled;

            if (!stoppedHost && !stoppedDatabase) {
                // No process was actually stopped. This implies an attempt
                // to stop an engine that was not running. We distinguish this
                // from the normal result by returning 204.
                return 204;
            }

            // Return a representation that indicates the engine is stopped. We
            // can and should NOT query the application object model for the
            // engine, since some other client or server mechanism might already
            // be busy restarting the engine, and the semantics we offer is that
            // we have succeded stopping the engine, so the result must reflect
            // that. The important aspect is that right after the completion of
            // the stop command, the engine WAS in fact stopped, and thats what
            // we need to tell the client or agent.
            //   The result of any successful stop command should contain the
            // snapshot of the state at the completion of the command.
            applicationDatabase = (DatabaseInfo)commandInfo.Result;

            var headers = new Dictionary<string, string>(1);
            var stoppedEngine = EngineHandler.JSON.CreateRepresentation(applicationDatabase, headers);
            
            return RESTUtility.JSON.CreateResponse(stoppedEngine.ToJson(), 200, headers);
        }

        static Response ToErrorResponse(CommandInfo commandInfo) {
            ErrorInfo single;

            single = null;
            if (ErrorInfoExtensions.TryGetSingleReasonErrorBasedOnServerConvention(commandInfo.Errors, out single)) {
                if (single.GetErrorCode() == Error.SCERRDATABASENOTFOUND) {
                    var msg = single.ToErrorMessage();
                    var errDetail = RESTUtility.JSON.CreateError(msg.Code, msg.Body, msg.Helplink);
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
    }
}