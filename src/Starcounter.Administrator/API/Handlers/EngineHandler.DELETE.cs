
using Codeplex.Data;
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

            // Just to be sure we don't forget to change this some, once
            // we implement asynchronous requests.
            Trace.Assert(commandInfo.IsCompleted);

            // Check if there is a specific exit code and it indicates a failing
            // precondition.
            conditionFailed = ToResponseIfPreconditionFailed(commandInfo);
            if (conditionFailed != null) return conditionFailed;

            // Check what processes were actually stopped and decide on
            // the status code to use based on that. We make a difference if
            // we actually stopped something or not. Both are considered
            // successful in terms of the service we offer (to assure an
            // engine is stopped), but if we find nothing was actually stopped,
            // we include an entity for this (and return 200). The normal
            // scenario is to return 204 with no content.

            var stoppedHost = !commandInfo.GetProgressOf(
                StopDatabaseCommand.DefaultProcessor.Tasks.StopCodeHostProcess).WasCancelled;
            var stoppedDatabase = !commandInfo.GetProgressOf(
                StopDatabaseCommand.DefaultProcessor.Tasks.StopDatabaseProcess).WasCancelled;

            if (!stoppedHost && !stoppedDatabase) {
                dynamic nothingStopped = new DynamicJson();
                nothingStopped.Code = Error.SCERRDATABASEENGINENOTRUNNING;
                nothingStopped.Message = ErrorCode.ToMessage(Error.SCERRDATABASEENGINENOTRUNNING).Body;
                return RESTUtility.JSON.CreateResponse(nothingStopped.ToString());
            }

            return 204;
        }

        static Response ToResponseIfPreconditionFailed(CommandInfo commandInfo) {
            if (commandInfo.ExitCode.HasValue &&
                commandInfo.ExitCode.Value == Error.SCERRCOMMANDPRECONDITIONFAILED) {
                var detail = RESTUtility.JSON.CreateError(Error.SCERRCOMMANDPRECONDITIONFAILED);
                return RESTUtility.JSON.CreateResponse(detail.ToJson(), 412);
            }
            return null;
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