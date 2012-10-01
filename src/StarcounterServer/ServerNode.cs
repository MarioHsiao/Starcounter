﻿
using Starcounter;
using Starcounter.ABCIPC;
using Starcounter.ABCIPC.Internal;
using Starcounter.Configuration;
using Starcounter.Server.PublicModel;
using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.IO;
using Starcounter.Server.Commands;
using Starcounter.Internal;

namespace Starcounter.Server {

    /// <summary>
    /// Representing the running server, hosted in a server program.
    /// </summary>
    internal sealed class ServerNode {
        private readonly CommandDispatcher dispatcher;

        /// <summary>
        /// Gets the server configuration.
        /// </summary>
        internal readonly ServerConfiguration Configuration;

        /// <summary>
        /// Gets the database default values to be used when creating databases
        /// and values are not explicitly given.
        /// </summary>
        internal readonly DatabaseDefaults DatabaseDefaultValues;

        /// <summary>
        /// Gets the <see cref="DatabaseEngine"/> used by the current server.
        /// </summary>
        internal readonly DatabaseEngine DatabaseEngine;

        /// <summary>
        /// Gets the simple name of the server.
        /// </summary>
        internal readonly string Name;

        /// <summary>
        /// Gets the URI of this server.
        /// </summary>
        internal readonly string Uri;

        /// <summary>
        /// Gets the Starcounter installation directory.
        /// </summary>
        internal readonly string InstallationDirectory;

        /// <summary>
        /// Gets the full path of the temporary directory decided and resolved
        /// when the server started.
        /// </summary>
        internal string TempDirectory {
            get;
            private set;
        }

        /// <summary>
        /// Gets the full path of the database directory decided and resolved
        /// when the server started.
        /// </summary>
        internal string DatabaseDirectory {
            get;
            private set;
        }

        /// <summary>
        /// Gets the dictionary with databases maintained by this server,
        /// keyed by their name.
        /// </summary>
        internal Dictionary<string, Database> Databases { get; private set; }

        /// <summary>
        /// Gets the current public model snapshot of the server and
        /// it's databases. The public model is updated in a thread-safe
        /// maner whenever there is a change in any of the internal
        /// domain state objects (modified by command processors).
        /// </summary>
        internal PublicModelProvider CurrentPublicModel { get; private set; }

        IResponseSerializer ResponseSerializer;

        /// <summary>
        /// Initializes a <see cref="ServerNode"/>.
        /// </summary>
        /// <param name="configuration"></param>
        internal ServerNode(ServerConfiguration configuration) {
            this.InstallationDirectory = Path.GetDirectoryName(typeof(ServerProgram).Assembly.Location);
            this.Configuration = configuration;
            this.DatabaseDefaultValues = new DatabaseDefaults();
            this.Name = configuration.Name;
            this.Uri = ScUri.MakeServerUri(ScUri.GetMachineName(), this.Name);
            this.Databases = new Dictionary<string, Database>();
            this.DatabaseEngine = new DatabaseEngine(this);
            this.dispatcher = new CommandDispatcher(this);
        }

        internal void Setup() {
            string serverRepositoryDirectory;
            string tempDirectory;
            string databaseDirectory;

            // Validate the database directory exist. We refuse to start if
            // we can not properly resolve it to an existing directory.

            serverRepositoryDirectory = Path.GetDirectoryName(Path.GetFullPath(this.Configuration.ConfigurationFilePath));
            databaseDirectory = Environment.ExpandEnvironmentVariables(this.Configuration.DatabaseDirectory);
            if (!Path.IsPathRooted(databaseDirectory)) {
                databaseDirectory = Path.Combine(serverRepositoryDirectory, databaseDirectory);
            }
            databaseDirectory = Path.GetFullPath(databaseDirectory);
            if (!Directory.Exists(databaseDirectory)) {
                throw ErrorCode.ToException(Error.SCERRUNSPECIFIED, string.Format("Database directory doesn't exist. Path: {0}", databaseDirectory));
            }

            // Setup the temporary directory. This directory we create
            // if it's not found, since it's not the end of the world if
            // it's wrongly given and temporary files end up somewhere
            // not intended.

            tempDirectory = Environment.ExpandEnvironmentVariables(this.Configuration.TempDirectory);
            if (!Path.IsPathRooted(tempDirectory)) {
                tempDirectory = Path.Combine(serverRepositoryDirectory, tempDirectory);
            }
            tempDirectory = Path.GetFullPath(tempDirectory);
            if (!Directory.Exists(tempDirectory)) {
                Directory.CreateDirectory(tempDirectory);
            }

            this.DatabaseDirectory = databaseDirectory;
            this.TempDirectory = tempDirectory;

            this.dispatcher.DiscoverAssembly(GetType().Assembly);
            this.DatabaseEngine.Setup();
            this.DatabaseDefaultValues.Update(this.Configuration);
            SetupDatabases();
            this.CurrentPublicModel = new PublicModelProvider(this);
            this.ResponseSerializer = new NewtonSoftJsonSerializer(this);
        }

