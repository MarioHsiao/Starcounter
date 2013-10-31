using System;
using System.Runtime.InteropServices;

namespace Starcounter.Management.Win32
{
/// <summary>
/// PInvoke signatures for unmanaged service application related
/// native Win32 functions.
/// </summary>
public static class Win32Service
{
    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern IntPtr CreateService(
        IntPtr hSCManager,
        string lpServiceName,
        string lpDisplayName,
        uint dwDesiredAccess,
        uint dwServiceType,
        uint dwStartType,
        uint dwErrorControl,
        string lpBinaryPathName,
        string lpLoadOrderGroup,
        string lpdwTagId,
        string lpDependencies,
        string lpServiceStartName,
        string lpPassword);

    [DllImport("advapi32.dll", SetLastError = true)]
    public static extern IntPtr OpenService(IntPtr hSCManager, string lpServiceName, uint dwDesiredAccess);


    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CloseServiceHandle(IntPtr hSCObject);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DeleteService(IntPtr hService);


    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern Boolean ChangeServiceConfig(IntPtr hService, UInt32 nServiceType, UInt32 nStartType,
                                                     UInt32 nErrorControl, String lpBinaryPathName,
                                                     String lpLoadOrderGroup, IntPtr lpdwTagId,
                                                     String lpDependencies, String lpServiceStartName,
                                                     String lpPassword, String lpDisplayName);

    [DllImport("advapi32.dll", EntryPoint = "OpenSCManagerW", ExactSpelling = true, CharSet = CharSet.Unicode,
               SetLastError = true)]
    public static extern IntPtr OpenSCManager(string machineName, string databaseName, uint dwAccess);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ChangeServiceConfig2(
        IntPtr hService,
        int dwInfoLevel,
        IntPtr lpInfo);

    [Flags]
    private enum ACCESS_MASK : uint
    {
        DELETE = 0x00010000,
        READ_CONTROL = 0x00020000,
        WRITE_DAC = 0x00040000,
        WRITE_OWNER = 0x00080000,
        SYNCHRONIZE = 0x00100000,

        STANDARD_RIGHTS_REQUIRED = 0x000f0000,

        STANDARD_RIGHTS_READ = 0x00020000,
        STANDARD_RIGHTS_WRITE = 0x00020000,
        STANDARD_RIGHTS_EXECUTE = 0x00020000,

        STANDARD_RIGHTS_ALL = 0x001f0000,

        SPECIFIC_RIGHTS_ALL = 0x0000ffff,

        ACCESS_SYSTEM_SECURITY = 0x01000000,

        MAXIMUM_ALLOWED = 0x02000000,

        GENERIC_READ = 0x80000000,
        GENERIC_WRITE = 0x40000000,
        GENERIC_EXECUTE = 0x20000000,
        GENERIC_ALL = 0x10000000,

        DESKTOP_READOBJECTS = 0x00000001,
        DESKTOP_CREATEWINDOW = 0x00000002,
        DESKTOP_CREATEMENU = 0x00000004,
        DESKTOP_HOOKCONTROL = 0x00000008,
        DESKTOP_JOURNALRECORD = 0x00000010,
        DESKTOP_JOURNALPLAYBACK = 0x00000020,
        DESKTOP_ENUMERATE = 0x00000040,
        DESKTOP_WRITEOBJECTS = 0x00000080,
        DESKTOP_SWITCHDESKTOP = 0x00000100,

        WINSTA_ENUMDESKTOPS = 0x00000001,
        WINSTA_READATTRIBUTES = 0x00000002,
        WINSTA_ACCESSCLIPBOARD = 0x00000004,
        WINSTA_CREATEDESKTOP = 0x00000008,
        WINSTA_WRITEATTRIBUTES = 0x00000010,
        WINSTA_ACCESSGLOBALATOMS = 0x00000020,
        WINSTA_EXITWINDOWS = 0x00000040,
        WINSTA_ENUMERATE = 0x00000100,
        WINSTA_READSCREEN = 0x00000200,

        WINSTA_ALL_ACCESS = 0x0000037f
    }


    /// <summary>
    /// Access to the service. Before granting the requested access, the
    /// system checks the access token of the calling process.
    /// </summary>
    [Flags]
    public enum SERVICE_ACCESS : uint
    {
        /// <summary>
        /// Required to call the QueryServiceConfig and
        /// QueryServiceConfig2 functions to query the service configuration.
        /// </summary>
        SERVICE_QUERY_CONFIG = 0x00001,

        /// <summary>
        /// Required to call the ChangeServiceConfig or ChangeServiceConfig2 function
        /// to change the service configuration. Because this grants the caller
        /// the right to change the executable file that the system runs,
        /// it should be granted only to administrators.
        /// </summary>
        SERVICE_CHANGE_CONFIG = 0x00002,

        /// <summary>
        /// Required to call the QueryServiceStatusEx function to ask the service
        /// control manager about the status of the service.
        /// </summary>
        SERVICE_QUERY_STATUS = 0x00004,

