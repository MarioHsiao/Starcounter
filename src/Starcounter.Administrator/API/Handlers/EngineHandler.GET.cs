
using Starcounter.Administrator.API.Utilities;
using Starcounter.Advanced;

namespace Starcounter.Administrator.API.Handlers {

    internal static partial class EngineHandler {
        /// <summary>
        /// Handles a GET for this resource.
        /// </summary>
        /// <param name="name">
        /// The name of the database whose engine the client request a
        /// representation of.</param>
        /// <param name="request">
        /// The REST request.</param>
        /// <returns>The response to be sent back to the client.</returns>
        static object OnGET(string name, Request request) {
            var server = RootHandler.Host.Runtime;
            var applicationDatabase = server.GetDatabaseByName(name);
            if (applicationDatabase == null) {
                var errDetail = RESTUtility.JSON.CreateError(Error.SCERRDATABASENOTFOUND);
                return RESTUtility.JSON.CreateResponse(errDetail.ToJson(), 404);
            }

            // Concern here: should we return 404 both for the database
            // not being found and the engine? They are semantically different,
            // so probably not. Right now we differ them by returning a content
            // describing the case that the database is not found above, and
            // returning no content if the host process was found not running.
            // Consider and possible resolve this.
            // TODO:

            if (applicationDatabase.HostProcessId == 0)
                return 404;

            return EngineHandler.JSON.CreateRepresentation(applicationDatabase);
        }
    }
}