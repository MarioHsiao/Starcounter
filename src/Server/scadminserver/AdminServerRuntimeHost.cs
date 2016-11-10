using Starcounter.Bootstrap.RuntimeHosts.Shared;
using Starcounter.Internal;

namespace scadminserver
{
    public class AdminServerRuntimeHost : AppSharedRuntimeHost
    {
        public AdminServerRuntimeHost() : base()
        {
            RedirectConsoleOutput = false;
            StarcounterEnvironment.IsAdministratorApp = true;
        }
    }
}