        internal void Start() {
            Starcounter.ABCIPC.Server ipcServer;

            // Assume for now interactive mode. This code is still just
            // to get up and running. We'll eventually utilize pipes and
            // spawn another thread, etc.
            
            if (!Console.IsInputRedirected) {
                ipcServer = Utils.PromptHelper.CreateServerAttachedToPrompt();
            } else {
                ipcServer = new Starcounter.ABCIPC.Server(Console.In.ReadLine, Console.Out.WriteLine);
            }

            ipcServer.Handle("GetServerInfo", delegate(Request request) {
                request.Respond(ResponseSerializer.SerializeReponse(CurrentPublicModel.ServerInfo));
            });

            ipcServer.Handle("GetDatabase", delegate(Request request) {
                string name;
                ScUri serverUri;
                string uri;
                
                name = request.GetParameter<string>();
                serverUri = ScUri.FromString(this.Uri);
                uri = ScUri.MakeDatabaseUri(serverUri.MachineName, serverUri.ServerName, name).ToString();

                var info = CurrentPublicModel.GetDatabase(uri);
                if (info == null) {
                    request.Respond(false, "Database not found");
                    return;
                }

                request.Respond(ResponseSerializer.SerializeReponse(info));
            });

            ipcServer.Handle("GetDatabases", delegate(Request request) {
                var databases = CurrentPublicModel.GetDatabases();
                request.Respond(ResponseSerializer.SerializeReponse(databases));
            });

            ipcServer.Handle("GetCommandDescriptors", delegate(Request request) {
                var supportedCommands = dispatcher.CommandDescriptors;
                request.Respond(ResponseSerializer.SerializeReponse(supportedCommands));
            });

            ipcServer.Handle("GetCommands", delegate(Request request) {
                var commands = this.dispatcher.GetRecentCommands();
                request.Respond(ResponseSerializer.SerializeReponse(commands));
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

                var info = dispatcher.Enqueue(new ExecAppCommand(exePath, workingDirectory, argsArray));

                request.Respond(true, ResponseSerializer.SerializeReponse(info));
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

            ipcServer.Receive();
        }

        internal void Stop() {
        }

        /// <summary>
        /// Creates a snapshot of this <see cref="ServerNode"/> in the
        /// form of a public model <see cref="ServerInfo"/>.
        /// </summary>
        /// <returns>A <see cref="ServerInfo"/> representing the current state
        /// of this server.</returns>
        internal ServerInfo ToPublicModel() {
            var info = new ServerInfo() {
                Configuration = this.Configuration.Clone(),
                DefaultMaxImageSize = this.DatabaseDefaultValues.MaxImageSize,
                DefaultTransactionLogSize = this.DatabaseDefaultValues.TransactionLogSize,
                IsMonitoringSupported = false,
                ServerConfigurationPath = this.Configuration.ConfigurationFilePath,
                Uri = this.Uri,
                UserName = WindowsIdentity.GetCurrent().Name,
            };
            return info;
        }

        void SetupDatabases() {
            foreach (var databaseDirectory in Directory.GetDirectories(this.DatabaseDirectory)) {
                var databaseName = Path.GetFileName(databaseDirectory).ToLowerInvariant();
                var databaseConfigPath = Path.Combine(databaseDirectory, databaseName + DatabaseConfiguration.FileExtension);

                var config = DatabaseConfiguration.Load(databaseConfigPath);
                var database = new Database(this, config);
                this.Databases.Add(databaseName, database);
            }
        }
    }
}
