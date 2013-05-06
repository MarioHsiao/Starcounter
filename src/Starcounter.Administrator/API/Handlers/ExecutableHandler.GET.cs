
using Starcounter.Administrator.API.Utilities;
using Starcounter.Advanced;
using Starcounter.Server.PublicModel;
using System.Collections.Generic;

namespace Starcounter.Administrator.API.Handlers {

    internal static partial class ExecutableHandler {
        /// <summary>
        /// Handles a GET for this resource.
        /// </summary>
        /// <param name="engineName">
        /// The name of the database whose engine the executable is
        /// assumed to run within.</param>
        /// <param name="key">The unique key of the executable, retreived in
        /// a previous request from server (e.g. when POSTing to, or retreiving
        /// executables from, the collection.</param>
        /// <param name="request">The REST request.</param>
        /// <returns>The response to be sent back to the client.</returns>
        static object OnGET(string engineName, string key, Request request) {
            var server = RootHandler.Host.Runtime;
            var applicationDatabase = server.GetDatabaseByName(engineName);
            if (applicationDatabase == null) {
                var errDetail = RESTUtility.JSON.CreateError(Error.SCERRDATABASENOTFOUND);
                return RESTUtility.JSON.CreateResponse(errDetail.ToJson(), 404);
            }

            var engineState = applicationDatabase.Engine;
            if (engineState == null || engineState.HostProcessId == 0) {
                var errDetail = RESTUtility.JSON.CreateError(Error.SCERRDATABASEENGINENOTRUNNING);
                return RESTUtility.JSON.CreateResponse(errDetail.ToJson(), 404);
            }

            AppInfo exeState = null;
            foreach (var candidate in engineState.HostedApps) {
                if (candidate.Key == key) {
                    exeState = candidate;
                    break;
                }
            }

            if (exeState == null) {
                var errDetail = RESTUtility.JSON.CreateError(Error.SCERREXECUTABLENOTRUNNING);
                return RESTUtility.JSON.CreateResponse(errDetail.ToJson(), 404);
            }

            var headers = new Dictionary<string, string>(1);
            var exe = ExecutableHandler.JSON.CreateRepresentation(applicationDatabase, exeState, headers);
            
            return RESTUtility.JSON.CreateResponse(exe.ToJson(), 200, headers);
        }
    }
}