using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using BuildSystemHelper;
using System.Threading;
using System.Reflection;

namespace BranchBuild
{
    class BranchBuild
    {
        /// <summary>
        /// Stop other versions of the same build type.
        /// </summary>
        public static void StopOtherBuildsOfSameType(String buildType, String currentBuildNumber)
        {
            // For custom build all builds should be present.
            if (buildType == BuildSystem.CustomBuildsName)
                return;

            Console.Error.WriteLine("Stopping other builds of the same type...");

            String buildTypeDir = Path.Combine(BuildSystem.LocalBuildsFolder, buildType);
            DirectoryInfo dirInfo = new DirectoryInfo(buildTypeDir);

            foreach (DirectoryInfo dir in dirInfo.GetDirectories("*", SearchOption.TopDirectoryOnly))
            {
                // Checking all directories except current one.
                if (dir.Name != currentBuildNumber)
                {
                    String daemonStopFilePath = Path.Combine(buildTypeDir, Path.Combine(dir.Name, BuildSystem.StopDaemonFileName));

                    // Creating daemon stop file.
                    if (!File.Exists(daemonStopFilePath))
                    {
                        File.WriteAllText(daemonStopFilePath, "Stopped!");
                        Console.Error.WriteLine("Stopped other running build version: " + dir.Name);
                    }
                }
            }
        }

