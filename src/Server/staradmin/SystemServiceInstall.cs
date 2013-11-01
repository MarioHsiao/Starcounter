
using Starcounter.Server.Service;
using Starcounter.Server.Setup;
using Starcounter.Server.Windows;

namespace staradmin {

    internal static class SystemServiceInstall {

        internal static void Install() {
            var setup = new SystemServiceSetup();
            setup.StartupType = StartupType.Manual;
            setup.Execute();
        }

        internal static void Uninstall() {
            SystemServerService.Delete(SystemServerService.Name);
        }
    }
}
