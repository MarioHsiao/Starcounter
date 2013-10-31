
namespace Starcounter.Server.Windows {

    public enum StartupType: uint {
        Automatic = Win32Service.SERVICE_START.SERVICE_AUTO_START,
        Manual = Win32Service.SERVICE_START.SERVICE_DEMAND_START,
        Disabled = Win32Service.SERVICE_START.SERVICE_DISABLED
    }
}