        /// <summary>
        /// Branches specific build to be one of known types: Nightly, Stable, Special.
        /// </summary>
        static Int32 Main(string[] args)
        {
            /*
            Environment.SetEnvironmentVariable("SC_RELEASING_BUILD", "True");
            Environment.SetEnvironmentVariable("SC_CUSTOM_BUILD", "True");
            Environment.SetEnvironmentVariable("Configuration", "Release");
            Environment.SetEnvironmentVariable(BuildSystem.CheckOutDirEnvVar, "C:\\github");
            Environment.SetEnvironmentVariable(BuildSystem.BuildNumberEnvVar, "1.2.3.4");
            Environment.SetEnvironmentVariable(BuildSystem.BuildSystemDirEnvVar, "C:\\BuildSystem");
            */

            try
            {
                // Printing tool welcome message.
                BuildSystem.PrintToolWelcome("Build Branch Creator");

                // Checking if same executable is already running.
                if (BuildSystem.IsSameExecutableRunning())
                    return 0;

                // Checking if its releasing build.
                if (!BuildSystem.IsReleasingBuild())
                {
                    Console.WriteLine("It is not a releasing build. Quiting.");
                    return 0;
                }

                // Checking that all needed variables are defined.
                if (!BuildSystem.AllEnvVariablesExist(new String[] 
                {
                    "Configuration",
                    // "Platform",
                    BuildSystem.BuildNumberEnvVar,
                    BuildSystem.CheckOutDirEnvVar,
                    BuildSystem.BuildSystemDirEnvVar
                }))
                {
                    throw new Exception("Some needed environment variables do not exist...");
                }

                String buildsFolderName = null;

                // Checking if its a scheduled stable build.
                if (Environment.GetEnvironmentVariable("SC_STABLE_BUILD") != null)
                    buildsFolderName = BuildSystem.StableBuildsName;

                // Checking if its a nightly build.
                if (BuildSystem.IsNightlyBuild())
                    buildsFolderName = BuildSystem.NightlyBuildsName;

                // Checking if its a scheduled custom build.
                if (Environment.GetEnvironmentVariable("SC_CUSTOM_BUILD") != null)
                    buildsFolderName = BuildSystem.CustomBuildsName;

                // Checking if its just a latest build.
                if (buildsFolderName == null)
                    return 0;

                // Obtaining current workspace directory.
                String devRootDir = Environment.GetEnvironmentVariable(BuildSystem.CheckOutDirEnvVar);
                if (devRootDir == null)
                    throw new Exception("Can't get path to current workspace directory.");

                if (!Directory.Exists(Path.Combine(devRootDir, "Level1")))
                    throw new Exception("Path to current workspace directory is wrong.");

                // Removing Level0 sources directory.
                if (Directory.Exists(Path.Combine(devRootDir, "Level0")))
                    Directory.Delete(Path.Combine(devRootDir, "Level0"), true);

                // Target build directory.
                String buildNumber = Environment.GetEnvironmentVariable(BuildSystem.BuildNumberEnvVar);
                if (buildNumber == null)
                    throw new Exception("Can't get build number environment variable.");

                String targetBuildDir = Path.Combine(BuildSystem.LocalBuildsFolder, Path.Combine(buildsFolderName, buildNumber));

                // Stopping previous versions of the same build type.
                StopOtherBuildsOfSameType(buildsFolderName, buildNumber);

                // Dynamically checking if directory exists and quiting if it does.
                if (Directory.Exists(targetBuildDir))
                    throw new Exception("Directory is occupied. Quiting...");

                // Now creating empty directory.
                Directory.CreateDirectory(targetBuildDir);

                // Creating stop file.
                File.WriteAllText(Path.Combine(targetBuildDir, BuildSystem.StopDaemonFileName), "Stop!");

                // Build tools used.
                String[] buildToolNames = { "BuildsFillupDaemon", "GenerateInstaller" };

                // Diagnostics.
                Console.Error.WriteLine("Copying sources and binaries to the build directory...");

                // Copy all needed build tools to target directory.
                String buildToolsBinDir = Path.Combine(devRootDir, "Level1", "bsbin", "Debug");

                // Copying needed binaries.
                foreach (String toolName in buildToolNames)
                {
                    File.Copy(Path.Combine(buildToolsBinDir, toolName + ".exe"),
                        Path.Combine(targetBuildDir, toolName + ".exe"),
                        true);

                    File.Copy(Path.Combine(buildToolsBinDir, toolName + ".pdb"),
                        Path.Combine(targetBuildDir, toolName + ".pdb"),
                        true);
                }

                // Copying shared build system library.
                File.Copy(Path.Combine(buildToolsBinDir, "BuildSystemHelper.dll"),
                    Path.Combine(targetBuildDir, "BuildSystemHelper.dll"),
                    true);

                File.Copy(Path.Combine(buildToolsBinDir, "BuildSystemHelper.pdb"),
                    Path.Combine(targetBuildDir, "BuildSystemHelper.pdb"),
                    true);

                // Creating version info file.
                String versionFileContents = "<?xml version=\"1.0\" encoding=\"UTF-8\" ?>" + Environment.NewLine;

                // Adding header version tag.
                versionFileContents += "<VersionInfo>" + Environment.NewLine;

                // Adding current build configuration.
                versionFileContents += "  <Configuration>" + Environment.GetEnvironmentVariable("Configuration") + "</Configuration>" + Environment.NewLine;

                // Adding current build platform.
                versionFileContents += "  <Platform>" + Environment.GetEnvironmentVariable("Platform") + "</Platform>" + Environment.NewLine;

                // Adding current build version.
                versionFileContents += "  <Version>" + Environment.GetEnvironmentVariable(BuildSystem.BuildNumberEnvVar) + "</Version>" + Environment.NewLine;

                // Adding builds folder name.
                versionFileContents += "  <BuildsFolderName>" + buildsFolderName + "</BuildsFolderName>" + Environment.NewLine;

                // Adding closing tag.
                versionFileContents += "</VersionInfo>" + Environment.NewLine;

                // Saving version file.
                File.WriteAllText(Path.Combine(targetBuildDir, BuildSystem.VersionXMLFileName), versionFileContents);

                // Copy all sources and binaries from the current build
                // folder to the destination build directory.
                BuildSystem.CopyFilesRecursively(
                    new DirectoryInfo(devRootDir),
                    new DirectoryInfo(targetBuildDir));

                // Copying the consolidated directory.
                String binOutputPath = Environment.GetEnvironmentVariable(BuildSystem.BuildOutputEnvVar);
                if (binOutputPath == null)
                    throw new Exception("Can not obtain current binary output directory.");

                // Copying all binaries.
                BuildSystem.CopyFilesRecursively(new DirectoryInfo(binOutputPath),
                    new DirectoryInfo(Path.Combine(targetBuildDir, BuildSystem.CommonDefaultBuildOutputPath)));

                // Removing built tools binary directory.
                Directory.Delete(Path.Combine(targetBuildDir, "BuildSystem"), true);

                // Deleting stop file.
                File.Delete(Path.Combine(targetBuildDir, BuildSystem.StopDaemonFileName));

                // Configuring builds fill up process.
                ProcessStartInfo buildsFillupProcInfo = new ProcessStartInfo();
                buildsFillupProcInfo.FileName = "\"" + Path.Combine(targetBuildDir, BuildSystem.BuildDaemonName + ".exe") + "\"";

                // Starting the builds fill up process.
                Process buildsFillUpProc = Process.Start(buildsFillupProcInfo);
                buildsFillUpProc.Close();

                // Diagnostics message.
                Console.Error.WriteLine("Starting builds fill-up process and quiting...");

                return 0;
            }
            catch (Exception generalException)
            {
                return BuildSystem.LogException(generalException);
            }
        }
    }
}
