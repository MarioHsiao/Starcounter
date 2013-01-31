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
            Process[] procs = Process.GetProcessesByName(StarcounterConstants.ProgramNames.ScService);
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

    // Desktop shortcut.
    readonly String PersonalServerDesktopShortcutPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                     ConstantsBank.SCProductName + ".lnk");

    // Start Menu shortcut.
    readonly String PersonalServerStartMenuPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                     "Programs\\" + ConstantsBank.SCProductName + "\\" + ConstantsBank.SCProductName + ".lnk");

    /// <summary>
    /// Creates a Desktop and Menu shortcuts for personal server.
    /// </summary>
    void CreatePersonalServerShortcuts()
    {
        // Checking if shortcuts should be installed.
        Boolean createShortcuts = InstallerMain.InstallSettingCompare(ConstantsBank.Setting_CreatePersonalServerShortcuts, ConstantsBank.Setting_True);

        // Checking if user wants to create desktop shortcuts.
        if (!createShortcuts)
            return;

        // Logging event.
        Utilities.ReportSetupEvent("Creating Personal Server shortcuts...");

        // Shortcut to installation directory.
        String installPath = InstallerMain.InstallationBaseComponent.ComponentPath;

        // Checking if we need to disable the activity monitor.
        String args = " ";

        // Calling external tool to create shortcut.
        Utilities.CreateShortcut(
            Path.Combine(installPath, ConstantsBank.SCPersonalServerExeName),
            PersonalServerDesktopShortcutPath,
            args,
            installPath,
            "Starts Starcounter Personal Server.");

        // Obtaining path to Start Menu for a current user.
        String startMenuDir = Path.GetDirectoryName(PersonalServerStartMenuPath);

        // Creating Start Menu directory if needed.
        if (!Directory.Exists(startMenuDir))
            Directory.CreateDirectory(startMenuDir);

        // Calling external tool to create shortcut.
        Utilities.CreateShortcut(
            Path.Combine(installPath, ConstantsBank.SCPersonalServerExeName),
            PersonalServerStartMenuPath,
            args,
            installPath,
            "Starts Starcounter Personal Server.");

        // Updating progress.
        InstallerMain.ProgressIncrement();
    }

    /// <summary>
    /// Starts personal server if demanded.
    /// </summary>
    void StartPersonalServer()
    {
        // Adding process to post-setup start.
        InstallerMain.AddProcessToPostStart(Path.Combine(InstallerMain.InstallationBaseComponent.ComponentPath, ConstantsBank.SCPersonalServerExeName), "");
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

        // Creating the repository using server functionality.
        var setup = RepositorySetup.NewDefault(serverPath, StarcounterEnvironment.ServerNames.PersonalUser);
        setup.Execute();

        // Killing server process (in order to later start it with normal privileges).
        KillServersButNotService();

        // Updating progress.
        InstallerMain.ProgressIncrement();

        // Creating shortcuts.
        CreatePersonalServerShortcuts();

        // Starts personal server if demanded.
        StartPersonalServer();
    }

    // Path to personal server configuration file.
    internal static readonly String PersonalServerConfigPath =
        Path.Combine(InstallerMain.InstallationDir, "Personal.xml");

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

        // Removing desktop shortcut.
        if (File.Exists(PersonalServerDesktopShortcutPath))
            File.Delete(PersonalServerDesktopShortcutPath);

        // Removing Start Menu shortcut.
        if (File.Exists(PersonalServerStartMenuPath))
            File.Delete(PersonalServerStartMenuPath);

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