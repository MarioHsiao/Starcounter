
using System;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.ServiceProcess;
using Starcounter.Advanced.Configuration;
using Starcounter.Management.Win32;
using Starcounter.Internal;

namespace Starcounter.InstallerEngine
{
/// <summary>
/// Exposes utility methods to discover Windows services that configures
/// local system servers, allows verification of them and allows their
/// metadata to be easily accessed. Also contains the (internal) functionality
/// to create server services.
/// </summary>
public static class SystemServerService
{
    /// <summary>
    /// Gets a value indicating if <paramref name="service"/> represents
    /// the configuration of a Starcounter system server.
    /// </summary>
    /// <param name="service">
    /// The service to check.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="service"/> is identified
    /// as a configured system service, <see langword="false"/> if not.
    /// </returns>
    public static bool IsSystemServerService(ServiceController service)
    {
        return service.ServiceName.StartsWith(StarcounterEnvironment.ServerNames.SystemServerServiceName,
            StringComparison.InvariantCultureIgnoreCase);
    }

    /// <summary>
    /// Creates a Starcounter server service using the supplied parameters
    /// and internal knowledge.
    /// </summary>
    /// <param name="serviceManagerHandle">Handle to the service manager. Must
    /// be opened with the privilege to create new services in the service
    /// database.</param>
    /// <param name="displayName">The display name to use. Pass <see langword="null"/>
    /// to use the default display name.</param>
    /// <param name="description">A text describing the functionality of the
    /// service. Pass <see langword="null"/> to use the default description.
    /// </param>
    /// <param name="startupType">The startup type of the service.</param>
    /// <param name="commandLine">The command line of the service, referencing the
    /// Starcounter server executable and the arguments to pass to it.</param>
    /// <param name="user">The user under which the service should run.</param>
    /// <param name="password">The password to use by the system when logging on
    /// as the given <paramref name="user"/>.</param>
    /// <param name="serviceName"></param>
    public static void Create(
        IntPtr serviceManagerHandle,
        string displayName,
        string serviceName,
        string description,
        StartupType startupType,
        string commandLine,
        string user,
        string password)
    {
        Win32Service.SERVICE_START serviceStart;
        string serviceNameCandidate;
        int serviceNumber;
        string serviceDisplayName;
        string serviceDisplayNameCandidate;
        IntPtr serviceHandle;
        int win32error;

        serviceNumber = 0;
        serviceNameCandidate = serviceName;
        serviceDisplayName = serviceDisplayNameCandidate = displayName ?? "Starcounter System Server";

        switch (startupType)
        {
            case StartupType.Automatic:
                serviceStart = Win32Service.SERVICE_START.SERVICE_AUTO_START;
                break;
            case StartupType.Manual:
                serviceStart = Win32Service.SERVICE_START.SERVICE_DEMAND_START;
                break;
            case StartupType.Disable:
            default:
                serviceStart = Win32Service.SERVICE_START.SERVICE_DISABLED;
                break;
        }

        // NOTE:
        // If the Starcounter service account has been altered, assuring it
        // might return credentials out of date. If so, the CreateService API
        // will verify the correctness of the credentials (including the
        // password) and return false with GetLastError() returning code 1057.
        // Since this scenario MIGHT happen (if users remove or alter the
        // service account), this error is one we should consider providing
        // a helping hand with.

        createService:
        serviceHandle = Win32Service.CreateService(
                            serviceManagerHandle,
                            serviceNameCandidate,
                            serviceDisplayNameCandidate,
                            (uint)Win32Service.SERVICE_ACCESS.SERVICE_ALL_ACCESS,
                            (uint)Win32Service.SERVICE_TYPE.SERVICE_WIN32_OWN_PROCESS,
                            (uint)serviceStart,
                            (uint)Win32Service.SERVICE_ERROR.SERVICE_ERROR_NORMAL,
                            commandLine,
                            null,
                            null,
                            null,
                            string.Format(".\\{0}", user),
                            password
                        );
        if (serviceHandle == IntPtr.Zero)
        {
            win32error = Marshal.GetLastWin32Error();
            
            if (win32error == 1073 || win32error == 1078)
            {
                // Dont let non-crucial conflicting service names or service
                // display names hinder us.
                
                serviceNumber++;
                if (win32error == 1073)
                {
                    serviceNameCandidate = string.Format("{0}{1}", serviceName, serviceNumber);
                }
                else
                {
                    serviceDisplayNameCandidate = string.Format("{0} ({1})", serviceDisplayName, serviceNumber);
                }
                goto createService;
            }
            throw new Win32Exception(win32error);
        }
        try
        {
            // Configure the service for our needs and add it to the
            // remote configuration if instructed to, and make sure to
            // execute all further setup code guarded with a finally
            // clause that properly closes the service handle.
            
            ConfigureService(serviceHandle, description);
        }
        finally
        {
            Win32Service.CloseServiceHandle(serviceHandle);
        }
    }

    /// <summary>
    /// Deletes a Starcounter server service (removing it from the registry)
    /// given it's service name.
    /// </summary>
    /// <param name="serviceName">Name of the service to delete.</param>
    public static void Delete(string serviceName)
    {
        if (string.IsNullOrEmpty(serviceName)) throw new ArgumentNullException("serviceName");

        Delete(new ServiceController(serviceName));
    }

