using System;

namespace Starcounter.Bootstrap
{
    /// <summary>
    /// Defines the semantics of a service that are in charge of the
    /// lifetime of a runtime host. The primary duty of such service
    /// is to provide a mechanism (Run()) for the runtime host to use
    /// to decide when it's time to shut down.
    /// </summary>
    public interface ILifetimeService
    {
        /// <summary>
        /// Configure the current life-time service.
        /// </summary>
        /// <param name="configuration"></param>
        void Configure(IHostConfiguration configuration);

        /// <summary>
        /// Announce to the lifetime service when the runtime host
        /// moves into "started" mode.
        /// </summary>
        /// <param name="schedulerContext"></param>
        void Start(IntPtr schedulerContext);

        /// <summary>
        /// Called by the runtime host, giving control to the lifetime
        /// service to run until told to shut down.
        /// </summary>
        void Run();
    }
}
