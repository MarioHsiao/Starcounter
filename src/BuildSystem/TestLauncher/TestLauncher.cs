using System;
using System.IO;
using System.Diagnostics;
using System.Threading;
using BuildSystemHelper;
using System.Reflection;

namespace TestLauncher
{
    class TestLauncher
    {
        static readonly String InstallPath = Path.GetFullPath(Environment.GetEnvironmentVariable(BuildSystem.CheckOutDirEnvVar) + "\\ScInstallation");
        static Boolean InstallVsPlugin = true;

        static TextWriter ErrOut = Console.Error;
        static TextWriter StdOut = Console.Out;

        // Points to consolidated binaries directory.
        static String ConsolidatedDir = "";

        static readonly String SetupSettings =
@"
; This file contains settings obtained from Starcounter Installer GUI.
[Starcounter Installation Settings]

; NOTE: Starcounter installation directory is the same from which you
; start the installer engine (in GUI you specify the target installation
; directory where Starcounter files will be copied).
; The uninstall process will never delete your databases.
; However, you still can remove them manually by deleting server
; directories described below.

; Determines what type(s) of installation should be performed.

; Personal (for the Current User) Starcounter server installation with
; private database server folder.
InstallPersonalServer = True

; System type of Starcounter server installation, i.e. machine wide, where
; Starcounter server is started upon system startup
; as Microsoft Windows service.
InstallSystemServer = False

; Specifies the database server path for each installed Starcounter server
; (only selected types of installation will use the corresponding values).
PersonalServerPath = " + InstallPath + @"\Personal
SystemServerPath   = " + InstallPath + @"\System

; True if Starcounter Visual Studio 2010 developer
; integration should be installed.
InstallVS2010Integration = False" +

@"; True if Starcounter Visual Studio 2012 developer
; integration should be installed.
InstallVS2012Integration = " + InstallVsPlugin +

@"; Indicates if sample database 'MyMusic' should be installed.
InstallSampleDatabase = True

; The following options are most likely will always be 'True':

; True if Starcounter Activity Monitor extension should be installed.
InstallActivityMonitor = False

; True if Starcounter Administrator should be installed.
InstallAdministrator = True

; True if Starcounter Administrator shortcuts should be created (Desktop and Start Menu).
CreateAdministratorShortcuts = True

; True if Starcounter shortcuts should be added to Start Menu.
AddStarcounterToStartMenu = True

[Starcounter Uninstall Settings]

; True if Starcounter Personal Server for current user should be uninstalled.
RemovePersonalServer = True

; True if Starcounter System Server should be uninstalled.
RemoveSystemServer = True

; True if Starcounter Visual Studio 2010 integration should be removed.
RemoveVS2010Integration = False

; True if Starcounter Visual Studio 2012 integration should be removed.
RemoveVS2012Integration = True

; True if Starcounter Administrator should be removed.
RemoveAdministrator = True
";

