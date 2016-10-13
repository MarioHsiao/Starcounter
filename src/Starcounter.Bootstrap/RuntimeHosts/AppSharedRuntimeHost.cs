
using Starcounter.Bootstrap.Management;
using System;

namespace Starcounter.Bootstrap.RuntimeHosts
{
    public class AppSharedRuntimeHost : RuntimeHost
    {
        public override void Run(Func<IHostConfiguration> configProvider)
        {
            try
            {
                base.Run(configProvider);
            }
            catch (Exception ex)
            {
                if (!StarcounterInternal.Hosting.ExceptionManager.HandleUnhandledException(ex)) throw;
            }
        }

        protected override ILifetimeService CreateLifetimeService()
        {
            return new ManagementService();
        }
    }
}
