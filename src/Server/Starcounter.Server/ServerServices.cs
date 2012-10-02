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

namespace Starcounter.Server {

    using Server = Starcounter.ABCIPC.Server;

    /// <summary>
    /// Encapsulates the services exposed by the server.
    /// </summary>
    internal sealed class ServerServices {
        ServerEngine engine;
        Server ipcServer;
        IResponseSerializer responseSerializer;

        internal ServerServices(ServerEngine engine, Server ipcServer, IResponseSerializer responseSerializer) {
            this.engine = engine;
            this.ipcServer = ipcServer;
            this.responseSerializer = responseSerializer;
        }

        internal void Setup() {

            ipcServer.Handle("GetServerInfo", delegate(Request request) {
                request.Respond(responseSerializer.SerializeReponse(engine.CurrentPublicModel.ServerInfo));
            });

            ipcServer.Handle("GetDatabase", delegate(Request request) {
                string name;
                ScUri serverUri;
                string uri;

                name = request.GetParameter<string>();
                serverUri = ScUri.FromString(engine.Uri);
                uri = ScUri.MakeDatabaseUri(serverUri.MachineName, serverUri.ServerName, name).ToString();

                var info = engine.CurrentPublicModel.GetDatabase(uri);
                if (info == null) {
                    request.Respond(false, "Database not found");
                    return;
                }

                request.Respond(responseSerializer.SerializeReponse(info));
            });

            ipcServer.Handle("GetDatabases", delegate(Request request) {
                var databases = engine.CurrentPublicModel.GetDatabases();
                request.Respond(responseSerializer.SerializeReponse(databases));
            });

            ipcServer.Handle("GetCommandDescriptors", delegate(Request request) {
                var supportedCommands = engine.Dispatcher.CommandDescriptors;
                request.Respond(responseSerializer.SerializeReponse(supportedCommands));
            });

            ipcServer.Handle("GetCommands", delegate(Request request) {
                var commands = engine.Dispatcher.GetRecentCommands();
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

                var info = EnqueueExecAppCommandWithDispatcher(exePath, workingDirectory, argsArray);

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

        internal void Start() {
            AppProcess.WaitForStartRequests(OnAppExeStartRequest);
            ipcServer.Receive();
        }

        /// <summary>
        /// Handles requests coming from booting App executables.
        /// </summary>
        /// <param name="properties"></param>
        /// <returns></returns>
        bool OnAppExeStartRequest(Dictionary<string, string> properties) {
            string assemblyPath;
            string workingDirectory;
            string serializedArgs;
            string[] arguments;

            // Validate all required properties are given in the given
            // property set and only enque a command if they are.
            //
            // If any of them are missing, we simply log a warning and keep
            // listening for new requests.

            if (!properties.TryGetValue("AssemblyPath", out assemblyPath)) {
                //LogWarning("Ignoring starting request without given assembly path.");
                return true;
            }

            if (!properties.TryGetValue("WorkingDirectory", out workingDirectory)) {
                workingDirectory = Path.GetDirectoryName(assemblyPath);
            }

            if (properties.TryGetValue("Args", out serializedArgs)) {
                arguments = KeyValueBinary.ToArray(serializedArgs);
            } else {
                arguments = new string[0];
            }

            EnqueueExecAppCommandWithDispatcher(assemblyPath, workingDirectory, arguments);
            return true;
        }

        CommandInfo EnqueueExecAppCommandWithDispatcher(string assemblyPath, string workingDirectory, string[] args) {
            ExecAppCommand command = new ExecAppCommand(assemblyPath, workingDirectory, args);
            return engine.Dispatcher.Enqueue(command);
        }
    }
}