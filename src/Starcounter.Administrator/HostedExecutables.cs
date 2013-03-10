
using Starcounter.Advanced;
using Starcounter.Server;
using Starcounter.Server.PublicModel;
using Starcounter.Server.PublicModel.Commands;
using StarcounterApps3;
using System;
using System.Diagnostics;
using Starcounter.Internal.Web;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Specialized;

namespace Starcounter.Administrator {
    /// <summary>
    /// Abstracts the admin server resource /databases/{name}/executables
    /// and implements it's REST interface.
    /// </summary>
    internal static class HostedExecutables {
        static ServerEngine engine;
        static IServerRuntime runtime;
        static string serverHost;
        static int serverPort;

        const string relativeResourceUri = "/databases/{?}/executables";

        internal static void Setup(
            string serverHost,
            int serverPort,
            ServerEngine engine, 
            IServerRuntime runtime) {
            HostedExecutables.engine = engine;
            HostedExecutables.runtime = runtime;
            HostedExecutables.serverHost = serverHost;
            HostedExecutables.serverPort = serverPort;
            StarcounterBase.POST<HttpRequest, string>(relativeResourceUri, HandlePOST);
        }

        static object HandlePOST(HttpRequest request, string name) {

            var execRequest = new ExecRequest();
            execRequest.PopulateFromJson(request.GetContentStringUtf8_Slow());

            var cmd = new ExecCommand(engine, execRequest.ExecutablePath, null, null);
            cmd.DatabaseName = name;
            cmd.EnableWaiting = true;
            cmd.LogSteps = execRequest.LogSteps;
            cmd.NoDb = execRequest.NoDb;
            cmd.CanAutoCreateDb = execRequest.CanAutoCreateDb;

            // Ask the server runtime to ask the command.
            // Assert it's executed by the default processor, since we
            // depend on that to produce accurate responses.
            // This is somewhat theoretical though, since we are in
            // charge of both. The assert is more there if someone
            // would introduce some changes that affects this later.

            var commandInfo = runtime.Execute(cmd);
            Trace.Assert(commandInfo.ProcessorToken == ExecCommand.DefaultProcessor.Token);
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

                    if (single.GetErrorCode() == Starcounter.Error.SCERRDATABASENOTFOUND) {
                        // The database was not found, and the request indicated it was not
                        // allowed to automatically create it.
                        return 404;
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
            //
            // Whichever of these we return, we should include an entity (JSON-based)
            // that describes the now-running executable, the host proccess it runs
            // in (PID), the machine, the server, the database name, etc. And the URI
            // of the executable itself - both in the body and in the Location field.
            // Also, if the database was created, we should describe that new resource
            // too (name/uri, size, files, whatever).
            //
            // We are awaiting the proper design of upcoming HttpResponse though, as
            // discussed in this forum thread:
            // http://www.starcounter.com/forum/showthread.php?2482-Returning-HTTP-responses
            //
            // TODO:

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

        static HttpResponse CreateResponseFor201(
            CommandInfo command, ExecRequest execRequest, string databaseName) {

            // The Location response header field SHOULD be set to an ABSOLUTE
            // Uri, referencing the created resource as described by:
            // http://tools.ietf.org/html/rfc2616#section-14.30
            //
            // That is, in this case: http://host:port/path/to/the/executable/
            // like http://localhost:8181/databases/default/executables/foo.exe.
            //
            // From 10.2.2 201 Created:
            // "The newly created resource can be referenced by the URI(s)
            // returned in the entity of the response, with the most specific URI
            // for the resource given by a Location header field. The response
            // SHOULD include an entity containing a list of resource
            // characteristics and location(s) from which the user or user agent can
            // choose the one most appropriate".

            var runningExeRelativeUri = string.Format(HostedExecutables.relativeResourceUri, databaseName);
            runningExeRelativeUri += "/" + Path.GetFileName(execRequest.ExecutablePath);
            
            var location = string.Format("http://{0}:{1}{2}", serverHost, serverPort, runningExeRelativeUri);
            var createdDatabase = command.GetProgressOf(
                ExecCommand.DefaultProcessor.Tasks.CreateDatabase) != null;

            var database = runtime.GetDatabase(command.DatabaseUri);
            Trace.Assert(database != null);

            var headers = new NameValueCollection(StringComparer.InvariantCultureIgnoreCase);
            headers.Add("Location", location);

            var x = new {
                DatabaseUri = command.DatabaseUri,
                DatabaseHostPID = database.HostProcessId,
                DatabaseCreated = createdDatabase
            };
            var content = JsonConvert.SerializeObject(x);

            return new HttpResponse { Uncompressed = 
                HttpResponseBuilder.Slow.FromStatusHeadersAndStringContent(201, headers, content) 
            };
        }
    }
}
