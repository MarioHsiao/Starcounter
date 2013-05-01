
using Starcounter.Administrator.API.Utilities;
using Starcounter.Advanced;
using Starcounter.Internal;
using Starcounter.Server.PublicModel;
using Starcounter.Server.PublicModel.Commands;
using Starcounter.Server.Rest.Representations.JSON;
using System.Collections.Generic;
using System.Diagnostics;

namespace Starcounter.Administrator.API.Handlers {

    internal static partial class ExecutableCollectionHandler {
        /// <summary>
        /// Handles a POST to this resource.
        /// </summary>
        /// <param name="name">
        /// The name of the engine hosting the executable collection
        /// represented by this resource.</param>
        /// <param name="request">
        /// The REST request.</param>
        /// <returns>The response to be sent back to the client.</returns>
        static object OnPOST(string name, Request request) {
            Executable exe;
            ErrorDetail errDetail;
            var engine = RootHandler.Host.Engine;
            var runtime = RootHandler.Host.Runtime;

            var response = RESTUtility.JSON.CreateFromRequest<Executable>(request, out exe);
            if (response != null) return response;

            // Grab args, expect them to be formatted as a key value binary.
            // Support human-mode too!
            // On return, return the arguments we have deserialized, unpacked.
            // TODO:

            string[] userArgs = null;
            var rawArgs = exe.Arguments == null || exe.Arguments.Count == 0 ? null : exe.Arguments[0].dummy;
            if (!string.IsNullOrEmpty(rawArgs)) {
                userArgs = KeyValueBinary.ToArray(rawArgs);
            }

            var cmd = new ExecCommand(engine, exe.Path, null, userArgs);
            cmd.DatabaseName = name;
            cmd.EnableWaiting = true;

            var commandInfo = runtime.Execute(cmd);
            Trace.Assert(commandInfo.ProcessorToken == ExecCommand.DefaultProcessor.Token);
            if (cmd.EnableWaiting) {
                commandInfo = runtime.Wait(commandInfo);
            }

            if (commandInfo.HasError) {
                ErrorInfo single;
                ErrorMessage msg;

                single = null;
                if (ErrorInfoExtensions.TryGetSingleReasonErrorBasedOnServerConvention(commandInfo.Errors, out single)) {
                    var code = single.GetErrorCode();
                    switch (code) {
                        case Error.SCERRDATABASENOTFOUND:
                            msg = single.ToErrorMessage();
                            errDetail = RESTUtility.JSON.CreateError(msg.Code, msg.Body, msg.Helplink);
                            return RESTUtility.JSON.CreateResponse(errDetail.ToJson(), 404);
                        case Error.SCERREXECUTABLENOTFOUND:
                            msg = single.ToErrorMessage();
                            errDetail = RESTUtility.JSON.CreateError(msg.Code, msg.Body, msg.Helplink);
                            return RESTUtility.JSON.CreateResponse(errDetail.ToJson(), 422);
                        case Error.SCERREXECUTABLEALREADYRUNNING:
                        case Error.SCERRDATABASEENGINENOTRUNNING:
                            // Conflicts with the state expected; we map to HTTP 409.
                            msg = single.ToErrorMessage();
                            errDetail = RESTUtility.JSON.CreateError(msg.Code, msg.Body, msg.Helplink);
                            return RESTUtility.JSON.CreateResponse(errDetail.ToJson(), 409);
                        default:
                            break;
                    };
                }

                if (single == null) {
                    single = commandInfo.Errors[0];
                }
                msg = single.ToErrorMessage();

                errDetail = RESTUtility.JSON.CreateError(msg.Code, msg.ToString(), msg.Helplink);
                return RESTUtility.JSON.CreateResponse(errDetail.ToJson(), 500);
            }

            // The command succeeded, indicating the executable was accepted and
            // is now running inside the engine. We hand out an entity from the
            // state given to us by the processor, and return 201.

            var result = (DatabaseInfo) commandInfo.Result;
            var headers = new Dictionary<string, string>(2);
            var exeCreated = ExecutableHandler.JSON.CreateRepresentation(result, cmd.ExecutablePath, headers);
            exeCreated.StartedBy = exe.StartedBy;
            headers.Add("Location", exe.Uri);

            return RESTUtility.JSON.CreateResponse(exeCreated.ToJson(), 201, headers);
        }
    }
}