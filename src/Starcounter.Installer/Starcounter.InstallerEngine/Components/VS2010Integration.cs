using System;
using Starcounter.InstallerEngine.VsSetup;
using Microsoft.Win32;
using Starcounter;

namespace Starcounter.InstallerEngine
{
public class CVS2010Integration : VSIntegration
{
    DevEnv devEnv;

    /// <summary>
    /// Component initialization.
    /// </summary>
    public CVS2010Integration()
    {
        devEnv = new DevEnv(VisualStudioVersion.VS2010);
    }

    /// <inheritdoc/>
    public override string BuildNumber
    {
        get { return VisualStudioVersion.VS2010.BuildNumber; }
    }

    /// <summary>
    /// Provides descriptive name of the components.
    /// </summary>
    public override String DescriptiveName
    {
        get
        {
            return "Starcounter Visual Studio 2010 Integration Component";
        }
    }

    /// <summary>
    /// Provides name of the component setting.
    /// </summary>
    public override String SettingName
    {
        get
        {
            return ConstantsBank.Setting_InstallVS2010Integration;
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
        Utilities.ReportSetupEvent("Installing Microsoft Visual Studio 2010 Starcounter integration...");

        // Check if Visual Studio is running.
        CheckVStudioRunning();

        // Cleaning up previous installations if any.
        devEnv.InstallTemplates(false);

        String installPath = InstallerMain.InstallationBaseComponent.ComponentPath;

        // Running Visual Studio setup (which includes installation of templates).
        VSInstaller.InstallVs2010(installPath.TrimEnd(new char[] { '\\' }));

        // Checking that Visual Studio has stopped working.
        WaitVStudioToFinish();

        // Putting demo as a start component at the end of installation.
        CSamplesDemos.StartDemoInVs();

        // Updating progress.
        InstallerMain.ProgressIncrement();
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
                // TODO: Sometimes fails by some reason.
                if (!CanBeRemoved())
                    return;
            }
        }

        // Checking if we are running on the build server.
        if (Utilities.RunningOnBuildServer())
            return;

        // Logging event.
        Utilities.ReportSetupEvent("Deleting Visual Studio 2010 Starcounter integration...");

        // Deleting Starcounter Visual Studio 2010 integration and templates on demand.
        // Checking if Visual Studio is running (that can lock certain libraries like MSBuild.dll).
        CheckVStudioRunning();

        try
        {
            // Running Visual Studio setup (which includes uninstallation of templates).
            VSInstaller.UninstallVs2010(InstallerMain.InstallationDir.TrimEnd(new char[] { '\\' }));
        }
        catch (Exception ex)
        {
            uint errorCode;
            bool ignoreException;

            ignoreException = false;

            if (ErrorCode.TryGetCode(ex, out errorCode))
            {
                if (errorCode == Error.SCERRVSIXENGINENOTFOUND && UninstallEngine.CompleteCleanupSetting)
                {
                    // If we are doing a full cleanup, we can't trust any state at all,
                    // not even that Visual Studio is installed. We just let this error
                    // slip.

                    ignoreException = true;
                }
            }

            // The only condition we let pass is VS not being installed.

            if (!ignoreException)
                throw;
        }
        finally
        {
            // Cleaning up previous installations if any.
            devEnv.InstallTemplates(false);
        }

        // Updating progress.
        InstallerMain.ProgressIncrement();
    }

    /// <summary>
    /// Checks if components is already installed.
    /// </summary>
    /// <returns>True if already installed.</returns>
    public override Boolean IsInstalled()
    {
        // Check registry value indicating the VS 2010 integration is actually
        // installed for the current user.

        RegistryKey installedProductKey = Registry.CurrentUser.OpenSubKey(ConstantsBank.RegistryVS2010StarcounterInstalledProductKey);
        if (installedProductKey == null)
            return false;

        installedProductKey.Close();
        return true;
    }

    /// <summary>
    /// Checks if component can be installed.
    /// </summary>
    /// <returns>True if can.</returns>
    public override Boolean CanBeInstalled()
    {
        if (!DependenciesCheck.VStudio2010Installed())
        {
            throw ErrorCode.ToException(Error.SCERRINSTALLERVS2010NOTFOUND,
                "Microsoft Visual Studio 2010 is required to install the Starcounter integration.");
        }

        return true;
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
        return InstallerMain.InstallationSettingCompare(ConstantsBank.Setting_InstallVS2010Integration, ConstantsBank.Setting_True);
    }

    /// <summary>
    /// Determines if this component should be uninstalled
    /// in this session.
    /// </summary>
    /// <returns>True if component should be uninstalled.</returns>
    public override Boolean ShouldBeRemoved()
    {
        return UninstallEngine.UninstallationSettingCompare(ConstantsBank.Setting_RemoveVS2010Integration, ConstantsBank.Setting_True);
    }
}
}