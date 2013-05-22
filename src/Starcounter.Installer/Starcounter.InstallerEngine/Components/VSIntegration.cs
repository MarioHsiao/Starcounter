
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Starcounter.InstallerEngine.VsSetup;
using Microsoft.Win32;
using System.IO;
using Starcounter;

namespace Starcounter.InstallerEngine
{
    /// <summary>
    /// Base class for Visual Studio integration components.
    /// </summary>
    public abstract class VSIntegration : CComponentBase
    {
        /// <summary>
        /// Gets a list of Visual Studio editions we support integration with and
        /// that are installed on the current machine.
        /// </summary>
        /// <remarks>
        /// Implementation designed as suggested by this article:
        /// http://www.mztools.com/articles/2008/MZ2008003.aspx
        /// </remarks>
        /// <returns></returns>
        public static List<VisualStudioEdition> GetInstalledVSEditionsSupported()
        {
            List<VisualStudioEdition> installedEditions = new List<VisualStudioEdition>();
            VisualStudioVersion[] supportedVersions = new VisualStudioVersion[]
            {
                VisualStudioVersion.VS2010,
                VisualStudioVersion.VS2012
            };

            // No matter the OS, and despite us being a 64-bit application, we request
            // the 32-bit view of the registry, since this is where Visual Studio keep
            // it's settings (still being a 32-bit application).

            using (var localMachine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
            {
                foreach (var version in supportedVersions)
                {
                    using (var versionKey = localMachine.OpenSubKey(string.Format(@"SOFTWARE\Microsoft\VisualStudio\{0}", version.BuildNumber)))
                    {
                        if (versionKey == null)
                            continue;

                        using (var setupKey = versionKey.OpenSubKey(@"Setup\VS"))
                        {
                            if (setupKey == null)
                                continue;

                            foreach (var namedEdition in VisualStudioEdition.NamedEditions)
                            {
                                using (var editionKey = setupKey.OpenSubKey(namedEdition))
                                {
                                    if (editionKey == null)
                                        continue;

                                    // We have located an edition of a visual studio we support.
                                    // If we can resolve it's installation directory, we'll add
                                    // it to our list of supported editions we must consider
                                    // during installation.

                                    // Get the installation directory either from the specific
                                    // edition key, or from the embracing setup key, whichever
                                    // we find first.

                                    string installationDirectory = editionKey.GetValue("ProductDir") as string;
                                    if (string.IsNullOrEmpty(installationDirectory))
                                        installationDirectory = setupKey.GetValue("ProductDir") as string;

                                    if (!string.IsNullOrEmpty(installationDirectory))
                                    {
                                        var edition = new VisualStudioEdition();
                                        edition.Version = version;
                                        edition.Name = namedEdition;
                                        edition.InstallationDirectory = installationDirectory;
                                        installedEditions.Add(edition);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return installedEditions;
        }

        /// <summary>
        /// Gets the build number of the target Visual Studio version this components
        /// installs into, e.g. "10.0" for Visual Studio 2010.
        /// </summary>
        public abstract string BuildNumber { get; }

        /// <summary>
        /// Gets the full path to the devenv.exe representing the Visual Studio
        /// version this component target.
        /// </summary>
        string DevEnvPath
        {
            get
            {
                return Path.GetFullPath(ConstantsBank.VS2012DevEnvPath);
            }
        }

        /// <summary>
        /// Check if Microsoft Visual Studio is running.
        /// </summary>
        protected void CheckVStudioRunning()
        {
            // Checking if Visual Studio is running (that can lock certain libraries like MSBuild.dll).
            while (IsVSWithThisBuildRunning())
            {
                String vsRunning = "One or more instances of Microsoft Visual Studio (devenv.exe) are running. Please shut them down and press OK.";
                if (InstallerMain.SilentFlag)
                {
                    // Printing a console message.
                    Utilities.ConsoleMessage(vsRunning);

                    throw ErrorCode.ToException(Error.SCERRINSTALLERVSTUDIOISRUNNING);
                }
                else
                {
                    Utilities.MessageBoxInfo(vsRunning, "Microsoft Visual Studio is running...");
                }

                Thread.Sleep(5000);
            }
        }

        /// <summary>
        /// Wait until Microsoft Visual Studio has finished its work.
        /// </summary>
        protected void WaitVStudioToFinish()
        {
            while (IsVSWithThisBuildRunning())
            {
                Thread.Sleep(1000);
            }
        }

        /// <summary>
        /// Returns a value indicating if any devenv.exe instance from the Visual
        /// Studio version this component targets is running.
        /// </summary>
        /// <returns>True if at least one such instance is running. False if not.</returns>
        bool IsVSWithThisBuildRunning()
        {
            foreach (var runningDevEnvProcess in Process.GetProcessesByName("devenv"))
            {
                if (string.Equals(runningDevEnvProcess.MainModule.FileName, this.DevEnvPath, StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}