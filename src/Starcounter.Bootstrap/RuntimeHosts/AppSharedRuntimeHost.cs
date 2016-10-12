
using System;

namespace Starcounter.Bootstrap.RuntimeHosts
{
    public class AppSharedRuntimeHost : RuntimeHost
    {
        public override void Run(Func<IHostConfiguration> configProvider, Action shutdownAuthority = null)
        {
            try
            {
                base.Run(configProvider, shutdownAuthority);
            }
            catch (Exception ex)
            {
                if (!StarcounterInternal.Hosting.ExceptionManager.HandleUnhandledException(ex)) throw;
            }
        }
    }
}
