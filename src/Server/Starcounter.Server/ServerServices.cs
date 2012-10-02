
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter.ABCIPC;
using Starcounter.Apps.Bootstrap;
using Starcounter.Server.PublicModel;
using Starcounter.Internal;
using Starcounter.Server.Commands;
using System.IO;
using Starcounter.ABCIPC.Internal;

namespace Starcounter.Server {

    using Server = Starcounter.ABCIPC.Server;

    /// <summary>
    /// Encapsulates the services exposed by the server.
    /// </summary>
    public sealed class ServerServices {
        ServerEngine engine;
        IServerRuntime runtime;
        Server ipcServer;
        IResponseSerializer responseSerializer;

        public ServerServices(ServerEngine engine) {
            // Assume for now interactive mode. This code is still just
            // to get up and running. We'll eventually utilize pipes and
            // spawn another thread, etc.
            Starcounter.ABCIPC.Server ipcServer;
            if (!Console.IsInputRedirected) {
                ipcServer = Utils.PromptHelper.CreateServerAttachedToPrompt();
            } else {
                ipcServer = new Starcounter.ABCIPC.Server(Console.In.ReadLine, Console.Out.WriteLine);
            }
            this.engine = engine;
            this.runtime = null;
            this.ipcServer = ipcServer;
            this.responseSerializer = new NewtonSoftJsonSerializer(engine);
        }

        public void Setup() {

            ipcServer.Handle("GetServerInfo", delegate(Request request) {
                request.Respond(responseSerializer.SerializeReponse(runtime.GetServerInfo()));
            });

            ipcServer.Handle("GetDatabase", delegate(Request request) {
                string name;
                ScUri serverUri;
                string uri;

                name = request.GetParameter<string>();
                serverUri = ScUri.FromString(engine.Uri);
                uri = ScUri.MakeDatabaseUri(serverUri.MachineName, serverUri.ServerName, name).ToString();

                var info = runtime.GetDatabase(uri);
                if (info == null) {
                    request.Respond(false, "Database not found");
                    return;
                }

                request.Respond(responseSerializer.SerializeReponse(info));
            });

            ipcServer.Handle("GetDatabases", delegate(Request request) {
                var databases = runtime.GetDatabases();
                request.Respond(responseSerializer.SerializeReponse(databases));
            });

            ipcServer.Handle("GetCommandDescriptors", delegate(Request request) {
                // Redirect to IServerRuntime as soon as supported.
                // TODO:
                var supportedCommands = engine.Dispatcher.CommandDescriptors;
                request.Respond(responseSerializer.SerializeReponse(supportedCommands));
            });

            ipcServer.Handle("GetCommands", delegate(Request request) {
                var commands = runtime.GetCommands();
                request.Respond(responseSerializer.SerializeReponse(commands));
            });

            ipcServer.Handle("ExecApp", delegate(Request request) {
                string exePath;
                string workingDirectory;
                string args;
                string[] argsArray;

                var properties = request.GetParameter<Dictionary<string, string>>();
                if (properties == null || !properties.TryGetValue("AssemblyPath", out exePath)) {
                    request.Respond(false, "Missing required argument 'AssemblyPath'");
                    return;
                }
                exePath = exePath.Trim('"').Trim('\\', '/');

                properties.TryGetValue("WorkingDir", out workingDirectory);
                if (properties.TryGetValue("Args", out args)) {
                    argsArray = KeyValueBinary.ToArray(args);
                } else {
                    argsArray = new string[0];
                }

                var info = engine.AppsService.EnqueueExecAppCommandWithDispatcher(exePath, workingDirectory, argsArray);

                request.Respond(true, responseSerializer.SerializeReponse(info));
            });

            #region Command stubs not yet implemented

            ipcServer.Handle("CreateDatabase", delegate(Request request) {
                request.Respond(false, "NotImplemented");
            });

            // See 2.0 GetServerLogsByNumber
            ipcServer.Handle("GetLogsByNumber", delegate(Request request) {
                request.Respond(false, "NotImplemented");
            });

            // See 2.0 GetServerLogsByDate
            ipcServer.Handle("GetLogsByDate", delegate(Request request) {
                request.Respond(false, "NotImplemented");
            });

            // See 2.0 GetServerStatistics
            ipcServer.Handle("GetServerStatistics", delegate(Request request) {
                request.Respond(false, "NotImplemented");
            });

            // See 2.0 GetDatabaseExecutionInfo
            ipcServer.Handle("GetDatabaseExecutionInfo", delegate(Request request) {
                request.Respond(false, "NotImplemented");
            });

            #endregion
        }

        /// <summary>
        /// Starts the <see cref="ServerServices"/>, blocking the calling
        /// thread until a request to shut down comes in.
        /// </summary>
        /// <remarks>
        /// The <see cref="ServerServices"/> does not stop or dispose the
        /// <see cref="ServerEngine"/> upon shutdown; this is up to the
        /// caller.
        /// </remarks>
        public void Start() {
            // We don't assign the runtime until we are started,
            // trusting the engine was started prior to us.
            this.runtime = this.engine.CurrentPublicModel;
            ipcServer.Receive();
        }
    }
}