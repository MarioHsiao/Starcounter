using System;
using Microsoft.Win32;
using System.IO;
using System.ServiceProcess;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Security.AccessControl;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ComponentModel;
using Starcounter;
using Starcounter.Management.Win32;
using Starcounter.Configuration;
using Starcounter.Internal;
using Starcounter.Server.Setup;

namespace Starcounter.InstallerEngine
{
public class CSystemServer : CComponentBase
{
    /// <summary>
    /// Component initialization.
    /// </summary>
    public CSystemServer()
    {
    }

    /// <summary>
    /// Provides descriptive name of the components.
    /// </summary>
    public override String DescriptiveName
    {
        get
        {
            return "Starcounter System Server Component";
        }
    }

    /// <summary>
    /// Provides installation path of the component.
    /// </summary>
    public override String ComponentPath
    {
        get
        {
            return InstallerMain.GetInstallationSettingValue(ConstantsBank.Setting_SystemServerPath);
        }
    }

    /// <summary>
    /// Provides name of the component setting.
    /// </summary>
    public override String SettingName
    {
        get
        {
            return ConstantsBank.Setting_InstallSystemServer;
        }
    }

    /// <summary>
    /// Starts all available Starcounter service on the machine.
    /// </summary>
    internal static void StartStarcounterServices()
    {
        // Logging event.
        Utilities.ReportSetupEvent("Starting Starcounter services...");

        // Obtaining a list of all services in the system.
        ServiceController[] allServices = ServiceController.GetServices();
        foreach (ServiceController someService in allServices)
        {
            // Selecting the service which starts with Starcounter keyword.
            if (someService.ServiceName.StartsWith(ConstantsBank.SCProductName, StringComparison.CurrentCultureIgnoreCase))
            {
                // We have found Starcounter service so checking if its started.
                if (someService.Status != ServiceControllerStatus.Running &&
                    someService.Status != ServiceControllerStatus.StartPending &&
                    someService.Status != ServiceControllerStatus.ContinuePending)
                {
                    // Starting the service.
                    someService.Start();
                }

                // Now waiting for the stop status for the server.
                someService.WaitForStatus(ServiceControllerStatus.Running);

                // Releasing service resources.
                someService.Close();
            }
        }
    }

    public void InstallSMMonitor(String serviceAccountName, String serviceAccountPassword, String pathToResources)
    {
        IntPtr serviceManagerHandle = Win32Service.OpenSCManager(null, null, (uint) Win32Service.SERVICE_ACCESS.SERVICE_CHANGE_CONFIG);
        if (serviceManagerHandle == IntPtr.Zero)
        {
            throw ErrorCode.ToException(Error.SCERRINSTALLERINTERNALPROBLEM, "GetLastWin32Error returned: " + Marshal.GetLastWin32Error());
        }

        try
        {
            // Creating service command line in specified binaries folder.
            String commandLine = "\"" + Path.Combine(InstallerMain.InstallationBaseComponent.ComponentPath, "SMMonitor.exe") + "\"" + " \"" + pathToResources + "\"";

            // Creating service with specified parameters.
            SystemServerService.Create(
                serviceManagerHandle,
                "Starcounter Connectivity Shared Memory Monitor",
                "StarcounterSMMonitor",
                "Handles Starcounter connectivity shared memory resources.",
                StartupType.Automatic,
                commandLine,
                serviceAccountName,
                serviceAccountPassword
                );
        }
        finally
        {
            Win32Service.CloseServiceHandle(serviceManagerHandle);
        }
    }

    // Path to server configuration file.
    String systemServerConfigPath_ = null;
    String SystemServerConfigPath
    {
        get
        {
            if (systemServerConfigPath_ != null)
                return systemServerConfigPath_;

            if (InstallerMain.InstallationDir == null)
                return null;

            systemServerConfigPath_ = Path.Combine(InstallerMain.InstallationDir, StarcounterEnvironment.ServerNames.SystemServer + ".xml");

            return systemServerConfigPath_;
        }
    }

