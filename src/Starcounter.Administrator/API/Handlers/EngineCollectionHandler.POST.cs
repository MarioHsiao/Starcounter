
using Starcounter.Administrator.API.Utilities;
using Starcounter.Advanced;
using Starcounter.Internal;
using Starcounter.Server.PublicModel;
using Starcounter.Server.PublicModel.Commands;
using Starcounter.Server.Rest.Representations.JSON;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Starcounter.Administrator.API.Handlers {

    internal static partial class EngineCollectionHandler {

        /// <summary>
        /// Handles a POST to this resource. Posting to the collection of
        /// engines is semantically equal to starting an engine.
        /// </summary>
        /// <param name="request">
        /// The REST request.</param>
        /// <returns>The response to be sent back to the client.</returns>
        static object OnPOST(Request request) {
            var serverEngine = RootHandler.Host.Engine;
            var runtime = RootHandler.Host.Runtime;
            var admin = RootHandler.API;
            ErrorDetail errDetail;

            // From RFC2616:
            // "The posted entity is subordinate to that URI in the same way
            // that a file is subordinate to a directory containing it, a news
            // article is subordinate to a newsgroup to which it is posted,
            // or a record is subordinate to a database".
            //
            // What it means to this mapping is that we don't expect a full
            // Engine representation, but rather a reference to an engine as
            // defined in XSON-based class EngineCollection, the Engines
            // array instances.

            var engine = new EngineCollection.EnginesApp();
            engine.PopulateFromJson(request.GetBodyStringUtf8_Slow());

            var name = engine.Name;
            if (string.IsNullOrWhiteSpace(name)) {
                errDetail = new ErrorDetail() {
                    Text = "Database engine name not specified",
                    ServerCode = Error.SCERRUNSPECIFIED
                };
                return RESTUtility.JSON.CreateResponse(errDetail.ToJson(), 422);
            }

            var startCommand = new StartDatabaseCommand(serverEngine, name);
            startCommand.EnableWaiting = true;
            startCommand.NoDb = engine.NoDb;
            startCommand.LogSteps = engine.LogSteps;

            var commandInfo = runtime.Execute(startCommand);
            Trace.Assert(commandInfo.ProcessorToken == StartDatabaseCommand.DefaultProcessor.Token);
            if (startCommand.EnableWaiting) {
                commandInfo = runtime.Wait(commandInfo);
            }

            if (commandInfo.HasError) {
                ErrorInfo single;
                ErrorMessage msg;

                single = null;
                if (ErrorInfoExtensions.TryGetSingleReasonErrorBasedOnServerConvention(commandInfo.Errors, out single)) {
                    if (single.GetErrorCode() == Starcounter.Error.SCERRDATABASENOTFOUND) {
                        msg = single.ToErrorMessage();
                        errDetail = new ErrorDetail();
                        errDetail.Text = msg.Body;
                        errDetail.ServerCode = Error.SCERRDATABASENOTFOUND;
                        errDetail.Helplink = msg.Helplink;
                        return RESTUtility.JSON.CreateResponse(errDetail.ToJson(), 404);
                    }
                }

                if (single == null) {
                    single = commandInfo.Errors[0];
                }
                msg = single.ToErrorMessage();

                errDetail = RESTUtility.JSON.CreateError(msg.Code, msg.ToString(), msg.Helplink);
                return RESTUtility.JSON.CreateResponse(errDetail.ToJson(), 500);
            }

            // Check what processes were actually started and decide on
            // status code to use based on that.
            var startedHost = !commandInfo.GetProgressOf(
                StartDatabaseCommand.DefaultProcessor.Tasks.StartCodeHostProcess).WasCancelled;
            var startedDatabase = !commandInfo.GetProgressOf(
                StartDatabaseCommand.DefaultProcessor.Tasks.StartDatabaseProcess).WasCancelled;

            if (!startedHost && !startedDatabase) {
                // The request was fullfilled but nothing was actually
                // created, indicating the engine was already running.
                // According to specification, we should include "an
                // entity describing or containing the result of the
                // action". We just reuse the representation passed in
                // with a simple addition: setting its URI.
                engine.Uri = RootHandler.MakeAbsoluteUri(admin.Uris.Engine, name);
                return RESTUtility.JSON.CreateResponse(engine.ToJson(), 200);
            }

            // Derive a representation of the engine resource based on
            // the server application state. According to specification,
            // "the newly created resource can be referenced by the URI(s)
            // returned in the entity of the response, with the most
            // specific URI for the resource given by a Location header
            // field. The response SHOULD include an entity containing a
            // list of resource characteristics and location(s)".
            //
            // The tricky part here is that, to be correct, we must check
            // again the application state to see that it's semantics havent
            // changed since we got an OK from the server engine. We could
            // well be up for a suprise, when we find the state changed (if
            // some other client has caused it's manipulation between the
            // successfull processing of our command and "now". Currently,
            // we tackle this as 500. To be completly correct, we must have
            // the server capture the principal state of the engine before
            // it allows the execution of subsequent commands, and we should
            // return that state.

            var state = runtime.GetDatabaseByName(name);
            if (state == null || state.HostProcessId == 0) {
                errDetail = RESTUtility.JSON.CreateError(Error.SCERRDATABASEENGINETERMINATED);
                return RESTUtility.JSON.CreateResponse(errDetail.ToJson(), 500);
            }

            var headers = new Dictionary<string, string>(1);
            var result = EngineHandler.JSON.CreateRepresentation(state);
            headers.Add("Location", result.Uri);

            return RESTUtility.JSON.CreateResponse(result.ToJson(), 201, headers);
        }
    }
}