        /// <summary>
        /// Required to call the EnumDependentServices function to enumerate all
        /// the services dependent on the service.
        /// </summary>
        SERVICE_ENUMERATE_DEPENDENTS = 0x00008,

        /// <summary>
        /// Required to call the StartService function to start the service.
        /// </summary>
        SERVICE_START = 0x00010,

        /// <summary>
        ///     Required to call the ControlService function to stop the service.
        /// </summary>
        SERVICE_STOP = 0x00020,

        /// <summary>
        /// Required to call the ControlService function to pause or continue
        /// the service.
        /// </summary>
        SERVICE_PAUSE_CONTINUE = 0x00040,

        /// <summary>
        /// Required to call the EnumDependentServices function to enumerate all
        /// the services dependent on the service.
        /// </summary>
        SERVICE_INTERROGATE = 0x00080,

        /// <summary>
        /// Required to call the ControlService function to specify a user-defined
        /// control code.
        /// </summary>
        SERVICE_USER_DEFINED_CONTROL = 0x00100,

        /// <summary>
        /// Includes STANDARD_RIGHTS_REQUIRED in addition to all access rights in this table.
        /// </summary>
        SERVICE_ALL_ACCESS = (ACCESS_MASK.STANDARD_RIGHTS_REQUIRED |
        SERVICE_QUERY_CONFIG |
        SERVICE_CHANGE_CONFIG |
        SERVICE_QUERY_STATUS |
        SERVICE_ENUMERATE_DEPENDENTS |
        SERVICE_START |
        SERVICE_STOP |
        SERVICE_PAUSE_CONTINUE |
        SERVICE_INTERROGATE |
        SERVICE_USER_DEFINED_CONTROL),

        GENERIC_READ = ACCESS_MASK.STANDARD_RIGHTS_READ |
        SERVICE_QUERY_CONFIG |
        SERVICE_QUERY_STATUS |
        SERVICE_INTERROGATE |
        SERVICE_ENUMERATE_DEPENDENTS,

        GENERIC_WRITE = ACCESS_MASK.STANDARD_RIGHTS_WRITE |
        SERVICE_CHANGE_CONFIG,

        GENERIC_EXECUTE = ACCESS_MASK.STANDARD_RIGHTS_EXECUTE |
        SERVICE_START |
        SERVICE_STOP |
        SERVICE_PAUSE_CONTINUE |
        SERVICE_USER_DEFINED_CONTROL,

        /// <summary>
        /// Required to call the QueryServiceObjectSecurity or
        /// SetServiceObjectSecurity function to access the SACL. The proper
        /// way to obtain this access is to enable the SE_SECURITY_NAME
        /// privilege in the caller's current access token, open the handle
        /// for ACCESS_SYSTEM_SECURITY access, and then disable the privilege.
        /// </summary>
        ACCESS_SYSTEM_SECURITY = ACCESS_MASK.ACCESS_SYSTEM_SECURITY,

        /// <summary>
        /// Required to call the DeleteService function to delete the service.
        /// </summary>
        DELETE = ACCESS_MASK.DELETE,

        /// <summary>
        /// Required to call the QueryServiceObjectSecurity function to query
        /// the security descriptor of the service object.
        /// </summary>
        READ_CONTROL = ACCESS_MASK.READ_CONTROL,

        /// <summary>
        /// Required to call the SetServiceObjectSecurity function to modify
        /// the Dacl member of the service object's security descriptor.
        /// </summary>
        WRITE_DAC = ACCESS_MASK.WRITE_DAC,

        /// <summary>
        /// Required to call the SetServiceObjectSecurity function to modify
        /// the Owner and Group members of the service object's security
        /// descriptor.
        /// </summary>
        WRITE_OWNER = ACCESS_MASK.WRITE_OWNER,
    }

    /// <summary>
    /// Service types.
    /// </summary>
    [Flags]
    public enum SERVICE_TYPE : uint
    {
        /// <summary>
        /// Driver service.
        /// </summary>
        SERVICE_KERNEL_DRIVER = 0x00000001,

        /// <summary>
        /// File system driver service.
        /// </summary>
        SERVICE_FILE_SYSTEM_DRIVER = 0x00000002,

        /// <summary>
        /// Service that runs in its own process.
        /// </summary>
        SERVICE_WIN32_OWN_PROCESS = 0x00000010,

        /// <summary>
        /// Service that shares a process with one or more other services.
        /// </summary>
        SERVICE_WIN32_SHARE_PROCESS = 0x00000020,

        /// <summary>
        /// The service can interact with the desktop.
        /// </summary>
        SERVICE_INTERACTIVE_PROCESS = 0x00000100,
    }

    /// <summary>
    /// Service start options
    /// </summary>
    public enum SERVICE_START : uint
    {
        /// <summary>
        /// A device driver started by the system loader. This value is valid
        /// only for driver services.
        /// </summary>
        SERVICE_BOOT_START = 0x00000000,

