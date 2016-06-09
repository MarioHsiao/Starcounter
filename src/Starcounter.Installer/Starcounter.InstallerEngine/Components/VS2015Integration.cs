
using Starcounter.InstallerEngine.VsSetup;
using System;
using System.IO;

namespace Starcounter.InstallerEngine {

    public class VS2015Integration : VSIntegration {
        /// <inheritdoc/>
        public override string BuildNumber {
            get { return VisualStudioVersion.VS2015.BuildNumber; }
        }

        /// <inheritdoc/>
        public override String DescriptiveName {
            get {
                return "Starcounter Visual Studio 2015 Integration Component";
            }
        }

        /// <inheritdoc/>
        public override String SettingName {
            get {
                return ConstantsBank.Setting_InstallVS2015Integration;
            }
        }

        /// <inheritdoc/>
        public override void Install() {
            // Logging event.
            Utilities.ReportSetupEvent("Evaluating if Visual Studio 2015 integration should and can be installed...");

            // Checking if component should be installed in this session.
            if (!ShouldBeInstalled())
                return;

            // Checking that component is not already installed.
            if (!CanBeInstalled())
                return;

            // Logging event and some information about what paths we use
            // and if they exist
            var vsIDEDir = ConstantsBank.VS2015IDEDirectory;
            Utilities.ReportSetupEvent("Installing Microsoft Visual Studio 2015 Starcounter integration...");
            Utilities.LogMessage(
                string.Format("Using VS IDE installation directory \"{0}\" (Exist: {1}).",
                vsIDEDir,
                Directory.Exists(vsIDEDir) ? bool.TrueString : bool.FalseString)
                );

            // Check if Visual Studio is running.
            CheckVStudioRunning();

            String installPath = InstallerMain.InstallationBaseComponent.ComponentPath;

            // Running Visual Studio setup (which includes installation of templates).
            VSInstaller.InstallVs2015(installPath.TrimEnd(new char[] { '\\' }));

            // Checking that Visual Studio has stopped working.
            WaitVStudioToFinish();

            // Updating progress.
            InstallerMain.ProgressIncrement();
        }

        /// <inheritdoc/>
        public override void Uninstall() {
            if (!UninstallEngine.CompleteCleanupSetting) {
                if (UninstallEngine.RollbackSetting) {
                    // Checking if component was installed in this session.
                    if (!ShouldBeInstalled())
                        return;
                } else // Standard removal.
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

            // Logging event and some information about what paths we use
            // and if they exist
            var vsIDEDir = ConstantsBank.VS2015IDEDirectory;
            Utilities.ReportSetupEvent("Deleting Visual Studio 2015 Starcounter integration...");
            Utilities.LogMessage(
                string.Format("Using VS IDE installation directory \"{0}\" (Exist: {1}).",
                vsIDEDir,
                Directory.Exists(vsIDEDir) ? bool.TrueString : bool.FalseString)
                );

            // Deleting Starcounter Visual Studio 2015 integration and templates on demand.
            // Checking if Visual Studio is running (that can lock certain libraries like MSBuild.dll).
            CheckVStudioRunning();

            try {
                // Running Visual Studio setup (which includes uninstallation of templates).
                VSInstaller.UninstallVs2015(InstallerMain.InstallationDir.TrimEnd(new char[] { '\\' }));
            } catch (Exception ex) {
                uint errorCode;
                bool ignoreException;

                ignoreException = false;

                if (ErrorCode.TryGetCode(ex, out errorCode)) {
                    if (errorCode == Error.SCERRVSIXENGINENOTFOUND) {
                        // When uninstalling, we can't trust any state at all,
                        // not even that Visual Studio is installed. We just let
                        // this error slip after logging a notice that it
                        // occurred.
                        Utilities.LogMessage(string.Format("Notice: {0}", ex.Message));
                        ignoreException = true;
                    }
                }

                // The only condition we let pass is VS not being installed.

                if (!ignoreException)
                    throw;
            }

            // Updating progress.
            InstallerMain.ProgressIncrement();
        }

        /// <inheritdoc/>
        public override Boolean IsInstalled() {

            return IsComponentInstalled();

        }

        public static bool IsComponentInstalled() {
            if (!DependenciesCheck.VStudio2015Installed())
                return false;

            var manifest = VSIXUtilities.FindManifestFile(
               GetUserExtensionsRootFolder(VisualStudioVersion.VS2015),
               VSIXPackageInfo.VS2015.ExtensionIdentity);

            return manifest != null;
        }

        /// <inheritdoc/>
        public override Boolean CanBeInstalled() {

            if (!DependenciesCheck.VStudio2015Installed()) {
               throw ErrorCode.ToException(Error.SCERRINSTALLERVS2012NOTFOUND,
                   "Microsoft Visual Studio 2015 is required to install the Starcounter integration.");
            }

            return true;
        }

        /// <inheritdoc/>
        public override Boolean CanBeRemoved() {
            return IsInstalled();
        }

        /// <inheritdoc/>
        public override Boolean ShouldBeInstalled() {
            return InstallerMain.InstallationSettingCompare(ConstantsBank.Setting_InstallVS2015Integration, ConstantsBank.Setting_True);
        }

        /// <inheritdoc/>
        public override Boolean ShouldBeRemoved() {
            return UninstallEngine.UninstallationSettingCompare(ConstantsBank.Setting_RemoveVS2015Integration, ConstantsBank.Setting_True);
        }
    }
}
