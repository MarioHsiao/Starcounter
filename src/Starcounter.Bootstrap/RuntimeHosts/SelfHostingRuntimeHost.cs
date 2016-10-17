
using Starcounter.Hosting;
using System;

namespace Starcounter.Bootstrap.RuntimeHosts
{
    public class SelfHostingRuntimeHost : RuntimeHost
    {
        class LifetimeService : ILifetimeService
        {
            readonly SelfHostingRuntimeHost host;

            public LifetimeService(SelfHostingRuntimeHost h)
            {
                host = h;
            }

            void ILifetimeService.Configure(IHostConfiguration configuration)
            {
            }
            
            void ILifetimeService.Start(IntPtr schedulerContext)
            {    
            }

            void ILifetimeService.Run()
            {
                host.ApplicationMainLoop();
            }
        }

        class ExceptionManagerImpl : IExceptionManager
        {
            public bool HandleUnhandledException(Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// Callback invoked by the self-hosting host when ready for service.
        /// </summary>
        /// <remarks>
        /// The self-hosting host will invoke this callback and block the
        /// bootstrapping thread on it. When the application main loop return,
        /// the host will shut down and dispose.
        /// </remarks>
        internal Action ApplicationMainLoop { get; set; }

        protected override ILifetimeService CreateLifetimeService()
        {
            return new LifetimeService(this);
        }

        protected override IExceptionManager CreateExceptionManager()
        {
            return new ExceptionManagerImpl();
        }
    }
}
