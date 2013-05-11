
using Starcounter.Internal;
using System;
using System.Threading;

namespace Starcounter.Bootstrap.Management {
    /// <summary>
    /// Governs the interface the code host process expose over HTTP
    /// to allow code host level management.
    /// </summary>
    /// <remarks>
    /// Managing the code host is primarly done by the admin server as
    /// part of providing a way to run executables and start and stop
    /// engines and code host processes.
    /// </remarks>
    static internal class ManagementService {
        static ManualResetEvent shutdownEvent;

        /// <summary>
        /// Gets the host identity, used to expose management services
        /// over HTTP.
        /// </summary>
        public static string HostIdentity { get; private set; }

        /// <summary>
        /// Gets the port used to expose management services over HTTP.
        /// </summary>
        public static ushort Port { get; private set; }

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
        public static bool Unavailable { get; private set; }

        /// <summary>
        /// Gets a value that indicates if the current service is running
        /// in the administrator host.
        /// </summary>
        public static bool IsAdministrator { get; private set; }
        
        /// <summary>
        /// Initializes the management service.
        /// </summary>
        /// <param name="port">The port all handlers should register under.</param>
        /// <param name="hostIdentity">The identity of the host. Used when
        /// constructing and registering management URIs.</param>
        /// <param name="handleScheduler">Handle to the scheduler to use when
        /// management services need to schedule work to be done.</param>
        public static unsafe void Setup(ushort port, string hostIdentity, void* handleScheduler) {
            shutdownEvent = new ManualResetEvent(false);
            
            Unavailable = true;
            IsAdministrator = NewConfig.IsAdministratorApp;
            Port = port;
            HostIdentity = hostIdentity;
            
            if (!IsAdministrator) {
                CodeHostAPI.Setup(hostIdentity);
                new DbSession().RunSync(() => {
                    CodeHostHandler.Setup();
                    ExecutablesHandler.Setup(handleScheduler);
                });
            }
        }

        /// <summary>
        /// Instructs the service to shut down.
        /// </summary>
        public static void Shutdown() {
            shutdownEvent.Set();
        }

        /// <summary>
        /// Makes the management service functionality available to clients
        /// and blocks the calling thread until a request indicates the service
        /// should shut down (semantically eqivivalent to stopping the code
        /// host).
        /// </summary>
        public static void RunUntilShutdown() {
            Unavailable = false;
            shutdownEvent.WaitOne();
            Unavailable = true;
        }
    }
}