        static Int32 RunInstaller(Boolean uninstall, Boolean cleanup, String workingDir)
        {
            // Overwriting existing setup settings with new settings.
            File.WriteAllText(workingDir + "\\SetupSettings.ini", SetupSettings);

            ProcessStartInfo pInfo = new ProcessStartInfo();

            // Installer process info.
            pInfo.FileName = "\"" + workingDir + "\\Starcounter-Setup.exe\"";
            pInfo.Arguments = "--silent";
            pInfo.UseShellExecute = false;
            pInfo.RedirectStandardError = true;

            String uninstallMode = "";
            if (uninstall)
                uninstallMode = " --uninstall";
            if (cleanup)
                uninstallMode = " --cleanup";

            pInfo.Arguments += uninstallMode;

            pInfo.WorkingDirectory = workingDir;

            // Start the Installer.
            Process p = Process.Start(pInfo);

            try
            {
                // Wait for the process to exit or time out.
                p.WaitForExit(100000);

                //Check to see if the process is still running.
                if (p.HasExited == false)
                {
                    // Installer process is still running.
                    p.Kill();

                    // Creating error message.
                    String errMsg = "ERROR: ";
                    if (uninstall || cleanup) errMsg += "Uninstaller ";
                    else errMsg += "Installer ";
                    errMsg += "has not exited within the allowed wait interval.";

                    ErrOut.WriteLine(errMsg);
                    return 1;
                }
                else
                {
                    // Checking error code.
                    if (p.ExitCode != 0)
                    {
                        // Getting installer output.
                        String errorOutput = p.StandardError.ReadToEnd();

                        // Creating error message.
                        String errMsg = "ERROR: ";
                        if (uninstall || cleanup) errMsg += "Uninstaller ";
                        else errMsg += "Installer ";
                        errMsg += "has finished with incorrect exit code " + p.ExitCode + ".";
                        errMsg += "Message trace:" + Environment.NewLine + errorOutput;

                        ErrOut.WriteLine(errMsg);
                        return 1;
                    }
                }

                // Success!
                return 0;
            }
            finally
            {
                p.Close();
            }
        }

        /// <summary>
        /// Prints server log.
        /// </summary>
        static void PrintServerLog()
        {
            String logDirPath = InstallPath + "\\Personal\\Logs";
            if (!Directory.Exists(logDirPath))
            {
                ErrOut.WriteLine("Server log directory does not exist: " + logDirPath + ". Skipping printing log file.");
                return;
            }

            ProcessStartInfo pInfo = new ProcessStartInfo();
            pInfo.FileName = "\"" + ConsolidatedDir + "\\ServerLogTail.exe\"";
            pInfo.Arguments = "\"" + logDirPath + "\"";
            pInfo.UseShellExecute = false;

            ErrOut.WriteLine("  Printing server log contents (please look in 'All messages' tab)...");

            StdOut.WriteLine("-------- BEGINNING OF SERVER LOG ---------");
            StdOut.Flush();

            Process p = Process.Start(pInfo);
            Thread.Sleep(3000);

            StdOut.WriteLine("-------- END OF SERVER LOG ---------");
            StdOut.Flush();

            if (p != null)
            {
                p.Kill();
                p.Close();
            }
        }

        /// <summary>
        /// Runs all available tests.
        /// </summary>
        static Int32 RunAllTests()
        {
            // Indicates if all tests succeeded.
            Int32 errCode = 0;

            // Listing all tests configuration files.
            String[] testConfigPaths = Directory.GetFiles(Path.Combine(ConsolidatedDir, "BuildSystem\\Tests"), "*.xml", SearchOption.AllDirectories);

            // Going through all found tests.
            foreach (String testConfigPath in testConfigPaths)
            {
                ProcessStartInfo pInfo = new ProcessStartInfo();
                pInfo.FileName = "\"" + ConsolidatedDir + "\\TestRunner.exe\"";
                pInfo.Arguments = "\"" + testConfigPath + "\" \"" + InstallPath + "\"";
                pInfo.UseShellExecute = false;

                // Starting test process.
                Process p = Process.Start(pInfo);
                p.WaitForExit();
                Int32 exitCode = p.ExitCode;
                p.Close();

                // Checking if test succeeded.
                if (exitCode != 0)
                {
                    ErrOut.WriteLine("Test has failed: " + testConfigPath);
                    errCode = exitCode;
                }
            }

            return errCode;
        }

