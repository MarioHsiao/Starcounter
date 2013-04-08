using System;
using Microsoft.Win32;
using System.IO;
using System.ServiceProcess;
using System.DirectoryServices;
using Starcounter;
using System.Diagnostics;
using Starcounter.Internal;
using Starcounter.Server.Setup;
using System.Xml;
using Starcounter.Configuration;

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
    /// Provides name of the component setting.
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
            return InstallerMain.GetInstallationSettingValue(ConstantsBank.Setting_PersonalServerPath);
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
                     ConstantsBank.SCProductName + " " + StarcounterEnvironment.ServerNames.PersonalServer + " Server.lnk");

    readonly String PersonalServerAdminDesktopShortcutPath =
    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                 ConstantsBank.SCProductName + " " + StarcounterEnvironment.ServerNames.PersonalServer + " Administrator.lnk");

    // Start Menu shortcut.
    readonly String PersonalServerStartMenuPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                     "Programs\\" + ConstantsBank.SCProductName + "\\" + ConstantsBank.SCProductName + " " + StarcounterEnvironment.ServerNames.PersonalServer + " Server.lnk");

    /// <summary>
    /// Creates a Desktop and Menu shortcuts for personal server.
    /// </summary>
    void CreatePersonalServerShortcuts()
    {
        // Checking if shortcuts should be installed.
        Boolean createShortcuts = InstallerMain.InstallationSettingCompare(
            ConstantsBank.Setting_CreatePersonalServerShortcuts,
            ConstantsBank.Setting_True);

        // Checking if user wants to create desktop shortcuts.
        if (!createShortcuts)
            return;

        // Logging event.
        Utilities.ReportSetupEvent("Creating Personal Server shortcuts...");

        // Shortcut to installation directory.
        String installPath = InstallerMain.InstallationBaseComponent.ComponentPath;

        // Calling external tool to create server shortcut.
        Utilities.CreateShortcut(
            Path.Combine(installPath, ConstantsBank.SCServiceExeName),
            PersonalServerDesktopShortcutPath,
            StarcounterEnvironment.ServerNames.PersonalServer,
            installPath,
            "Starts " + ConstantsBank.SCProductName  + " Personal Server.",
            Path.Combine(InstallerMain.InstallationDir, ConstantsBank.SCIconFilename));

        // Calling external tool to create Administrator shortcut.
        Utilities.CreateShortcut(
            "http://localhost:" + InstallerMain.GetInstallationSettingValue(ConstantsBank.Setting_DefaultPersonalServerSystemHttpPort),
            PersonalServerAdminDesktopShortcutPath,
            "",
            installPath,
            "Starts " + ConstantsBank.SCProductName + " Personal Administrator.",
            Path.Combine(InstallerMain.InstallationDir, ConstantsBank.SCIconFilename));

        // Obtaining path to Start Menu for a current user.
        String startMenuDir = Path.GetDirectoryName(PersonalServerStartMenuPath);

        // Creating Start Menu directory if needed.
        if (!Directory.Exists(startMenuDir))
            Directory.CreateDirectory(startMenuDir);

        // Calling external tool to create shortcut.
        Utilities.CreateShortcut(
            Path.Combine(installPath, ConstantsBank.SCServiceExeName),
            PersonalServerStartMenuPath,
            StarcounterEnvironment.ServerNames.PersonalServer,
            installPath,
            "Starts " + ConstantsBank.SCProductName + " " + StarcounterEnvironment.ServerNames.PersonalServer + " Administrator.",
            Path.Combine(InstallerMain.InstallationDir, ConstantsBank.SCIconFilename));

        // Updating progress.
        InstallerMain.ProgressIncrement();
    }

    /// <summary>
    /// Starts personal server if demanded.
    /// </summary>
    void StartPersonalServer()
    {
        // Adding process to post-setup start.
        InstallerMain.AddProcessToPostStart(
            Path.Combine(InstallerMain.InstallationDir, ConstantsBank.SCServiceExeName),
            StarcounterEnvironment.ServerNames.PersonalServer
            );
    }

    /// <summary>
    /// Checks if server directory exists and asks user for its removal.
    /// </summary>
    /// <param name="serverDir"></param>
    public static void CheckExistingServerDirectory(String serverDir)
    {
        if (Directory.Exists(serverDir))
        {
            if (!Utilities.AskUserForDecision(
                "Server directory already exists: " + serverDir + Environment.NewLine +
                "Would you like to remove it? ALL DATA IN THIS DIRECTORY WILL BE LOST!",
                "Server directory already exists..."))
            {
                // TODO: Talk with Anders about catching this type of exception in GUI.
                //throw new InstallerAbortedException("User rejected removal of server directory: " + serverDir);
            }
            else
            {
                // Removing directory.
                Utilities.ForceDeleteDirectory(new DirectoryInfo(serverDir));
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

        // Server related directories.
        String serverDir = ComponentPath;

        // Checking for existing server directory.
        CheckExistingServerDirectory(serverDir);

        // Logging event.
        Utilities.ReportSetupEvent("Creating environment variables for personal database engine...");

        // Setting the default server environment variable.
        Environment.SetEnvironmentVariable(
            ConstantsBank.SCEnvVariableDefaultServer,
            ConstantsBank.SCPersonalDatabasesName,
            EnvironmentVariableTarget.User);

        // Logging event.
        Utilities.ReportSetupEvent("Installing personal database engine...");

        // Checking that server path is in user's personal directory.
        if (!Utilities.ParentChildDirectory(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\..", serverDir))
        {
            if (!Utilities.RunningOnBuildServer())
            {
                Utilities.MessageBoxWarning("You are installing Personal Server not in user directory."
                    + " Make sure you have read/write access rights to the directory: " +
                    serverDir, "Personal server installation in non-user directory...");
            }
        }

        // Logging event.
        Utilities.ReportSetupEvent("Creating structure for personal database engine...");

        // Creating new server repository.
        var setup = RepositorySetup.NewDefault(
            Path.Combine(serverDir, ".."),
            StarcounterEnvironment.ServerNames.PersonalServer);

        setup.Execute();

        String serverConfigPath = Path.Combine(serverDir, StarcounterEnvironment.ServerNames.PersonalServer + ServerConfiguration.FileExtension);

        // Replacing default server parameters.
        if (!Utilities.ReplaceXMLParameterInFile(
            serverConfigPath,
            StarcounterConstants.BootstrapOptionNames.DefaultUserHttpPort,
            InstallerMain.GetInstallationSettingValue(ConstantsBank.Setting_DefaultPersonalServerUserHttpPort)))
        {
            throw ErrorCode.ToException(Error.SCERRINSTALLERINTERNALPROBLEM,
                "Can't replace default Apps TCP port for " + StarcounterEnvironment.ServerNames.PersonalServer + " server.");
        }

        // Replacing default server parameters.
        if (!Utilities.ReplaceXMLParameterInFile(
            Path.Combine(serverDir, StarcounterEnvironment.ServerNames.PersonalServer + ServerConfiguration.FileExtension),
            ServerConfiguration.SystemHttpPortString,
            InstallerMain.GetInstallationSettingValue(ConstantsBank.Setting_DefaultPersonalServerSystemHttpPort)))
        {
            throw ErrorCode.ToException(Error.SCERRINSTALLERINTERNALPROBLEM,
                "Can't replace Administrator TCP port for " + StarcounterEnvironment.ServerNames.PersonalServer + " server.");
        }

        // Replacing default server parameters.
        if (!Utilities.ReplaceXMLParameterInFile(
            Path.Combine(serverDir, StarcounterEnvironment.ServerNames.PersonalServer + ServerConfiguration.FileExtension),
            StarcounterConstants.BootstrapOptionNames.SQLProcessPort,
            InstallerMain.GetInstallationSettingValue(ConstantsBank.Setting_DefaultPersonalPrologSqlProcessPort)))
        {
            throw ErrorCode.ToException(Error.SCERRINSTALLERINTERNALPROBLEM,
                "Can't replace Prolog SQL TCP port for " + StarcounterEnvironment.ServerNames.PersonalServer + " server.");
        }

        // Creating server config.
        InstallerMain.CreateServerConfig(
            StarcounterEnvironment.ServerNames.PersonalServer,
            ComponentPath,
            PersonalServerConfigPath);

        // Copying gateway configuration.
        InstallerMain.CopyGatewayConfig(
            serverDir,
            InstallerMain.GetInstallationSettingValue(ConstantsBank.Setting_DefaultPersonalServerSystemHttpPort));

        // Killing server process (in order to later start it with normal privileges).
        KillServersButNotService();

        // Creating shortcuts.
        CreatePersonalServerShortcuts();

        // Creating Administrator database.
        // TODO: Recover if in need of a database for Administrator.
        /*
        InstallerMain.CreateDatabaseSynchronous(
            StarcounterEnvironment.ServerNames.PersonalServer,
            ComponentPath,
            ConstantsBank.SCAdminDatabaseName);
        */

        // Starts personal server if demanded.
        StartPersonalServer();

        // Updating progress.
        InstallerMain.ProgressIncrement();

    }

    // Path to server configuration file.
    String personalServerConfigPath_ = null;
    String PersonalServerConfigPath
    {
        get
        {
            if (personalServerConfigPath_ != null)
                return personalServerConfigPath_;

            if (InstallerMain.InstallationDir == null)
                return null;

            personalServerConfigPath_ = Path.Combine(InstallerMain.InstallationDir, StarcounterEnvironment.ServerNames.PersonalServer + ".xml");

            return personalServerConfigPath_;
        }
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

        // Logging event.
        Utilities.ReportSetupEvent("Removing Personal Server environment variables...");

        // Removing default server environment variable.
        Environment.SetEnvironmentVariable(
            ConstantsBank.SCEnvVariableDefaultServer,
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

        // Removing desktop shortcut.
        if (File.Exists(PersonalServerAdminDesktopShortcutPath))
            File.Delete(PersonalServerAdminDesktopShortcutPath);

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

        // Checking for Starcounter server configuration file.
        String serverDir = InstallerMain.ReadServerInstallationPath(PersonalServerConfigPath);
        if (Directory.Exists(serverDir))
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
        return InstallerMain.InstallationSettingCompare(ConstantsBank.Setting_InstallPersonalServer, ConstantsBank.Setting_True);
    }

    /// <summary>
    /// Determines if this component should be uninstalled
    /// in this session.
    /// </summary>
    /// <returns>True if component should be uninstalled.</returns>
    public override Boolean ShouldBeRemoved()
    {
        return UninstallEngine.UninstallationSettingCompare(ConstantsBank.Setting_RemovePersonalServer, ConstantsBank.Setting_True);
    }
}
}