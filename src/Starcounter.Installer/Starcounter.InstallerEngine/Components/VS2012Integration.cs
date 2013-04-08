
using System;
using Starcounter.InstallerEngine.VsSetup;
using Microsoft.Win32;
using Starcounter;
using System.Diagnostics;

namespace Starcounter.InstallerEngine
{
    public class VS2012Integration : VSIntegration
    {
        DevEnv devEnv;

        public VS2012Integration()
        {
            devEnv = new DevEnv(VisualStudioVersion.VS2012);
        }

        /// <inheritdoc/>
        public override string BuildNumber
        {
            get { return VisualStudioVersion.VS2012.BuildNumber; }
        }

        /// <inheritdoc/>
        public override String DescriptiveName
        {
            get
            {
                return "Starcounter Visual Studio 2012 Integration Component";
            }
        }

        /// <inheritdoc/>
        public override String SettingName
        {
            get
            {
                return ConstantsBank.Setting_InstallVS2012Integration;
            }
        }

        /// <summary>
        /// Installs component.
        /// </summary>
        public override void Install()
        {
            // Logging event.
            Utilities.ReportSetupEvent("Evaluating if Visual Studio 2012 integration should and can be installed...");

            // Checking if component should be installed in this session.
            if (!ShouldBeInstalled())
                return;

            // Checking that component is not already installed.
            if (!CanBeInstalled())
                return;

            // Logging event.
            Utilities.ReportSetupEvent("Installing Microsoft Visual Studio 2012 Starcounter integration...");

            // Check if Visual Studio is running.
            CheckVStudioRunning();

            // Starting visual studio once before installation.
            devEnv.InstallTemplates(false);

            String installPath = InstallerMain.InstallationBaseComponent.ComponentPath;

            // Running Visual Studio setup (which includes installation of templates).
            VSInstaller.InstallVs2012(installPath.TrimEnd(new char[] { '\\' }));

            // Checking that Visual Studio has stopped working.
            WaitVStudioToFinish();

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
            Utilities.ReportSetupEvent("Deleting Visual Studio 2012 Starcounter integration...");

            // Deleting Starcounter Visual Studio 2012 integration and templates on demand.
            // Checking if Visual Studio is running (that can lock certain libraries like MSBuild.dll).
            CheckVStudioRunning();

            try
            {
                // Running Visual Studio setup (which includes uninstallation of templates).
                VSInstaller.UninstallVs2012(InstallerMain.InstallationDir.TrimEnd(new char[] { '\\' }));
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
                // Starting visual studio once before installation.
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
            // Check registry value indicating the VS 2012 integration is actually
            // installed for the current user.

            RegistryKey installedProductKey = Registry.CurrentUser.OpenSubKey(ConstantsBank.RegistryVS2012StarcounterInstalledProductKey);
            if (installedProductKey == null)
            {
                //Debugger.Launch();
                return false;
            }

            installedProductKey.Close();
            return true;
        }

        /// <summary>
        /// Checks if component can be installed.
        /// </summary>
        /// <returns>True if can.</returns>
        public override Boolean CanBeInstalled()
        {
            if (!DependenciesCheck.VStudio2012Installed())
            {
                throw ErrorCode.ToException(Error.SCERRINSTALLERVS2012NOTFOUND,
                    "Microsoft Visual Studio 2012 is required to install the Starcounter integration.");
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
            return InstallerMain.InstallationSettingCompare(ConstantsBank.Setting_InstallVS2012Integration, ConstantsBank.Setting_True);
        }

        /// <summary>
        /// Determines if this component should be uninstalled
        /// in this session.
        /// </summary>
        /// <returns>True if component should be uninstalled.</returns>
        public override Boolean ShouldBeRemoved()
        {
            return UninstallEngine.UninstallationSettingCompare(ConstantsBank.Setting_RemoveVS2012Integration, ConstantsBank.Setting_True);
        }
    }
}
