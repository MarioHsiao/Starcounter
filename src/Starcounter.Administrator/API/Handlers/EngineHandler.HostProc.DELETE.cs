
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
        static object OnHostDELETE(string name, Request request) {
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

            var stop = new StopDatabaseCommand(serverEngine, name, false);
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

            var result = (DatabaseInfo) commandInfo.Result;

            // Check if the process was actually stopped and decide on
            // the status code to use based on that.
            //
            // We use the following scheme status scheme for success:
            //  * 200 - the host was already stopped.
            //  * 204 - the code host was stopped, the database is still running.
            //  * 205 - the code host was stopped, resulting in the stopping of the
            //  engine. The client should clear its view and refetch the engine,
            //  which will not exist any longer.

            if (result.Engine == null)
                return 205;
            
            var stoppedHost = !commandInfo.GetProgressOf(
                StopDatabaseCommand.DefaultProcessor.Tasks.StopCodeHostProcess).WasCancelled;

            if (!stoppedHost) {
                dynamic nothingStopped = new DynamicJson();
                nothingStopped.Code = Error.SCERRDATABASEENGINENOTRUNNING;
                nothingStopped.Message = ErrorCode.ToMessage(Error.SCERRDATABASEENGINENOTRUNNING).Body;
                return RESTUtility.JSON.CreateResponse(nothingStopped.ToString());
            }

            return 204;
        }
    }
}