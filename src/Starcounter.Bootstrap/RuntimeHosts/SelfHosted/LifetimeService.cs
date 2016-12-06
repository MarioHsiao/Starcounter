using System;
using System.Threading;

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
            if (host.ApplicationMainLoop != null)
            {
                Scheduling.ScheduleTask(host.ApplicationMainLoop, true);
            }

            if (!host.NonBlockingBootstrap)
            {
                Thread.Sleep(Timeout.Infinite);
            }
        }
    }
}
