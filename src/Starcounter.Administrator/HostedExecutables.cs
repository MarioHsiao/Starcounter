﻿
using Starcounter.Advanced;
using Starcounter.Server;
using Starcounter.Server.PublicModel;
using Starcounter.Server.PublicModel.Commands;
using StarcounterApps3;
using System;
using System.Diagnostics;

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

            // Ask the server runtime to ask the command.
            // Assert it's executed by the default processor, since we
            // depend on that to produce accurate responses.
            // This is somewhat theoretical though, since we are in
            // charge of both. The assert is more there if someone
            // would introduce some changes that affects this later.

            var commandInfo = runtime.Execute(cmd);
            Trace.Assert(commandInfo.CommandType == ExecCommand.DefaultProcessor.ID);
            commandInfo = runtime.Wait(commandInfo);

            // Done. Check the outcome.

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

            // If it was successfull, lets look at what was actually accomplished to
            // return an appropriate response.
            // If we find a single task that indicates checking if the executable was
            // up to date, we know nothing else was done and we consider it a 200.
            //   In all other cases, we use 201, indicating it was in fact "created",
            // i.e. started as requested.

            if (commandInfo.Progress.Length == 1) {
                Trace.Assert(
                    commandInfo.Progress[0].TaskIdentity == 
                    ExecCommand.DefaultProcessor.Tasks.CheckRunningExeUpToDate
                );

                // Up to date.
                // Return a response that includes a reference to the running
                // executable inside the host.
                
                return 200;
            }

            // It's 201 created. Let's check if we created the database as well,
            // and if so, report about both in the response.
            // TODO:

            return 201;
        }
    }
}
