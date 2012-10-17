

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

    internal static class Host
    {

        internal static unsafe void Configure(ServerConfiguration c)
        {
            byte* mem = (byte*)System.Runtime.InteropServices.Marshal.AllocHGlobal(128);

            ulong hmenv = ConfigureMemory(c, mem);
            mem += 128;

            ulong hlogs = ConfigureLogging(c, hmenv);
        }

        private static unsafe ulong ConfigureMemory(ServerConfiguration c, void* mem128)
        {
            uint slabs = (64 * 1024 * 1024) / 4096;  // 64 MB
            ulong hmenv = sccorelib.mh4_menv_create(mem128, slabs);
            if (hmenv != 0) return hmenv;
            throw ErrorCode.ToException(Error.SCERROUTOFMEMORY);
        }

        private static unsafe ulong ConfigureLogging(ServerConfiguration c, ulong hmenv)
        {
            uint e;

            e = sccorelog.SCInitModule_LOG(hmenv);
            if (e != 0) throw ErrorCode.ToException(e);

            ulong hlogs;
            e = sccorelog.SCConnectToLogs(ScUri.MakeServerUri(Environment.MachineName, c.Name), null, null, &hlogs);
            if (e != 0) throw ErrorCode.ToException(e);

            string logDirectory = c.LogDirectory;
            // logDirectory = "c:\\Test"; // TODO:
            e = sccorelog.SCBindLogsToDir(hlogs, logDirectory);
            if (e != 0) throw ErrorCode.ToException(e);

            Starcounter.Logging.LogManager.Setup(hlogs);

            return hlogs;
        }
    }

    /// <summary>
    /// Representing the running server, hosted in a server program.
    /// </summary>
    public sealed class ServerEngine {
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
        /// Gets the default name of the pipe the server use for it's
        /// core services.
        /// </summary>
        internal string DefaultServicePipeName {
            get {
                return string.Format("sc//{0}/{1}", Environment.MachineName, this.Name).ToLowerInvariant();
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
        /// Gets the current <see cref="AppsService"/> running as part of
        /// any engine.
        /// </summary>
        internal AppsService AppsService { get; private set; }

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
        /// Gets the <see cref="SharedMemoryMonitor"/> utilized by this server
        /// engine to monitor shared memory connections.
        /// </summary>
        internal SharedMemoryMonitor SharedMemoryMonitor { get; private set; }

        /// <summary>
        /// Initializes a <see cref="ServerEngine"/>.
        /// </summary>
        /// <param name="serverConfigurationPath">Path to the server configuration
        /// file in the root of the server repository the engine will run.</param>
        public ServerEngine(string serverConfigurationPath) {
            this.InstallationDirectory = Path.GetDirectoryName(typeof(ServerEngine).Assembly.Location);
            this.Configuration = ServerConfiguration.Load(Path.GetFullPath(serverConfigurationPath));
            this.DatabaseDefaultValues = new DatabaseDefaults();
            this.Name = this.Configuration.Name;
            this.Uri = ScUri.MakeServerUri(ScUri.GetMachineName(), this.Name);
            this.Databases = new Dictionary<string, Database>(StringComparer.InvariantCultureIgnoreCase);
            this.DatabaseEngine = new DatabaseEngine(this);
            this.Dispatcher = new CommandDispatcher(this);
            this.AppsService = new Server.AppsService(this);
            this.WeaverService = new Server.WeaverService(this);
            this.StorageService = new DatabaseStorageService(this);
            this.SharedMemoryMonitor = new SharedMemoryMonitor(this);
        }

        /// <summary>
        /// Executes setup of the current engine.
        /// </summary>
        public void Setup() {
            string serverRepositoryDirectory;
            string tempDirectory;
            string databaseDirectory;

            Host.Configure(this.Configuration);

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

            this.Dispatcher.Setup();
            this.DatabaseEngine.Setup();
            this.DatabaseDefaultValues.Update(this.Configuration);
            SetupDatabases();
            this.CurrentPublicModel = new PublicModelProvider(this);
            this.AppsService.Setup();
            this.WeaverService.Setup();
            this.StorageService.Setup();
            this.SharedMemoryMonitor.Setup();
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
            this.SharedMemoryMonitor.Start();
            // this.AppsService.Start();

            // Start all other built-in standard components, like the gateway,
            // the process monitor, etc.
            // TODO:

            return this.CurrentPublicModel;
        }

        /// <summary>
        /// Runs the server, blocking the calling thread, until it receives
        /// a notification to stop. Only core services are exposed.
        /// </summary>
        /// <seealso cref="Run(ServerServices)"/>
        public void Run() {
            // When ran without any services configured, we make sure at least
            // the core services are exposed, using named pipes on the local
            // machine, based on the server name.
            var ipcServer = ABCIPC.Internal.ClientServerFactory.CreateServerUsingNamedPipes(this.DefaultServicePipeName);
            var coreServices = new ServerServices(this, ipcServer);
            
            coreServices.Setup(ServerServices.ServiceClass.Core);

            Run(coreServices);
        }

        /// <summary>
        /// Runs the server, blocking the calling thread, until it receives
        /// a notification to stop. All services defined in the given
        /// <see cref="ServerServices"/> set are exposed.
        /// </summary>
        /// <seealso cref="Run(ServerServices)"/>
        public void Run(ServerServices services) {
            services.Start();
        }

        /// <summary>
        /// Stops the current engine, meaning all built-in services of the
        /// engine will get a request to stop.
        /// </summary>
        public void Stop() {
            this.AppsService.Stop();

            // Stop all other built-in standard components, like the gateway,
            // the process monitor, etc.
            // TODO:
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
