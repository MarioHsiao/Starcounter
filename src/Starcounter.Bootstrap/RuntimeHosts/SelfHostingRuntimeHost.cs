
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
                host.Entrypoint();
            }
        }

        /// <summary>
        /// TODO: Rename
        /// </summary>
        internal Action Entrypoint { get; set; }

        protected override ILifetimeService CreateLifetimeService()
        {
            return new LifetimeService(this);
        }
    }
}
