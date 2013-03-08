// ***********************************************************************
// <copyright file="ServerServices.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

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
using Starcounter.Server.PublicModel.Commands;

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
        
        /// <summary>
        /// Define the classification of services.
        /// </summary>
        [Flags] internal enum ServiceClass {
            Core = 1,
            Management = 2,
            All = (Core | Management)
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerServices" /> class.
        /// </summary>
        /// <param name="engine">The engine.</param>
        public ServerServices(ServerEngine engine) {
            // Assume for now interactive mode. This code is still just
            // to get up and running. We'll eventually utilize pipes and
            // spawn another thread, etc.
            Starcounter.ABCIPC.Server ipcServer;
            if (!Console.IsInputRedirected) {
                ipcServer = ClientServerFactory.CreateServerUsingConsole();
            } else {
                ipcServer = new Starcounter.ABCIPC.Server(Console.In.ReadLine, delegate(string reply, bool endsRequest) {
                    Console.WriteLine(reply);
                });
            }
            this.engine = engine;
            this.runtime = null;
            this.ipcServer = ipcServer;
            this.responseSerializer = new NewtonSoftJsonSerializer(engine);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerServices" /> class.
        /// </summary>
        /// <param name="engine">The engine.</param>
        /// <param name="ipcServer">The ipc server.</param>
        public ServerServices(ServerEngine engine, Starcounter.ABCIPC.Server ipcServer) {
            this.engine = engine;
            this.runtime = null;
            this.ipcServer = ipcServer;
            this.responseSerializer = new NewtonSoftJsonSerializer(engine);
        }

        /// <summary>
        /// Setups this instance.
        /// </summary>
        public void Setup() {
            Setup(ServiceClass.All);
        }

        internal void Setup(ServiceClass classes) {
            if ((classes & ServiceClass.Core) != 0) {
                SetupCoreServices();
            }

            if ((classes & ServiceClass.Management) != 0) {
                SetupManagementServices();
            }
        }

        void SetupManagementServices() {

            ipcServer.Handle("GetServerInfo", delegate(Request request) {
                request.Respond(responseSerializer.SerializeResponse(runtime.GetServerInfo()));
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

                request.Respond(responseSerializer.SerializeResponse(info));
            });

            ipcServer.Handle("GetDatabases", delegate(Request request) {
                var databases = runtime.GetDatabases();
                request.Respond(responseSerializer.SerializeResponse(databases));
            });

            ipcServer.Handle("GetCommandDescriptors", delegate(Request request) {
                // Redirect to IServerRuntime as soon as supported.
                // TODO:
                var supportedCommands = engine.Dispatcher.CommandDescriptors;
                request.Respond(responseSerializer.SerializeResponse(supportedCommands));
            });

            ipcServer.Handle("GetCommands", delegate(Request request) {
                var commands = runtime.GetCommands();
                request.Respond(responseSerializer.SerializeResponse(commands));
            });

            // Allows a client to get the latest copy of the command info for
            // a command represented by it's ID.
            ipcServer.Handle("GetCommand", delegate(Request request) {
                var commandId = request.GetParameter<string>();
                var command = runtime.GetCommand(CommandId.Parse(commandId));
                if (command == null) {
                    request.Respond(false);
                } else {
                    request.Respond(responseSerializer.SerializeResponse(command));
                }
            });

            // Allows a client to get the latest copy of the command info for
            // a command represented by it's ID, if the command has completed.
            // If it has not, an empty response (true) is returned.
            ipcServer.Handle("GetCompletedCommand", delegate(Request request) {
                var commandId = request.GetParameter<string>();
                var command = runtime.GetCommand(CommandId.Parse(commandId));
                if (command == null || !command.IsCompleted) {
                    request.Respond(false);
                } else {
                    request.Respond(responseSerializer.SerializeResponse(command));
                }
            });

            ipcServer.Handle("CreateDatabase", delegate(Request request) {
                IServerRuntime runtime;
                CreateDatabaseCommand command;
                string name;
                bool synchronous;

                // Get required properties - we can default everything but the
                // name. Without a name, we consider the request a failure.

                var properties = request.GetParameter<Dictionary<string, string>>();
                if (properties == null || !properties.TryGetValue("Name", out name)) {
                    request.Respond(false, "Missing required argument 'Name'");
                    return;
                }
                synchronous = properties.ContainsKey("@@Synchronous");
                
                command = new CreateDatabaseCommand(this.engine, name);
                command.EnableWaiting = synchronous;
                runtime = engine.CurrentPublicModel;

                var info = runtime.Execute(command);
                if (synchronous) {
                    info = runtime.Wait(info);
                }

                // We respond always with a carry, that is the serialized version
                // of the command information. If the command indicates it has an
                // error, we responde FALSE to indicate the command was a failure.
                request.Respond(!info.HasError, responseSerializer.SerializeResponse(info));
            });

            ipcServer.Handle("StartDatabase", delegate(Request request) {
                IServerRuntime runtime;
                StartDatabaseCommand command;
                string name;
                bool synchronous;

                // Get required properties - we can default everything but the
                // name. Without a name, we consider the request a failure.

                var properties = request.GetParameter<Dictionary<string, string>>();
                if (properties == null || !properties.TryGetValue("Name", out name)) {
                    request.Respond(false, "Missing required argument 'Name'");
                    return;
                }
                synchronous = properties.ContainsKey("@@Synchronous");

                command = new StartDatabaseCommand(this.engine, name);
                command.EnableWaiting = synchronous;
                runtime = engine.CurrentPublicModel;
                
                var info = runtime.Execute(command);
                if (synchronous) {
                    info = runtime.Wait(info);
                }

                // We respond always with a carry, that is the serialized version
                // of the command information. If the command indicates it has an
                // error, we responde FALSE to indicate the command was a failure.
                request.Respond(!info.HasError, responseSerializer.SerializeResponse(info));
            });

            ipcServer.Handle("StopDatabase", delegate(Request request) {
                IServerRuntime runtime;
                StopDatabaseCommand command;
                string name;
                bool synchronous;

                // Get required properties - we can default everything but the
                // name. Without a name, we consider the request a failure.
                // We also support an additional parameter, indicating if we
                // should stop the database process too, and not just the worker
                // process (which is the default).

                var properties = request.GetParameter<Dictionary<string, string>>();
                if (properties == null || !properties.TryGetValue("Name", out name)) {
                    request.Respond(false, "Missing required argument 'Name'");
                    return;
                }
                synchronous = properties.ContainsKey("@@Synchronous");

                command = new StopDatabaseCommand(this.engine, name);
                command.StopDatabaseProcess = properties.ContainsKey("StopDb");
                command.EnableWaiting = synchronous;
                runtime = engine.CurrentPublicModel;

                var info = runtime.Execute(command);
                if (synchronous) {
                    info = runtime.Wait(info);
                }

                // We respond always with a carry, that is the serialized version
                // of the command information. If the command indicates it has an
                // error, we responde FALSE to indicate the command was a failure.
                request.Respond(!info.HasError, responseSerializer.SerializeResponse(info));
            });

            #region Command stubs not yet implemented

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

        void SetupCoreServices() {
            // Ping is considered a core service
            ipcServer.Handle("Ping", delegate(Request request) {
                request.Respond(true);
            });

            // Execution of apps is considered a core service too.
            // Without it, apps can't execute from the shell.
            ipcServer.Handle("ExecApp", delegate(Request request) {
                IServerRuntime runtime;
                ExecCommand command;
                string exePath;
                string workingDirectory;
                string args;
                string[] argsArray;
                bool synchronous;

                var properties = request.GetParameter<Dictionary<string, string>>();
                if (properties == null || !properties.TryGetValue("AssemblyPath", out exePath)) {
                    request.Respond(false, "Missing required argument 'AssemblyPath'");
                    return;
                }
                exePath = exePath.Trim('"').Trim('\\', '/');
                exePath = Path.GetFullPath(exePath);

                properties.TryGetValue("WorkingDir", out workingDirectory);
                if (properties.TryGetValue("Args", out args)) {
                    argsArray = KeyValueBinary.ToArray(args);
                } else {
                    argsArray = new string[0];
                }
                synchronous = properties.ContainsKey("@@Synchronous");
                runtime = engine.CurrentPublicModel;

                command = new ExecCommand(this.engine, exePath, workingDirectory, argsArray);
                command.EnableWaiting = synchronous;
                if (properties.ContainsKey("PrepareOnly")) {
                    command.PrepareOnly = true;
                }

                var info = runtime.Execute(command);
                if (synchronous) {
                    info = runtime.Wait(info);
                }

                // We respond always with a carry, that is the serialized version
                // of the command information. If the command indicates it has an
                // error, we responde FALSE to indicate the command was a failure.
                request.Respond(!info.HasError, responseSerializer.SerializeResponse(info));
            });
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
        internal void Start() {
            // We don't assign the runtime until we are started,
            // trusting the engine was started prior to us.
            this.runtime = this.engine.CurrentPublicModel;
            ipcServer.Receive();
        }
    }
}