
using Starcounter.Advanced;
using Starcounter.Bootstrap.Management.Representations.JSON;
using StarcounterInternal.Hosting;
using System;

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

			int i = 0;
            string[] userArgs = exe.Arguments.Count == 0 ? null : new string[exe.Arguments.Count];
			foreach (Executable.ArgumentsElementJson arg in exe.Arguments) { 
                userArgs[i++] = arg.dummy;
            }
            
            // Ask the loader to execute the given executable.
            // If this fails, the process can't really survive since
            // we have no way to clean up the loaded domain from the
            // failing code.
            //   Eventually, we will have a strategy to restart the
            // host without the now failing executable.
            try {
                Loader.ExecuteApplication(
                    schedulerHandle,
                    exe.Name,
                    exe.ApplicationFilePath,
                    exe.PrimaryFile,
                    exe.Path,
                    exe.WorkingDirectory,
                    userArgs,
                    !exe.RunEntrypointAsynchronous
                );
            } catch (Exception e) {
                if (!ExceptionManager.HandleUnhandledException(e)) throw;
            }

            return 204;
        }
    }
}