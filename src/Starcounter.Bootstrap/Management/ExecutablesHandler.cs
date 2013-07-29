
using Starcounter.Advanced;
using Starcounter.Bootstrap.Management.Representations.JSON;
using StarcounterInternal.Hosting;

namespace Starcounter.Bootstrap.Management {
    /// <summary>
    /// Implements the code host functionality behind the code host "Executables"
    /// management resource.
    /// </summary>
    internal static class ExecutablesHandler {
        static unsafe void* schedulerHandle;

        /// <summary>
        /// Performs setup of the <see cref="ExecutablesHandler"/>.
        /// </summary>
        /// <param name="handleScheduler">Handle to the scheduler to use when
        /// management services need to schedule work to be done.</param>
        public static unsafe void Setup(void* handleScheduler) {
            var uri = CodeHostAPI.Uris.Executables;
            var port = ManagementService.Port;
            schedulerHandle = handleScheduler;

            Handle.POST<Request>(port, uri, ExecutablesHandler.OnPOST);
        }

        static unsafe Response OnPOST(Request request) {
            if (ManagementService.Unavailable) {
                return 503;
            }

            Executable exe;
            var response = CodeHostHandler.JSON.CreateFromRequest<Executable>(request, out exe);
            if (response != null) return response;

            string[] userArgs = exe.Arguments.Count == 0 ? null : new string[exe.Arguments.Count];
            for (int i = 0; i < exe.Arguments.Count; i++) {
                userArgs[i] = exe.Arguments[i].dummy;
            }

            Loader.ExecApp(schedulerHandle, exe.Path, null, null, userArgs, !exe.RunEntrypointAsynchronous);
            return 204;
        }
    }
}