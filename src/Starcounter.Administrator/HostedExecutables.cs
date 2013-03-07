
using Starcounter.Advanced;
using Starcounter.Server;
using Starcounter.Server.PublicModel;
using Starcounter.Server.PublicModel.Commands;
using StarcounterApps3;

namespace Starcounter.Administrator {
    /// <summary>
    /// Abstracts the admin server resource /databases/{name}/executables
    /// and implements it's REST interface.
    /// </summary>
    internal static class HostedExecutables {
        static ServerEngine engine;
        static IServerRuntime runtime;

        const string relativeResourceUri = "/databases/{?}/executables";

        internal static void Setup(ServerEngine engine, IServerRuntime runtime) {
            HostedExecutables.engine = engine;
            HostedExecutables.runtime = runtime;
            StarcounterBase.POST<HttpRequest, string>(relativeResourceUri, HandlePOST);
        }

        static object HandlePOST(HttpRequest request, string name) {
            
            var execRequest = ExecRequest.FromJson(request);

            var cmd = new ExecCommand(engine, execRequest.ExecutablePath, null, null);
            cmd.DatabaseName = name;
            cmd.EnableWaiting = true;
            cmd.LogSteps = execRequest.LogSteps;
            cmd.NoDb = execRequest.NoDb;

            var commandInfo = runtime.Execute(cmd);
            commandInfo = runtime.Wait(commandInfo);

            if (commandInfo.HasError) {
                ErrorInfo single;

                single = null;
                if (ErrorInfoExtensions.TryGetSingleReasonErrorBasedOnServerConvention(commandInfo.Errors, out single)) {
                    if (single.GetErrorCode() == Starcounter.Error.SCERREXECUTABLENOTFOUND) {
                        // We want to give back the part we couldn't process and also
                        // make sure the reason is there and that 
                        // TODO:
                        return 422;
                    }
                }

                if (single == null)
                    single = commandInfo.Errors[0];

                throw single.ToErrorMessage().ToException();
            }

            // For illustration purposes, showing that we can return
            // Message objects as the content, we simply return the
            // one we have at hand. This should change, returning an
            // entity that contains context-relative information about
            // the resouce (i.e. the now running executable).
            // TODO:

            return execRequest;
        }
    }
}
