
using Starcounter.Administrator.API.Utilities;
using Starcounter.Bootstrap.Management.Representations.JSON;
using Starcounter.Server.PublicModel.Commands;
using Starcounter.Server.Rest.Representations.JSON;
using System.Diagnostics;

namespace Starcounter.Administrator.API.Handlers
{

    internal static partial class EngineHandler
    {
        /// <summary>
        /// Handles a PUT for this resource.
        /// </summary>
        /// <param name="name">
        /// The name of the database whose engines host the client issues a
        /// PUT on.</param>
        /// <param name="request">The request.</param>
        /// <returns>The response to be sent back to the client.</returns>
        static Response OnHostPUT(string name, Request request)
        {
            var serverEngine = RootHandler.Host.Engine;
            var runtime = RootHandler.Host.Runtime;
            ErrorDetail errDetail;

            var applicationDatabase = runtime.GetDatabaseByName(name);
            if (applicationDatabase == null)
            {
                errDetail = RESTUtility.JSON.CreateError(Error.SCERRDATABASENOTFOUND);
                return RESTUtility.JSON.CreateResponse(errDetail.ToJson(), 404);
            }

            var applicationEngine = applicationDatabase.Engine;
            if (applicationEngine != null && applicationEngine.HostProcessId != 0)
            {
                errDetail = RESTUtility.JSON.CreateError(Error.SCERRDATABASERUNNING);
                return RESTUtility.JSON.CreateResponse(errDetail.ToJson(), 409);
            }

            var host = new CodeHost();
            host.PopulateFromJson(request.Body);
            
            var register = new RegisterHostCommand(serverEngine, name, (int)host.ProcessId, host.HostedApplicationName);
            register.EnableWaiting = true;

            var commandInfo = runtime.Execute(register);
            if (register.EnableWaiting)
            {
                commandInfo = runtime.Wait(commandInfo);
            }
            if (commandInfo.HasError)
            {
                return ToErrorResponse(commandInfo);
            }

            // Just to be sure we don't forget to change this some, once
            // we implement asynchronous requests.
            Trace.Assert(commandInfo.IsCompleted);

            return 201;
        }
    }
}