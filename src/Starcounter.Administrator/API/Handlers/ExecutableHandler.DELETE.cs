
using Starcounter.Advanced;
using Starcounter.Server.PublicModel.Commands;

namespace Starcounter.Administrator.API.Handlers {

    internal static partial class ExecutableHandler {
        /// <summary>
        /// Handles a DELETE of this resource.
        /// </summary>
        /// <param name="database">
        /// The name of the database hosting the executable collection
        /// represented by this resource.</param>
        /// <param name="executable">The identity of the executable to
        /// delete from the collection of executables (i.e. semantically,
        /// the executable to unload from the code host).
        /// </param>
        /// <param name="request">
        /// The REST request.</param>
        /// <returns>The response to be sent back to the client.</returns>
        static Response OnDELETE(string database, string executable, Request request) {
            var serverEngine = RootHandler.Host.Engine;
            var runtime = RootHandler.Host.Runtime;
            
            var stop = new StopExecutableCommand(serverEngine, database, executable);
            stop.EnableWaiting = true;
            
            var commandInfo = runtime.Execute(stop);
            if (stop.EnableWaiting) {
                commandInfo = runtime.Wait(commandInfo);
            }

            return commandInfo.HasError ? 500 : 200;
        }
    }
}
