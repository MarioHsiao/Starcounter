
using Starcounter.Internal;
using System;
using System.Threading;

namespace Starcounter.Bootstrap.Management {
    
    /// <summary>
    /// Governs the interface the shared app code host process expose over HTTP
    /// to allow code host level management.
    /// </summary>
    /// <remarks>
    /// Managing the code host is primarly done by the admin server as
    /// part of providing a way to run executables and start and stop
    /// engines and code host processes.
    /// </remarks>
    internal class ManagementService : ILifetimeService {
        const string onlineBaseName = "SCCODE_EXE_";
        ManualResetEvent shutdownEvent;
        EventWaitHandle onlineEvent;
        IHostConfiguration configuration;

        /// <summary>
        /// Gets the host identity, used to expose management services
        /// over HTTP.
        /// </summary>
        public string HostIdentity { get; private set; }

        /// <summary>
        /// Gets the port used to expose management services over HTTP.
        /// </summary>
        public ushort Port { get; private set; }

        /// <summary>
        /// Governs the availability of the management service.
        /// </summary>
        /// <remarks>
        /// When the service is unavailable, all HTTP requests to any of
        /// its  resources will return a 503. Normally, the service will
        /// be unavailable during the bootstrap sequence of the code host
        /// and after it has been shut down (and before the process exits
        /// while handlers are still registered).
        /// </remarks>
        public bool Unavailable { get; private set; }

        /// <summary>
        /// Gets a value that indicates if the current service is running
        /// in the administrator host.
        /// </summary>
        public bool IsAdministrator { get; private set; }

        public void Configure(IHostConfiguration config)
        {
            configuration = config;
            HostIdentity = config.Name;
            Unavailable = true;
            shutdownEvent = new ManualResetEvent(false);
            onlineEvent = new EventWaitHandle(false, EventResetMode.ManualReset, string.Concat(onlineBaseName, HostIdentity.ToUpperInvariant()));
        }

        public void Start(IntPtr schedulerContext)
        {
            unsafe
            {
                Setup(configuration.DefaultSystemHttpPort, schedulerContext.ToPointer(), !configuration.NoNetworkGateway);
            }
        }

        public void Run()
        {
            RunUntilShutdown();
        }

        /// <summary>
        /// Sets up the management service.
        /// </summary>
        /// <param name="port">The port all handlers should register under.</param>
        /// <param name="handleScheduler">Handle to the scheduler to use when
        /// management services need to schedule work to be done.</param>
        /// <param name="setupAPI">Indicates if the API should be set up.</param>
        unsafe void Setup(ushort port, void* handleScheduler, bool setupAPI = true) {

            // TODO: This should be responsibility of custom implementation
            // It should be made configurable if hosting services should be
            // supported or not.
            IsAdministrator = StarcounterEnvironment.IsAdministratorApp;
            Port = port;
            
            if (IsAdministrator) {
                setupAPI = false;
            }
            
            if (setupAPI) {
                CodeHostAPI.Setup(this);
                Scheduling.ScheduleTask(() => {
                    CodeHostHandler.Setup(this);
                    ExecutablesHandler.Setup(this, handleScheduler);
                }, true);
            }
        }

        /// <summary>
        /// Instructs the service to shut down.
        /// </summary>
        public void Shutdown() {
            shutdownEvent.Set();
        }

        /// <summary>
        /// Makes the management service functionality available to clients
        /// and blocks the calling thread until a request indicates the service
        /// should shut down (semantically eqivivalent to stopping the code
        /// host).
        /// </summary>
        void RunUntilShutdown() {
            Unavailable = false;
            onlineEvent.Set();
            shutdownEvent.WaitOne();
            onlineEvent.Reset();
            onlineEvent.Close();
            Unavailable = true;
        }
    }
}