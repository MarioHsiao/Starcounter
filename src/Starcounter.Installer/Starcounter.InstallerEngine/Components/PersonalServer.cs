using System;
using Microsoft.Win32;
using System.IO;
using System.ServiceProcess;
using System.DirectoryServices;
using Starcounter;
using System.Diagnostics;
using Starcounter.Internal;
using Starcounter.Server.Setup;

namespace Starcounter.InstallerEngine
{
public class CPersonalServer : CComponentBase
{
    /// <summary>
    /// Component initialization.
    /// </summary>
    public CPersonalServer()
    {
    }

    /// <summary>
    /// Provides descriptive name of the components.
    /// </summary>
    public override String DescriptiveName
    {
        get
        {
            return "Starcounter Personal Server Component";
        }
    }

    /// <summary>
    /// Provides name of the component setting in INI file.
    /// </summary>
    public override String SettingName
    {
        get
        {
            return ConstantsBank.Setting_InstallPersonalServer;
        }
    }

    /// <summary>
    /// Provides installation path of the component.
    /// </summary>
    public override String ComponentPath
    {
        get
        {
            return InstallerMain.GetInstallSettingValue(ConstantsBank.Setting_PersonalServerPath);
        }
    }

    /// <summary>
    /// Kills all Starcounter server processes (except Starcounter service).
    /// </summary>
    internal static void KillServersButNotService()
    {
        if (InstallerMain.PersonalServerComponent.ShouldBeInstalled() ||
            InstallerMain.SystemServerComponent.ShouldBeInstalled())
        {
            Process[] procs = Process.GetProcessesByName(ConstantsBank.SCServerProcess);
            foreach (Process proc in procs)
            {
                // Checking if its not a service.
                if (proc.SessionId != 0)
                {
                    try
                    {
                        proc.Kill();
                        proc.WaitForExit(30000);
                    }
                    catch { }
                    finally { proc.Close(); }
                }
            }
        }
    }

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

        // Logging event.
        Utilities.ReportSetupEvent("Creating environment variables for personal database engine...");

        // Setting the default server environment variable.
        Environment.SetEnvironmentVariable(ConstantsBank.SCEnvVariableDefaultServer,
            ConstantsBank.SCPersonalDatabasesName,
            EnvironmentVariableTarget.User);

        // Logging event.
        Utilities.ReportSetupEvent("Installing personal database engine...");
        String serverPath = ComponentPath;

        // Checking that server path is in user's personal directory.
        if (!Utilities.ParentChildDirectory(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\..", serverPath))
        {
            if (!Utilities.RunningOnBuildServer())
            {
                Utilities.MessageBoxWarning("You are installing Personal Server not in user directory."
                    + " Make sure you have read/write access rights to the directory: " +
                    serverPath, "Personal server installation in non-user directory...");
            }
        }

        // Logging event.
        Utilities.ReportSetupEvent("Creating structure for personal database engine...");

        //a_m//
        /*
        // Creating personal server with specified path.
        RepositorySetup personalRepSetup = RepositorySetup.NewDefault(
            InstallerMain.InstallationBaseComponent.ComponentPath,
            serverPath,
            StarcounterEnvironment.DefaultPorts.ServerPortRangeSize);

        // Changing the name of the server explicitly.
        personalRepSetup.Structure.Name = StarcounterEnvironment.ServerNames.PersonalUser;

        UserServerSetup userServerSetup = new UserServerSetup(personalRepSetup);
        userServerSetup.Execute();
        */

        // Killing server process (in order to later start it with normal privileges).
        KillServersButNotService();

        // Updating progress.
        InstallerMain.ProgressIncrement();
    }

    // Path to personal server configuration file.
    internal static readonly String PersonalServerConfigPath =
        Path.Combine(StarcounterEnvironment.Directories.UserAppDataDirectory, "PersonalAndRemoteServers.config");

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

        // Logging event.
        Utilities.ReportSetupEvent("Removing Personal Server environment variables...");

        // Removing default server environment variable.
        Environment.SetEnvironmentVariable(ConstantsBank.SCEnvVariableDefaultServer,
            null,
            EnvironmentVariableTarget.User);

        // Logging event.
        Utilities.ReportSetupEvent("Deleting personal database server registry entries...");

        // Removing registry entries.
        RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE", true);

        // Looking if 'Starcounter' key exists and trying to delete it.
        if ((registryKey != null) && (registryKey.OpenSubKey(ConstantsBank.SCProductName, true) != null))
            registryKey.DeleteSubKeyTree(ConstantsBank.SCProductName);

        // Deleting personal server configuration folder if demanded.
        Utilities.ReportSetupEvent("Deleting personal database server configuration...");

        // Getting the server installation path.
        try
        {
            String personalServerPath = ComponentPath;

            if (Utilities.DirectoryIsNotEmpty(new DirectoryInfo(personalServerPath)))
            {
                InstallerMain.FinalSetupMessage += "You can find and manually delete the Personal Server databases directory in: '" +
                    personalServerPath + "'" + Environment.NewLine;
            }
        }
        catch { }

        // Deleting the config file.
        if (File.Exists(PersonalServerConfigPath))
            File.Delete(PersonalServerConfigPath);

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
        String envVar = ComponentsCheck.CheckServerEnvVars(true, false);
        if (envVar != null)
            return true;

        // Getting list of installed programs.
        RegistryKey rkSettings = Registry.CurrentUser.OpenSubKey("SOFTWARE");

        // Checking if Starcounter settings (personal installation) exist in registry.
        if ((rkSettings != null) && (rkSettings.OpenSubKey(ConstantsBank.SCProductName) != null))
            return true;

        // Checking for personal Starcounter server configuration file.
        if (File.Exists(PersonalServerConfigPath))
            return true;

        // None of evidence found.
        return false;
    }

    /// <summary>
    /// Checks if component can be installed.
    /// </summary>
    /// <returns>True if can.</returns>
    public override Boolean CanBeInstalled()
    {
        return (!IsInstalled());
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
        return InstallerMain.InstallSettingCompare(ConstantsBank.Setting_InstallPersonalServer, ConstantsBank.Setting_True);
    }

    /// <summary>
    /// Determines if this component should be uninstalled
    /// in this session.
    /// </summary>
    /// <returns>True if component should be uninstalled.</returns>
    public override Boolean ShouldBeRemoved()
    {
        return UninstallEngine.UninstallSettingCompare(ConstantsBank.Setting_RemovePersonalServer, ConstantsBank.Setting_True);
    }
}
}