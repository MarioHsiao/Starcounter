
using Starcounter.Advanced;
using Starcounter.Bootstrap.Management.Representations.JSON;
using StarcounterInternal.Hosting;
using System;
using Starcounter.Hosting;

namespace Starcounter.Bootstrap.Management {
    /// <summary>
    /// Implements the code host functionality behind the code host "Executables"
    /// management resource.
    /// </summary>
    internal static class ExecutablesHandler {
        static ManagementService managementService;
        static unsafe void* schedulerHandle;

        /// <summary>
        /// Performs setup of the <see cref="ExecutablesHandler"/>.
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="handleScheduler">Handle to the scheduler to use when
        /// management services need to schedule work to be done.</param>
        public static unsafe void Setup(ManagementService manager, void* handleScheduler) {
            managementService = manager;

            var uri = CodeHostAPI.Uris.Executables;
            var port = manager.Port;
            schedulerHandle = handleScheduler;

            Handle.POST<Request>(port, uri, ExecutablesHandler.OnPOST);
        }

        static unsafe Response OnPOST(Request request) {

            if (managementService.Unavailable) {
                return 503;
            }

            Executable exe;
            var response = CodeHostHandler.JSON.CreateFromRequest<Executable>(request, out exe);
            if (response != null) return response;

			int i = 0;
            string[] userArgs = exe.Arguments.Count == 0 ? null : new string[exe.Arguments.Count];
			foreach (var arg in exe.Arguments) {
                userArgs[i++] = arg.StringValue;
            }
            
            var app = new ApplicationBase(
                exe.Name, 
                exe.ApplicationFilePath, exe.PrimaryFile, exe.WorkingDirectory, userArgs
            );
            app.HostedFilePath = exe.Path;
            app.TransactEntrypoint = exe.TransactEntrypoint;
            foreach (var resdir in exe.ResourceDirectories) {
                app.ResourceDirectories.Add(resdir.StringValue);
            }

            try {
                Loader.ExecuteApplication(schedulerHandle, app, !exe.RunEntrypointAsynchronous);
            } catch (Exception e) {
                if (!ExceptionManager.HandleUnhandledException(e)) throw;
            }

            return 204;
        }
    }
}