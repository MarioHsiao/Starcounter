using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using System.Reflection;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Security.Principal;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Runtime.CompilerServices;
using Starcounter.Controls;
using System.Xml;

namespace Starcounter.InstallerWPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        const String ScVersion = "2.0.0.0";
        const String StarcounterBin = "StarcounterBin";
        const String ScInstallerGUI = "Starcounter-Setup";

        /// <summary>
        /// Returns the directory path where Starcounter is installed,
        /// obtained from environment variables.
        /// </summary>
        String GetInstalledDirFromEnv()
        {
            // First checking the user-wide installation directory.
            String scInstDir = Environment.GetEnvironmentVariable(StarcounterBin, EnvironmentVariableTarget.User);

            if (scInstDir != null)
                return scInstDir;

            // Then checking the system-wide installation directory.
            scInstDir = Environment.GetEnvironmentVariable(StarcounterBin, EnvironmentVariableTarget.Machine);

            return scInstDir;
        }

        /// <summary>
        /// Compares installed and current running Starcounter versions.
        /// Returns installed version string if versions are different.
        /// If versions are the same returns NULL.
        /// </summary>
        String CompareScVersions()
        {
            // Setting version to default value.
            String scVersion = "unknown";

            // Reading INSTALLED Starcounter version XML file.
            String installedVersion = null;
            String installDir = GetInstalledDirFromEnv();
            if (installDir != null)
            {
                XmlDocument versionXML = new XmlDocument();
                String versionInfoFilePath = Path.Combine(installDir, "VersionInfo.xml");

                // Checking that version file exists and loading it.
                try
                {
                    versionXML.Load(versionInfoFilePath);

                    // NOTE: We are getting only first element.
                    installedVersion = (versionXML.GetElementsByTagName("Version"))[0].InnerText;
                }
                catch { }
            }

            // Reading CURRENT embedded Starcounter version XML file.
            String currentVersion = ScVersion;

            // Checking if versions are not the same.
            if ((installedVersion != null) && (installedVersion != currentVersion))
                return installedVersion;

            // Setting version value to embedded version value.
            scVersion = currentVersion;
            return null;
        }

        /// <summary>
        /// Checks if another version of Starcounter is installed.
        /// </summary>
        /// <returns></returns>
        Boolean IsAnotherVersionInstalled()
        {
            // Compares installation versions.
            String previousVersion = CompareScVersions();
            if (previousVersion != null)
            {
                // IMPORTANT: Since StarcounterBin can potentially be used
                // in this installer we have to delete it for this process.
                Environment.SetEnvironmentVariable(StarcounterBin, null);

                MessageBoxResult userChoice = System.Windows.MessageBox.Show(
                    "Would you like to uninstall previous(" + previousVersion + ") version of Starcounter now?",
                    "Starcounter is already installed...",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (userChoice == MessageBoxResult.Yes)
                {
                    // Asking to launch previous version uninstaller.
                    String installDir = GetInstalledDirFromEnv();

                    // Trying "Starcounter-[Version]-Setup.exe".
                    String prevSetupExeName = "Starcounter-" + previousVersion + "-Setup.exe";
                    String prevSetupExePath = Path.Combine(installDir, prevSetupExeName);
                    if (!File.Exists(prevSetupExePath))
                    {
                        // Trying "Starcounter-Setup.exe".
                        prevSetupExeName = ScInstallerGUI + ".exe";
                        prevSetupExePath = Path.Combine(installDir, prevSetupExeName);
                        if (!File.Exists(prevSetupExePath))
                        {
                            System.Windows.MessageBox.Show(
                                "Can't find " + prevSetupExeName + " for Starcounter " + previousVersion +
                                " in '" + installDir + "'. Please uninstall previous version of Starcounter manually.");

                            return true;
                        }
                    }

                    Process prevSetupProcess = new Process();
                    prevSetupProcess.StartInfo.FileName = prevSetupExePath;
                    prevSetupProcess.StartInfo.Arguments = "DontCheckOtherInstances";
                    prevSetupProcess.Start();

                    // Waiting until previous installer finishes its work.
                    prevSetupProcess.WaitForExit();

                    // Checking version once again.
                    previousVersion = CompareScVersions();

                    // IMPORTANT: Since PATH env var still contains path to old installation directory
                    // we have to reset it for this process as well, once uninstallation is complete.
                    String pathUserEnvVar = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);
                    Environment.SetEnvironmentVariable("PATH", pathUserEnvVar);

                    // No more old installation - just continue the new one.
                    if (null == previousVersion)
                        return false;
                }

                System.Windows.MessageBox.Show(
                    "Please uninstall previous(" + previousVersion + ") version of Starcounter before installing this one.",
                    "Starcounter is already installed...");

                return true;
            }

            return false;
        }

        public App()
        {
            //System.Diagnostics.Debugger.Launch();

            // Checking if another Starcounter version is installed.
            // NOTE: Environment.Exit is used on purpose here, not just "return";
            if (IsAnotherVersionInstalled())
                Environment.Exit(0);

            // Showing main setup window.
            InitializationWindow window = new InitializationWindow();
            window.Show();
        }
    }
}