    readonly String SystemServerAdminDesktopShortcutPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                     ConstantsBank.SCProductName + " " + StarcounterEnvironment.ServerNames.SystemServer + " Administrator.lnk");

    /// <summary>
    /// Installs component.
    /// </summary>
    public override void Install()
    {
        // Checking if component should be installed in this session.
        if (!ShouldBeInstalled())
            return;

        // Checking that component is not already installed.
        if (!CanBeInstalled())
            return;

        // Server related directories.
        String serverDir = ComponentPath;

        // Checking for existing server directory.
        CPersonalServer.CheckExistingServerDirectory(serverDir);

        // Logging event.
        Utilities.ReportSetupEvent("Creating environment variables for system server...");

        // Setting the installation path environment variable.
        Environment.SetEnvironmentVariable(ConstantsBank.SCEnvVariableName,
            InstallerMain.InstallationBaseComponent.ComponentPath,
            EnvironmentVariableTarget.Machine);

        // Setting the default server environment variable.
        Environment.SetEnvironmentVariable(ConstantsBank.SCEnvVariableDefaultServer,
            ConstantsBank.SCSystemDatabasesName,
            EnvironmentVariableTarget.Machine);

        // Logging event.
        Utilities.ReportSetupEvent("Installing system server and service...");

        String serviceAccountName, serviceAccountPassword,
               installPath = InstallerMain.InstallationBaseComponent.ComponentPath;

        // Creating new server repository.
        var setup = RepositorySetup.NewDefault(
            Path.Combine(serverDir, ".."),
            StarcounterEnvironment.ServerNames.SystemServer);

        setup.Execute();

        String serverConfigPath = Path.Combine(serverDir, StarcounterEnvironment.ServerNames.SystemServer + ServerConfiguration.FileExtension);

        // Replacing default server parameters.
        if (!Utilities.ReplaceXMLParameterInFile(
            serverConfigPath,
            StarcounterConstants.BootstrapOptionNames.DefaultUserHttpPort,
            InstallerMain.GetInstallationSettingValue(ConstantsBank.Setting_DefaultSystemServerUserHttpPort)))
        {
            throw ErrorCode.ToException(Error.SCERRINSTALLERINTERNALPROBLEM,
                "Can't replace default Apps port for " + StarcounterEnvironment.ServerNames.SystemServer + " server.");
        }

        // Replacing default server parameters.
        if (!Utilities.ReplaceXMLParameterInFile(
            Path.Combine(serverDir, StarcounterEnvironment.ServerNames.SystemServer + ServerConfiguration.FileExtension),
            ServerConfiguration.SystemHttpPortString,
            InstallerMain.GetInstallationSettingValue(ConstantsBank.Setting_DefaultSystemServerSystemHttpPort)))
        {
            throw ErrorCode.ToException(Error.SCERRINSTALLERINTERNALPROBLEM,
                "Can't replace Administrator TCP port for " + StarcounterEnvironment.ServerNames.SystemServer + " server.");
        }

        // Replacing default server parameters.
        if (!Utilities.ReplaceXMLParameterInFile(
            Path.Combine(serverDir, StarcounterEnvironment.ServerNames.SystemServer + ServerConfiguration.FileExtension),
            StarcounterConstants.BootstrapOptionNames.SQLProcessPort,
            InstallerMain.GetInstallationSettingValue(ConstantsBank.Setting_DefaultSystemPrologSqlProcessPort)))
        {
            throw ErrorCode.ToException(Error.SCERRINSTALLERINTERNALPROBLEM,
                "Can't replace Prolog SQL TCP port for " + StarcounterEnvironment.ServerNames.SystemServer + " server.");
        }

        // Setting the given system server system HTTP port on the machine
        int serverSystemPort;
        var serverSystemPortString = InstallerMain.GetInstallationSettingValue(ConstantsBank.Setting_DefaultSystemServerSystemHttpPort);
        if (string.IsNullOrEmpty(serverSystemPortString) || !int.TryParse(serverSystemPortString, out serverSystemPort)) {
            throw ErrorCode.ToException(
                Error.SCERRINSTALLERINTERNALPROBLEM,
                "Unable to properly access given server HTTP port value: " + serverSystemPortString ?? "NULL");
        }
        Environment.SetEnvironmentVariable(
            ConstantsBank.SCEnvVariableDefaultSystemPort,
            serverSystemPortString,
            EnvironmentVariableTarget.Machine);

        // Creating server config.
        InstallerMain.CreateServerConfig(
            StarcounterEnvironment.ServerNames.SystemServer,
            ComponentPath,
            SystemServerConfigPath);

        // Testing if service account exists or creating a new one.
        SystemServerAccount.ChangeInstallationPlatform(true);
        SystemServerAccount.AssureAccount(
            installPath,
            serverDir,
            out serviceAccountName,
            out serviceAccountPassword);

        // Creating system database server in default path.
        SystemServerAccount.CreateService(
            installPath,
            StarcounterEnvironment.ServerNames.SystemServerServiceName,
            serviceAccountName,
            serviceAccountPassword);

        // Copying gateway configuration.
        InstallerMain.CopyGatewayConfig(
            serverDir,
            InstallerMain.GetInstallationSettingValue(ConstantsBank.Setting_DefaultSystemServerSystemHttpPort));

        // Creating Administrator database.
        // TODO: Recover if in need of a database for Administrator.
        /*InstallerMain.CreateDatabaseSynchronous(
            StarcounterEnvironment.ServerNames.SystemServer,
            ComponentPath,
            ConstantsBank.SCAdminDatabaseName);*/

        // Calling external tool to create Administrator shortcut.
        Utilities.CreateShortcut(
            "http://127.0.0.1:" + InstallerMain.GetInstallationSettingValue(ConstantsBank.Setting_DefaultSystemServerSystemHttpPort),
            SystemServerAdminDesktopShortcutPath,
            "",
            installPath,
            "Starts " + ConstantsBank.SCProductName + " " + StarcounterEnvironment.ServerNames.SystemServer + " Administrator.",
            Path.Combine(InstallerMain.InstallationDir, ConstantsBank.SCIconFilename));

        // Starting the service.
        StartStarcounterServices();

        // Updating progress.
        InstallerMain.ProgressIncrement();
    }

