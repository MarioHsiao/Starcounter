
using Starcounter;
using Starcounter.ABCIPC;
using Starcounter.ABCIPC.Internal;
using Starcounter.Configuration;
using Starcounter.Server.PublicModel;
using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.IO;

namespace Starcounter.Server {

    /// <summary>
    /// Representing the running server, hosted in a server program.
    /// </summary>
    internal sealed class ServerNode {

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

            this.DatabaseEngine.Setup();
            this.DatabaseDefaultValues.Update(this.Configuration);
            SetupDatabases();
            CurrentPublicModel = new PublicModelProvider(this);
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

            // To handle these two requests is the first 2.2 server
            // milestone.
            //   The creation should accept a single parameter (the
            // name) and use only defaults.

            ipcServer.Handle("CreateDatabase", delegate(Request request) {
                request.Respond(false, "Not implemented");
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

                request.Respond(string.Format("URI={0}, Files={1}", info.Uri, info.Configuration.Runtime.ImageDirectory));
            });

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
