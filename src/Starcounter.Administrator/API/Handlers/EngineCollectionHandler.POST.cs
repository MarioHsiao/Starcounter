
using Starcounter.Administrator.API.Utilities;
using Starcounter.Advanced;
using Starcounter.Server.PublicModel;
using Starcounter.Server.PublicModel.Commands;
using Starcounter.Server.Rest.Resources;
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
            var engine = new Engine();
            engine.PopulateFromJson(request.GetBodyStringUtf8_Slow());

            var name = engine.Database.Name;
            if (string.IsNullOrWhiteSpace(name)) {
                errDetail = new ErrorDetail() {
                    Text = "Database engine name not specified",
                    ServerCode = Error.SCERRUNSPECIFIED
                };
                return RESTUtility.CreateJSONResponse(errDetail.ToJson(), 422);
            }

            var startCommand = new StartDatabaseCommand(serverEngine, name);
            startCommand.EnableWaiting = true;

            var commandInfo = runtime.Execute(startCommand);
            Trace.Assert(commandInfo.ProcessorToken == StartDatabaseCommand.DefaultProcessor.Token);
            if (startCommand.EnableWaiting) {
                commandInfo = runtime.Wait(commandInfo);
            }

            if (commandInfo.HasError) {
                ErrorInfo single;

                single = null;
                if (ErrorInfoExtensions.TryGetSingleReasonErrorBasedOnServerConvention(commandInfo.Errors, out single)) {
                    if (single.GetErrorCode() == Starcounter.Error.SCERRDATABASENOTFOUND) {
                        var msg = single.ToErrorMessage();
                        errDetail = new ErrorDetail();
                        errDetail.Text = msg.Body;
                        errDetail.ServerCode = Error.SCERRDATABASENOTFOUND;
                        errDetail.Helplink = msg.Helplink;
                        return RESTUtility.CreateJSONResponse(errDetail.ToJson(), 404);
                    }
                }

                if (single == null)
                    single = commandInfo.Errors[0];

                // Create a 500. Right now we do it the sloppy way, letting
                // the server craft it for us based on the exception. We should
                // switch to a non-exception-based approach.

                throw single.ToErrorMessage().ToException();
            }

            // Check what processes were actually started and decide on
            // status code to use based on that.
            var startedHost = !commandInfo.GetProgressOf(
                StartDatabaseCommand.DefaultProcessor.Tasks.StartCodeHostProcess).IsCancelled;
            var startedDatabase = !commandInfo.GetProgressOf(
                StartDatabaseCommand.DefaultProcessor.Tasks.StartDatabaseProcess).IsCancelled;

            var state = runtime.GetDatabaseByName(name);
            engine = new Engine();
            engine.Uri = RootHandler.MakeAbsoluteUri(admin.Uris.Engine, name);

            if (!startedHost && !startedDatabase) {
                // The request was fullfilled but nothing was actually
                // created, indicating the engine was already running.
                // According to specification, we should include "an
                // entity describing or containing the result of the
                // action".
                return RESTUtility.CreateJSONResponse(engine.ToJson(), 200);
            }

            // Derive a representation of the engine resource based on
            // the server application state. According to specification,
            // "the newly created resource can be referenced by the URI(s)
            // returned in the entity of the response, with the most
            // specific URI for the resource given by a Location header
            // field. The response SHOULD include an entity containing a
            // list of resource characteristics and location(s)".

            var headers = new Dictionary<string, string>(1);
            engine.Database.Name = name;
            engine.Database.Uri = RootHandler.MakeAbsoluteUri(admin.Uris.Database, name);
            engine.CodeHostProcess.PID = state.HostProcessId;
            engine.DatabaseProcess.PID = -1;
            headers.Add("Location", engine.Uri);

            return RESTUtility.CreateJSONResponse(engine.ToJson(), 201, headers);
        }
    }
}