    /// <summary>
    /// Deletes Starcounter service and user.
    /// </summary>
    void DeleteServicesAndUser()
    {
        // Logging event.
        Utilities.ReportSetupEvent("Deleting Starcounter service and user...");

        // Searching for a user and removing all access rights.
        UserPrincipal scUser = UserPrincipal.FindByIdentity(new PrincipalContext(ContextType.Machine, Environment.MachineName),
            SystemServerAccount.UserName);

        // Checking if user really exists.
        if (scUser != null)
        {
            // Creating file system rule that we need to remove.
            FileSystemAccessRule fsr = new FileSystemAccessRule(scUser.Sid,
                FileSystemRights.FullControl, // Removing all possible rights.
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.InheritOnly,
                AccessControlType.Allow);

            // We have try here since installation settings probably could not be loaded.
            try
            {
                // Removing access rules for Starcounter user for installation directory.
                Utilities.RemoveDirectoryAccessRights(new DirectoryInfo(InstallerMain.InstallationDir), fsr);

                // Removing access rules for Starcounter user for system AppData directory.
                Utilities.RemoveDirectoryAccessRights(new DirectoryInfo(StarcounterEnvironment.Directories.SystemAppDataDirectory), fsr);

                // Removing access rules for Starcounter user for system server installation directory.
                Utilities.RemoveDirectoryAccessRights(new DirectoryInfo(ComponentPath), fsr);
            }
            catch { }

            try
            {
                // Deleting user.
                DirectoryEntry localMachine = new DirectoryEntry("WinNT://" + Environment.MachineName + ",computer");
                DirectoryEntry scUserEntry = localMachine.Children.Find(SystemServerAccount.UserName);
                localMachine.Children.Remove(scUserEntry);
                localMachine.CommitChanges();
            }
            catch { }

            // Deleting Starcounter user folder.
            String pathToUserDirs = Path.Combine(ConstantsBank.ProgramFilesPath, @"..\Users");
            String[] allUserDirs = Directory.GetDirectories(pathToUserDirs);
            foreach (String userDirPath in allUserDirs)
            {
                // Checking if name of the user folder starts with Starcounter user name.
                DirectoryInfo userDirInfo = new DirectoryInfo(userDirPath);
                if (userDirInfo.Name.StartsWith(SystemServerAccount.UserName, StringComparison.CurrentCultureIgnoreCase))
                {
                    Utilities.ForceDeleteDirectory(userDirInfo);
                }
            }
        }

        // Obtaining a list of all services in the system.
        ServiceController[] allServices = ServiceController.GetServices();
        foreach (ServiceController someService in allServices)
        {
            // Selecting the service which starts with Starcounter keyword.
            if (someService.ServiceName.StartsWith(ConstantsBank.SCProductName, StringComparison.CurrentCultureIgnoreCase))
            {
                if (someService.Status != ServiceControllerStatus.Stopped &&
                    someService.Status != ServiceControllerStatus.StopPending)
                {
                    // Stopping the service.
                    someService.Stop();
                }

                // Now waiting for the stop status for the server.
                someService.WaitForStatus(ServiceControllerStatus.Stopped);

                // Releasing service resources.
                someService.Close();

                // Eventually deleting the service.
                SystemServerService.Delete(someService.ServiceName);
            }
        }

        // Checking for Starcounter service existence.
        Boolean serviceStillFound = false;
        for (Int32 i = 0; i < 5; i++)
        {
            allServices = ServiceController.GetServices();
            foreach (ServiceController someService in allServices)
            {
                if (someService.ServiceName.StartsWith(ConstantsBank.SCProductName, StringComparison.CurrentCultureIgnoreCase))
                {
                    serviceStillFound = true;
                    break;
                }
            }

            // If service does not exist we are stopping the search procedure.
            if (!serviceStillFound)
                break;

            Thread.Sleep(1000);
        }

        // If service is still found - panic!
        String servicePanicMsg = "Starcounter system service still exists in the system and can not be completely removed." + Environment.NewLine +
                                 "Please check that Starcounter service is not blocked by any application or restart the system.";

        if (serviceStillFound)
        {
            if (InstallerMain.SilentFlag)
            {
                // Printing a console message.
                Utilities.ConsoleMessage(servicePanicMsg);
            }
            else
            {
                Utilities.MessageBoxInfo(servicePanicMsg, "Starcounter service still exists in the system...");
            }
        }

        // Disabling service start at the end.
        InstallerMain.StartService = false;
    }