        // First parameter is a path to consolidated directory.
        static Int32 Main(String[] args)
        {
            try
            {
                // Printing tool welcome message.
                BuildSystem.PrintToolWelcome("Starcounter Installer");

                // Checking if same executable is already running.
                if (BuildSystem.IsSameExecutableRunning())
                    return 0;

                Int32 errCode = 0;

                // Setting consolidated directory.
                if (args.Length > 0)
                    ConsolidatedDir = args[0];

                // Checking if we need to install VS plugin.
                if (BuildSystem.IsNightlyBuild() ||
                    Environment.GetEnvironmentVariable("SC_INSTALL_VS_PLUGIN") != null)
                {
                    InstallVsPlugin = true;
                }

                // Checking if Visual Studio plugin installation is enabled.
                if (InstallVsPlugin == true)
                    ErrOut.WriteLine("  Starcounter Visual Studio plugin installation enabled.");

                // Getting the check-out directory location.
                if (Environment.GetEnvironmentVariable(BuildSystem.CheckOutDirEnvVar) == null)
                {
                    ErrOut.WriteLine("Environment variable {0} does not exist.", BuildSystem.CheckOutDirEnvVar);
                    return 1;
                }

                // Catching all possible exceptions.
                String origSettingFilePath = ConsolidatedDir + "\\SetupSettings.ini";
                String savedSettingFilePath = ConsolidatedDir + "\\SetupSettingsSaved.ini";

                try
                {
                    // Removing server directories explicitly.
                    StdOut.WriteLine("  Removing old installation and server directories...");
                    if (Directory.Exists(InstallPath))
                        Directory.Delete(InstallPath, true);

                    // Saving the original settings file.
                    File.Copy(origSettingFilePath, savedSettingFilePath, true);

                    // Running installation.
                    ErrOut.WriteLine("  Starting Starcounter installer...");
                    errCode = RunInstaller(false, false, ConsolidatedDir);
                    if (errCode != 0)
                    {
                        // Trying to cleanup and then install again.
                        ErrOut.WriteLine("  Starcounter seems installed. Running installation cleanup...");
                        errCode = RunInstaller(false, true, ConsolidatedDir);
                        if (errCode != 0)
                            return errCode;

                        // And trying to install once again.
                        ErrOut.WriteLine("  Starting Starcounter installer again...");
                        errCode = RunInstaller(false, false, ConsolidatedDir);
                        if (errCode != 0)
                            return errCode;
                    }

                    // Setting StarcounterBin environment variable to newly installed path.
                    Environment.SetEnvironmentVariable(BuildSystem.StarcounterBinVar, ConsolidatedDir);

                    ErrOut.WriteLine("    Starcounter has been installed successfully.");

                    // Launching tests.
                    ErrOut.WriteLine("  Running Starcounter tests...");
                    errCode = RunAllTests();
                    if (errCode != 0)
                        return errCode;
                    ErrOut.WriteLine("    All Starcounter tests finished.");
                }
                finally
                {
                    // Printing server log as a first thing after all tests.
                    PrintServerLog();
                    ErrOut.WriteLine("---------------------------------------------------------------");

                    // Publishing server logs as artifacts.
                    //ErrOut.WriteLine("  Publishing server logs as artifacts...");
                    //StdOut.WriteLine("##teamcity[publishArtifacts '{0}']", Path.Combine(InstallPath, "Personal\\Logs\\*"));

                    // Running cleanup now.
                    ErrOut.WriteLine("  Starting Starcounter uninstaller...");
                    errCode = RunInstaller(true, false, ConsolidatedDir);

                    // Checking if uninstallation is successful.
                    if (errCode == 0)
                    {
                        ErrOut.WriteLine("    Starcounter has been uninstalled successfully.");
                    }
                    else
                    {
                        ErrOut.WriteLine("    ERROR uninstalling Starcounter, running complete clean up...");
                        RunInstaller(false, true, ConsolidatedDir);
                    }

                    // Restoring the original setup settings file.
                    ErrOut.WriteLine("  Restoring the original setup settings file...");
                    if (File.Exists(savedSettingFilePath))
                    {
                        File.Copy(savedSettingFilePath, origSettingFilePath, true);
                        File.Delete(savedSettingFilePath);
                    }

                    ErrOut.WriteLine("---------------------------------------------------------------");
                }

                return 0;
            }
            catch (Exception generalException)
            {
                return BuildSystem.LogException(generalException);
            }
        }
    }
}
