
using Starcounter.Bootstrap.RuntimeHosts;
using Starcounter.Internal;

namespace scadminserver
{
    public class AdminServerRuntimeHost : AppSharedRuntimeHost
    {
        public AdminServerRuntimeHost()
        {
            StarcounterEnvironment.IsAdministratorApp = true;
        }
    }
}