    /// <summary>
    /// Removes component.
    /// </summary>
    public override void Uninstall()
    {
        if (!UninstallEngine.CompleteCleanupSetting)
        {
            if (UninstallEngine.RollbackSetting)
            {
                // Checking if component was installed in this session.
                if (!ShouldBeInstalled())
                    return;
            }
            else // Standard removal.
            {
                // Checking if component is selected to be removed.
                if (!ShouldBeRemoved())
                    return;

                // Checking if component can be removed.
                if (!CanBeRemoved())
                    return;
            }
        }

        // Deleting Starcounter services and user.
        DeleteServicesAndUser();

        // Logging event.
        Utilities.ReportSetupEvent("Deleting system server environment variables...");

        // Removing installation environment variable.
        Environment.SetEnvironmentVariable(ConstantsBank.SCEnvVariableName,
            null,
            EnvironmentVariableTarget.Machine);

        // Removing default server environment variable.
        Environment.SetEnvironmentVariable(ConstantsBank.SCEnvVariableDefaultServer,
            null,
            EnvironmentVariableTarget.Machine);

        Environment.SetEnvironmentVariable(
            ConstantsBank.SCEnvVariableDefaultSystemPort,
            null,
            EnvironmentVariableTarget.Machine);

        // Logging event.
        Utilities.ReportSetupEvent("Deleting system server registry entries...");

        RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE", true);

        // Looking if 'Starcounter' key exists and trying to delete it.
        if ((registryKey != null) && (registryKey.OpenSubKey(ConstantsBank.SCProductName, true) != null))
            registryKey.DeleteSubKeyTree(ConstantsBank.SCProductName);

        // Deleting personal server configuration folder if demanded.
        Utilities.ReportSetupEvent("Deleting system database server configuration...");

        // Getting the server installation path.
        try
        {
            String systemServerPath = ComponentPath;

            if (Utilities.DirectoryIsNotEmpty(new DirectoryInfo(systemServerPath)))
            {
                InstallerMain.FinalSetupMessage += "You can find and manually delete the System Server databases directory in: '" +
                    systemServerPath + "'" + Environment.NewLine;
            }
        }
        catch { }

        // Deleting the config file.
        if (File.Exists(SystemServerConfigPath))
            File.Delete(SystemServerConfigPath);

        // Updating progress.
        InstallerMain.ProgressIncrement();
    }