        /// <summary>
        /// A device driver started by the IoInitSystem function. This value
        /// is valid only for driver services.
        /// </summary>
        SERVICE_SYSTEM_START = 0x00000001,

        /// <summary>
        /// A service started automatically by the service control manager
        /// during system startup. For more information, see Automatically
        /// Starting Services.
        /// </summary>
        SERVICE_AUTO_START = 0x00000002,

        /// <summary>
        /// A service started by the service control manager when a process
        /// calls the StartService function. For more information, see
        /// Starting Services on Demand.
        /// </summary>
        SERVICE_DEMAND_START = 0x00000003,

        /// <summary>
        /// A service that cannot be started. Attempts to start the service
        /// result in the error code ERROR_SERVICE_DISABLED.
        /// </summary>
        SERVICE_DISABLED = 0x00000004,
    }


    /// <summary>
    /// Severity of the error, and action taken, if this service fails
    /// to start.
    /// </summary>
    public enum SERVICE_ERROR
    {
        /// <summary>
        /// The startup program ignores the error and continues the startup
        /// operation.
        /// </summary>
        SERVICE_ERROR_IGNORE = 0x00000000,

        /// <summary>
        /// The startup program logs the error in the event log but continues
        /// the startup operation.
        /// </summary>
        SERVICE_ERROR_NORMAL = 0x00000001,

        /// <summary>
        /// The startup program logs the error in the event log. If the
        /// last-known-good configuration is being started, the startup
        /// operation continues. Otherwise, the system is restarted with
        /// the last-known-good configuration.
        /// </summary>
        SERVICE_ERROR_SEVERE = 0x00000002,

        /// <summary>
        /// The startup program logs the error in the event log, if possible.
        /// If the last-known-good configuration is being started, the startup
        /// operation fails. Otherwise, the system is restarted with the
        /// last-known good configuration.
        /// </summary>
        SERVICE_ERROR_CRITICAL = 0x00000003,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SERVICE_FAILURE_ACTIONS
    {
        public int dwResetPeriod;
        public IntPtr lpRebootMsg;
        public IntPtr lpCommand;
        public int cActions;
        public IntPtr lpsaActions;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SERVICE_DESCRIPTION
    {
        public IntPtr lpDescription;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SC_ACTION
    {
        public SC_ACTION_TYPE Type;
        public uint Delay;
    }

    public enum SC_ACTION_TYPE
    {
        None = 0,
        RestartService = 1,
        RebootComputer = 2,
        Run_Command = 3
    }

    public enum SERVICE_CONFIGURATION_INFO_LEVEL
    {
        SERVICE_CONFIG_DESCRIPTION = 1,
        SERVICE_CONFIG_FAILURE_ACTIONS = 2,
        SERVICE_CONFIG_DELAYED_AUTO_START_INFO = 3,
        SERVICE_CONFIG_FAILURE_ACTIONS_FLAG = 4,
        SERVICE_CONFIG_SERVICE_SID_INFO = 5,
        SERVICE_CONFIG_REQUIRED_PRIVILEGES_INFO = 6,
        SERVICE_CONFIG_PRESHUTDOWN_INF = 7
    }

    [StructLayout(LayoutKind.Sequential)]
    public class QUERY_SERVICE_CONFIG
    {
        [MarshalAs(UnmanagedType.U4)]
        public UInt32 dwServiceType;

        [MarshalAs(UnmanagedType.U4)]
        public UInt32 dwStartType;

        [MarshalAs(UnmanagedType.U4)]
        public UInt32 dwErrorControl;

        [MarshalAs(UnmanagedType.LPWStr)]
        public String lpBinaryPathName;

        [MarshalAs(UnmanagedType.LPWStr)]
        public String lpLoadOrderGroup;

        [MarshalAs(UnmanagedType.U4)]
        public UInt32 dwTagID;

        [MarshalAs(UnmanagedType.LPWStr)]
        public String lpDependencies;

        [MarshalAs(UnmanagedType.LPWStr)]
        public String lpServiceStartName;

        [MarshalAs(UnmanagedType.LPWStr)]
        public String lpDisplayName;
    };

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern Boolean QueryServiceConfig(
        IntPtr hService,
        IntPtr intPtrQueryConfig,
        UInt32 cbBufSize,
        out UInt32 pcbBytesNeeded
    );

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public class SERVICE_STATUS_PROCESS
    {
        public uint serviceType;
        public uint currentState;
        public uint controlsAccepted;
        public uint win32ExitCode;
        public uint serviceSpecificExitCode;
        public uint checkPoint;
        public uint waitHint;
        public uint processID;
        public uint serviceFlags;
    }

    public const int SC_STATUS_PROCESS_INFO = 0x00;

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern unsafe bool QueryServiceStatusEx(
        IntPtr serviceHandle,
        int infoLevel,
        IntPtr buffer,
        int bufferSize,
        out int bytesNeeded
    );
}
}