using System;

namespace Starcounter.Bootstrap.RuntimeHosts.SelfHosted
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
}
