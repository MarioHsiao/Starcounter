
using Starcounter.Bootstrap.Management;
using Starcounter.Hosting;
using System;

namespace Starcounter.Bootstrap.RuntimeHosts.Shared
{
    public class AppSharedRuntimeHost : RuntimeHost
    {
        public AppSharedRuntimeHost() : base()
        {
            RedirectConsoleOutput = true;
        }

        public override void Run(Func<IHostConfiguration> configProvider, Func<IAppStart> autoStartProvdier = null)
        {
            try
            {
                base.Run(configProvider, autoStartProvdier);
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

        protected override IExceptionManager CreateExceptionManager()
        {
            return new ExceptionManager();
        }

        protected override IAssemblyResolver CreateAssemblyResolver()
        {
            return new AssemblyResolver(new PrivateAssemblyStore());
        }
    }
}
