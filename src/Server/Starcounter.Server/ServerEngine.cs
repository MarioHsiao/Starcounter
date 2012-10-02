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
    internal sealed class ServerEngine {
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

        internal ServerServices ServiceHost { get; private set; }

        /// <summary>
        /// Initializes a <see cref="ServerEngine"/>.
        /// </summary>
        /// <param name="serverConfigurationPath">Path to the server configuration
        /// file in the root of the server repository the engine will run.</param>
        internal ServerEngine(string serverConfigurationPath) {
            this.InstallationDirectory = Path.GetDirectoryName(typeof(ServerEngine).Assembly.Location);
            this.Configuration = ServerConfiguration.Load(Path.GetFullPath(serverConfigurationPath));
            this.DatabaseDefaultValues = new DatabaseDefaults();
            this.Name = this.Configuration.Name;
            this.Uri = ScUri.MakeServerUri(ScUri.GetMachineName(), this.Name);
            this.Databases = new Dictionary<string, Database>();
            this.DatabaseEngine = new DatabaseEngine(this);
            this.Dispatcher = new CommandDispatcher(this);

            // Assume for now interactive mode. This code is still just
            // to get up and running. We'll eventually utilize pipes and
            // spawn another thread, etc.
            Starcounter.ABCIPC.Server ipcServer;
            if (!Console.IsInputRedirected) {
                ipcServer = Utils.PromptHelper.CreateServerAttachedToPrompt();
            } else {
                ipcServer = new Starcounter.ABCIPC.Server(Console.In.ReadLine, Console.Out.WriteLine);
            }
            this.ServiceHost = new ServerServices(this, ipcServer, new NewtonSoftJsonSerializer(this));
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

            this.Dispatcher.DiscoverAssembly(GetType().Assembly);
            this.DatabaseEngine.Setup();
            this.DatabaseDefaultValues.Update(this.Configuration);
            SetupDatabases();
            this.CurrentPublicModel = new PublicModelProvider(this);
            this.ServiceHost.Setup();
        }

        internal void Start() {
            this.ServiceHost.Start();
        }

        internal void Stop() {
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
