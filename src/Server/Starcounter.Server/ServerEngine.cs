// ***********************************************************************
// <copyright file="ServerEngine.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Advanced.Configuration;
using Starcounter.Internal;
using Starcounter.Logging;
using Starcounter.Server.Commands;
using Starcounter.Server.Commands.InternalCommands;
using Starcounter.Server.PublicModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Principal;

namespace Starcounter.Server {

    /// <summary>
    /// Representing the running server, hosted in a server program.
    /// </summary>
    public sealed class ServerEngine {
        /// <summary>
        /// Gets the simple name of the server.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// Gets the URI of this server.
        /// </summary>
        public readonly string Uri;

        /// <summary>
        /// Gets a reference to the log source dedicated to the server
        /// host.
        /// </summary>
        /// <remarks>This log source is not available until <see cref="Setup"/>
        /// has been successfully invoked.</remarks>
        public LogSource HostLog { get; private set; }

        /// <summary>
        /// Gets the installed <see cref="CommandDispatcher"/> the current
        /// engine will utilize when executing commands.
        /// </summary>
        internal readonly CommandDispatcher Dispatcher;

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
        /// Gets the <see cref="GatewayService"/> used by the current server.
        /// </summary>
        internal readonly GatewayService GatewayService;

        /// <summary>
        /// Gets the default name of the pipe the server use for it's
        /// core services.
        /// </summary>
        internal string DefaultServicePipeName {
            get {
                return ScUriExtensions.MakeLocalServerPipeString(this.Name);
            }
        }

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

        /// <summary>
        /// Gets the <see cref="ExecutableService"/> used when the current
        /// server engine needs operate on executables.
        /// </summary>
        internal ExecutableService ExecutableService { get; private set; }

        /// <summary>
        /// Gets the <see cref="WeaverService"/> used when the current server
        /// engine will need to weave user code.
        /// </summary>
        internal WeaverService WeaverService { get; private set; }

        /// <summary>
        /// Gets the <see cref="DatabaseStorageService"/> used when the current
        /// server engine needs to operate on database storage files.
        /// </summary>
        internal DatabaseStorageService StorageService { get; private set; }

        /// <summary>
        /// Gets the <see cref="DatabaseHostingService"/> used by this server
        /// to initiate local communication with database host processes.
        /// </summary>
        internal DatabaseHostingService DatabaseHostService { get; private set; }

        /// <summary>
        /// Initializes a <see cref="ServerEngine"/>.
        /// </summary>
        /// <param name="serverConfigurationPath">Path to the server configuration
        /// file in the root of the server repository the engine will run.</param>
        /// <param name="installDir">Path to the installation directory.</param>
        public ServerEngine(string serverConfigurationPath, String installDir = null) {
            // TODO: Talk to Per!
            if (installDir == null)
                this.InstallationDirectory = StarcounterEnvironment.InstallationDirectory;
            else
                this.InstallationDirectory = installDir;

            this.Configuration = ServerConfiguration.Load(Path.GetFullPath(serverConfigurationPath));
            this.DatabaseDefaultValues = new DatabaseDefaults();
            this.Name = this.Configuration.Name;
            this.Uri = ScUri.MakeServerUri(ScUri.GetMachineName(), this.Name);
            this.Databases = new Dictionary<string, Database>(StringComparer.InvariantCultureIgnoreCase);
            this.DatabaseEngine = new DatabaseEngine(this);
            this.Dispatcher = new CommandDispatcher(this);
            this.WeaverService = new Server.WeaverService(this);
            this.StorageService = new DatabaseStorageService(this);
            this.DatabaseHostService = new DatabaseHostingService(this);
            this.ExecutableService = new ExecutableService(this);
            this.GatewayService = new GatewayService(this);
        }

        /// <summary>
        /// Executes setup of the current engine.
        /// </summary>
        public void Setup() {
            ServerHost.Configure(this.Configuration);

            // Validate the database directory exist. We refuse to start if
            // we can not properly resolve it to an existing directory.

            var databaseDirectory = this.Configuration.GetResolvedDatabaseDirectory();
            if (!Directory.Exists(databaseDirectory)) {
                throw ErrorCode.ToException(Error.SCERRUNSPECIFIED, string.Format("Database directory doesn't exist. Path: {0}", databaseDirectory));
            }

            // Setup the temporary directory. This directory we create
            // if it's not found, since it's not the end of the world if
            // it's wrongly given and temporary files end up somewhere
            // not intended.

            var tempDirectory = this.Configuration.GetResolvedTempDirectory();
            if (!Directory.Exists(tempDirectory)) {
                Directory.CreateDirectory(tempDirectory);
            }

            this.DatabaseDirectory = databaseDirectory;
            this.TempDirectory = tempDirectory;

            this.Dispatcher.Setup();
            this.DatabaseEngine.Setup();
            this.DatabaseDefaultValues.Update(this.Configuration);
            this.ExecutableService.Setup();
            this.WeaverService.Setup();
            this.StorageService.Setup();
            this.DatabaseHostService.Setup();
            this.GatewayService.Setup();

            SetupDatabases();
            this.CurrentPublicModel = new PublicModelProvider(this);
            this.HostLog = ServerHost.Log;
        }

        /// <summary>
        /// Starts the current engine, meaning all built-in services of the
        /// engine will get the chance to start.
        /// </summary>
        /// <remarks>
        /// This call is not blocking; after all built-in services has been
        /// started, control is returned to the host.
        /// </remarks>
        /// <returns>
        /// A reference to an implementation of <see cref="IServerRuntime"/>
        /// allowing the host to interact with the now running server.
        /// </returns>
        public IServerRuntime Start() {
            return this.CurrentPublicModel;
        }

        /// <summary>
        /// Stops the current engine, meaning all built-in services of the
        /// engine will get a request to stop.
        /// </summary>
        public void Stop() {
        }

        /// <summary>
        /// Creates a snapshot of this <see cref="ServerEngine"/> in the
        /// form of a public model <see cref="ServerInfo"/>.
        /// </summary>
        /// <returns>A <see cref="ServerInfo"/> representing the current state
        /// of this server.</returns>
        internal ServerInfo ToPublicModel() {
            var info = new ServerInfo() {
                Configuration = this.Configuration.Clone(),
                DefaultTransactionLogSize = this.DatabaseDefaultValues.TransactionLogSize,
                IsMonitoringSupported = false,
                ServerConfigurationPath = this.Configuration.ConfigurationFilePath,
                Uri = this.Uri,
                UserName = WindowsIdentity.GetCurrent().Name,
            };
            return info;
        }

        void SetupDatabases() {
            foreach (var databaseConfigPath in DatabaseConfiguration.GetAllFiles(this.DatabaseDirectory)) {
                var databaseDirectory = Path.GetDirectoryName(databaseConfigPath);
                var databaseName = Path.GetFileName(databaseDirectory).ToLowerInvariant();

                if (File.Exists(databaseConfigPath)) {
                    // If the file exist, it means this database exist and should
                    // be considered by the server.
                    var config = DatabaseConfiguration.Load(databaseConfigPath);
                    var database = new Database(this, config);
                    this.Databases.Add(databaseName, database);
                }

                // Check for orphaned database files and enque a command to drop
                // them if it does.

                var files = DeletedDatabaseFile.GetAllFromDirectory(databaseDirectory);
                if (files.Length > 0) {
                    Dispatcher.Enqueue(new DropDeletedDatabaseFilesCommand(this, databaseName));
                }
            }
        }
    }
}