    /// <summary>
    /// Deletes a Starcounter server service (removing it from the registry)
    /// given a reference to a <see cref="ServiceController"/> referencing it.
    /// </summary>
    /// <param name="service">A <see cref="ServiceController"/> referencing
    /// the service to delete.</param>
    internal static void Delete(ServiceController service)
    {
        IntPtr serviceManagerHandle;

        if (service == null) throw new ArgumentNullException("service");

        serviceManagerHandle = 
            Win32Service.OpenSCManager(null, null, (uint)Win32Service.SERVICE_ACCESS.SERVICE_CHANGE_CONFIG);
        if (serviceManagerHandle == IntPtr.Zero)
            throw new 
                System.ComponentModel.Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error());

        try
        {
            SystemServerService.Delete(serviceManagerHandle, service);
        }
        finally
        {
            Win32Service.CloseServiceHandle(serviceManagerHandle);
        }
    }

    /// <summary>
    /// Deletes a Starcounter server service (removing it from the registry)
    /// given it's service name.
    /// </summary>
    /// <param name="serviceManagerHandle">A handle to the service manager.
    /// The manager handle must be opened with the permission to change the
    /// service configuration (i.e. SERVICE_ACCESS.SERVICE_CHANGE_CONFIG).
    /// </param>
    /// <param name="serviceName">Name of the service to delete.</param>
    internal static void Delete(
        IntPtr serviceManagerHandle,
        string serviceName)
    {
        if (string.IsNullOrEmpty(serviceName)) throw new ArgumentNullException("serviceName");

        Delete(serviceManagerHandle, new ServiceController(serviceName));
    }

    /// <summary>
    /// Deletes a Starcounter server service (removing it from the registry)
    /// given a reference to a <see cref="ServiceController"/> referencing it.
    /// </summary>
    /// <param name="serviceManagerHandle">A handle to the service manager.
    /// The manager handle must be opened with the permission to change the
    /// service configuration (i.e. SERVICE_ACCESS.SERVICE_CHANGE_CONFIG).
    /// </param>
    /// <param name="service">A <see cref="ServiceController"/> referencing
    /// the service to delete.</param>
    internal static void Delete(IntPtr serviceManagerHandle, ServiceController service)
    {
        IntPtr serviceHandle;
        bool result;

        if (serviceManagerHandle == IntPtr.Zero) throw new ArgumentNullException("serviceManagerHandle");
        if (service == null) throw new ArgumentNullException("service");

        serviceHandle = Win32Service.OpenService(
            serviceManagerHandle, service.ServiceName, (uint)Win32Service.SERVICE_ACCESS.DELETE);
        if (serviceHandle == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error());

        try
        {
            result = Win32Service.DeleteService(serviceHandle);
            if (result == false)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }
        finally
        {
            Win32Service.CloseServiceHandle(serviceHandle);
        }

    }

    private static unsafe void ConfigureService(IntPtr serviceHandle, string description)
    {
        Win32Service.SERVICE_DESCRIPTION pDescription;
        bool result;
        const int actionCount = 3;
        // Add a description to the service
        description = description ?? string.Format(
                          "Manages Starcounter databases. If this service is disabled, the databases in the catalogue of Starcounter " +
                          "databases it maintains will not be started or monitored, and neither will they be displayed in client programs " +
                          "such as 'Starcounter Administrator' and the 'Starcounter for Visual Studio' extension."
                      );
        pDescription.lpDescription = Marshal.StringToHGlobalUni(description);
        try
        {
            result = Win32Service.ChangeServiceConfig2(
                         serviceHandle,
                         (int)Win32Service.SERVICE_CONFIGURATION_INFO_LEVEL.SERVICE_CONFIG_DESCRIPTION,
                         new IntPtr( & pDescription)
                     );
            if (result == false)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }
        finally
        {
            Marshal.FreeHGlobal(pDescription.lpDescription);
        }
        // Configure recovery actions, the strategy for the service manager to use if
        // the service does not work as expected.
        Win32Service.SC_ACTION * pActions = stackalloc Win32Service.SC_ACTION[actionCount];

        // First failure restart is done immediately.
        pActions[0].Type = Win32Service.SC_ACTION_TYPE.RestartService;
        pActions[0].Delay = 60 * 1000;

        // Second failure restart is done after 1 minute.
        pActions[1].Type = Win32Service.SC_ACTION_TYPE.RestartService;
        pActions[1].Delay = 60 * 1000;

        // No third failure restart.
        pActions[2].Type = Win32Service.SC_ACTION_TYPE.None;
        pActions[2].Delay = 0;

        Win32Service.SERVICE_FAILURE_ACTIONS failureActions = new Win32Service.SERVICE_FAILURE_ACTIONS
        {
            dwResetPeriod = 120 * 60,
            cActions = actionCount,
            lpsaActions = (IntPtr)pActions
        };
        result = Win32Service.ChangeServiceConfig2(
                     serviceHandle,
                     (int)Win32Service.SERVICE_CONFIGURATION_INFO_LEVEL.SERVICE_CONFIG_FAILURE_ACTIONS,
                     new IntPtr( & failureActions)
                 );
        if (result == false)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }

}
}