using System;
using Microsoft.Win32;
using System.IO;
using System.ServiceProcess;
using System.DirectoryServices;
using System.Diagnostics;
using Starcounter;
using Starcounter.Internal;

namespace Starcounter.InstallerEngine
{
public class CAdministrator : CComponentBase
{
    /// <summary>
    /// Component initialization.
    /// </summary>
    public CAdministrator()
    {
    }

    /// <summary>
    /// Provides descriptive name of the components.
    /// </summary>
    public override String DescriptiveName
    {
        get
        {
            return "Starcounter Administrator Component";
        }
    }

    /// <summary>
    /// Provides name of the component setting in INI file.
    /// </summary>
    public override String SettingName
    {
        get
        {
            return ConstantsBank.Setting_InstallAdministrator;
        }
    }

    /// <summary>
    /// Starts Starcounter Administrator utility if demanded.
    /// </summary>
    void StartStartcounterAdministrator()
    {
        // Checking for Administrator executable presence.
        String adminExeFullPath = Path.Combine(InstallerMain.InstallationBaseComponent.ComponentPath, ConstantsBank.SCAdministratorName);
        if (!File.Exists(adminExeFullPath))
            return;

        // Creating start parameters for the process.
        String procArgs = "";
        if (!InstallerMain.ActivityMonitorComponent.ShouldBeInstalled())
            procArgs = ConstantsBank.SCNoActMonFlag;

        // Adding process to post-setup start.
        InstallerMain.AddProcessToPostStart(adminExeFullPath, procArgs);
    }

    // Desktop shortcut.
    readonly String AdminDesktopShortcutPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                     ConstantsBank.SCProductName + " Administrator.lnk");

    // Start Menu shortcut.
    readonly String AdminStartMenuPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                     "Programs\\" + ConstantsBank.SCProductName + "\\" + ConstantsBank.SCProductName + " Administrator.lnk");

    /// <summary>
    /// Creates a Desktop and Menu shortcuts for Starcounter Administrator.
    /// </summary>
    void CreateAdministratorShortcuts()
    {
        // Checking if shortcuts should be installed.
        Boolean createShortcuts = InstallerMain.InstallSettingCompare(ConstantsBank.Setting_CreateAdministratorShortcuts, ConstantsBank.Setting_True);

        // Checking if user wants to create desktop shortcuts.
        if (!createShortcuts)
            return;

        // Logging event.
        Utilities.ReportSetupEvent("Creating Administrator shortcuts...");

        // Shortcut to installation directory.
        String installPath = InstallerMain.InstallationBaseComponent.ComponentPath;

        // Checking if we need to disable the activity monitor.
        String args = " ";
        if (!InstallerMain.ActivityMonitorComponent.ShouldBeInstalled())
            args = ConstantsBank.SCNoActMonFlag;

        // Calling external tool to create shortcut.
        Utilities.CreateShortcut(
            Path.Combine(installPath, ConstantsBank.SCAdministratorName),
            AdminDesktopShortcutPath,
            args,
            installPath,
            "Starts Starcounter Administrator.");

        // Obtaining path to Start Menu for a current user.
        String startMenuDir = Path.GetDirectoryName(AdminStartMenuPath);

        // Creating Start Menu directory if needed.
        if (!Directory.Exists(startMenuDir))
            Directory.CreateDirectory(startMenuDir);

        // Calling external tool to create shortcut.
        Utilities.CreateShortcut(
            Path.Combine(installPath, ConstantsBank.SCAdministratorName),
            AdminStartMenuPath,
            args,
            installPath,
            "Starts Starcounter Administrator.");

        // Updating progress.
        InstallerMain.ProgressIncrement();
    }

    /// <summary>
    /// Installs component.
    /// </summary>
    public override void Install()
    {
        // Checking if component should be installed in this session.
        if (!ShouldBeInstalled())
            return;

        // Logging event.
        Utilities.ReportSetupEvent("Installing Starcounter Administrator...");

        // Checking that component is not already installed.
        if (CanBeInstalled())
        {
            // Installing (actually recovering) Administrator component.
            String adminExeFullPath = Path.Combine(InstallerMain.InstallationBaseComponent.ComponentPath,
                                                   ConstantsBank.SCAdministratorName);
            if (!File.Exists(adminExeFullPath))
            {
                String adminRemovedExeFullPath = Path.Combine(InstallerMain.InstallationBaseComponent.ComponentPath,
                                                              ConstantsBank.SCRemovedAdminName);
                if (File.Exists(adminRemovedExeFullPath))
                {
                    // Renaming the file so its not found and recognized as deleted.
                    File.Move(adminRemovedExeFullPath, adminExeFullPath);
                }
            }
        }

        // Updating progress.
        InstallerMain.ProgressIncrement();

        // Creating Administrator shortcuts.
        CreateAdministratorShortcuts();

        // Starting after installation.
        StartStartcounterAdministrator();
    }

    /// <summary>
    /// Renames Starcounter administrator.
    /// </summary>
    void RenameAdministrator()
    {
        String adminExeFullPath = Path.Combine(InstallerMain.InstallationBaseComponent.ComponentPath,
                                               ConstantsBank.SCAdministratorName);
        if (File.Exists(adminExeFullPath))
        {
            String adminRemovedExeFullPath = Path.Combine(InstallerMain.InstallationBaseComponent.ComponentPath,
                                                          ConstantsBank.SCRemovedAdminName);
            if (File.Exists(adminRemovedExeFullPath))
            {
                // Removed Administrator file already exists so skipping renaming it.
                File.Delete(adminExeFullPath);
            }
            else
            {
                // Renaming the file so its not found and recognized as deleted.
                File.Move(adminExeFullPath, adminRemovedExeFullPath);
            }
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
        Utilities.ReportSetupEvent("Deleting Starcounter Administrator component...");

        // Renaming Administrator first.
        RenameAdministrator();

        // Removing desktop shortcut.
        if (File.Exists(AdminDesktopShortcutPath))
            File.Delete(AdminDesktopShortcutPath);

        // Removing Start Menu shortcut.
        if (File.Exists(AdminStartMenuPath))
            File.Delete(AdminStartMenuPath);

        // Deleting all Administrator related files.
        String[] adminRelatedFiles = { @".*Administrator.*" };
        Utilities.ForceDeleteDirectoryEntryPatterns(new DirectoryInfo(StarcounterEnvironment.Directories.UserAppDataDirectory),
            adminRelatedFiles,
            true);

        // Updating progress.
        InstallerMain.ProgressIncrement();
    }

    /// <summary>
    /// Checks if components is already installed.
    /// </summary>
    /// <returns>True if already installed.</returns>
    public override Boolean IsInstalled()
    {
        String scInstallPath = CInstallationBase.GetInstalledDirFromEnv();
        if (scInstallPath != null)
        {
            // Checking that Administrator binaries exist in installation directory.
            String[] binariesToCheck = { @"^StarcounterAdministrator\.exe$" };

            if (Utilities.DirectoryContainsFilesRegex(scInstallPath, binariesToCheck))
                return true;
        }

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
        return InstallerMain.InstallSettingCompare(ConstantsBank.Setting_InstallAdministrator, ConstantsBank.Setting_True);
    }

    /// <summary>
    /// Determines if this component should be uninstalled
    /// in this session.
    /// </summary>
    /// <returns>True if component should be uninstalled.</returns>
    public override Boolean ShouldBeRemoved()
    {
        return UninstallEngine.UninstallSettingCompare(ConstantsBank.Setting_RemoveAdministrator, ConstantsBank.Setting_True);
    }
}
}