    /// <summary>
    /// Checks if components is already installed.
    /// </summary>
    /// <returns>True if already installed.</returns>
    public override Boolean IsInstalled()
    {
        // Checking for Starcounter environment variables existence.
        String envVar = ComponentsCheck.CheckServerEnvVars(false, true);
        if (envVar != null)
            return true;

        // Open necessary registry keys.
        RegistryKey rkSettings = Registry.LocalMachine.OpenSubKey("SOFTWARE");

        // Checking if Starcounter settings (system-wide installation) exist in registry.
        if ((rkSettings != null) && (rkSettings.OpenSubKey(ConstantsBank.SCProductName) != null))
            return true;

        // Checking if Starcounter user exists.
        try
        {
            DirectoryEntry localMachine = new DirectoryEntry("WinNT://" + Environment.MachineName + ",computer");
            localMachine.Children.Find(SystemServerAccount.UserName);
            return true;
        }
        catch
        {
            // Need to catch an exception when the user is not found.
        }

        // Checking for Starcounter service existence.
        ServiceController[] allServices = ServiceController.GetServices();
        foreach (ServiceController someService in allServices)
        {
            if (someService.ServiceName.StartsWith(ConstantsBank.SCProductName, StringComparison.CurrentCultureIgnoreCase))
                return true;
        }

        // Checking for Starcounter server configuration file.
        String serverDir = InstallerMain.ReadServerInstallationPath(SystemServerConfigPath);
        if (Directory.Exists(serverDir))
            return true;

        // Didn't find any component footprints.
        return false;
    }

    /// <summary>
    /// Checks if component can be installed.
    /// </summary>
    /// <returns>True if can.</returns>
    public override Boolean CanBeInstalled()
    {
        return !IsInstalled();
    }

    /// <summary>
    /// Checks if component can be installed.
    /// </summary>
    /// <returns>True if can.</returns>
    public override Boolean CanBeRemoved()
    {
        return IsInstalled();
    }

    /// <summary>
    /// Determines if this component should be installed
    /// in this session.
    /// </summary>
    /// <returns>True if component should be installed.</returns>
    public override Boolean ShouldBeInstalled()
    {
        return InstallerMain.InstallationSettingCompare(ConstantsBank.Setting_InstallSystemServer, ConstantsBank.Setting_True);
    }

    /// <summary>
    /// Determines if this component should be uninstalled
    /// in this session.
    /// </summary>
    /// <returns>True if component should be uninstalled.</returns>
    public override Boolean ShouldBeRemoved()
    {
        return UninstallEngine.UninstallationSettingCompare(ConstantsBank.Setting_RemoveSystemServer, ConstantsBank.Setting_True);
    }
}
}