using System;
using System.IO;
using System.Diagnostics;
using System.Threading;
using BuildSystemHelper;

namespace BuildLevel0
{
    class BuildLevel0
    {
        // Which Level0 version to build.
        static readonly String level0_BuildBranch = Environment.GetEnvironmentVariable("GIT_LEVEL0_BRANCH");

        // Important directories.
        static readonly String Level0_RootDir =
            Environment.GetEnvironmentVariable(BuildSystem.CheckOutDirEnvVar) + "\\Level0\\msbuild";

        // Level0 latest stable files directory.
        static readonly String FtpLatestStableDir =
            BuildSystem.MappedBuildServerFTP + "\\SCBuilds\\Level0\\" + level0_BuildBranch + "\\LatestStable";

        /// <summary>
        /// Core function to build Level0 in a certain configuration and platform.
        /// </summary>
        /// <param name="configuration">Release or Debug</param>
        /// <param name="platform">Win32 or x64</param>
        /// <param name="parameters">Specific, if any.</param>
        /// <param name="errorOut">Where to wirte about errors.</param>
        static void BuildWithIncrediBuild(String configuration, String platform, String parameters, TextWriter errorOut)
        {
            String buildType = "/build";

            // Killing interrupting processes.
            errorOut.WriteLine("Killing disturbing processes...");
            BuildSystem.KillDisturbingProcesses(new String[] {"buildlevel0", "buildconsole", "buildsystem"});
            Thread.Sleep(2000);

            // Create IncrediBuild process start information.
            ProcessStartInfo ibProcInfo = new ProcessStartInfo
            {
                /*
                FileName = @"C:\Program Files (x86)\Xoreax\IncrediBuild\BuildConsole.exe",
                Arguments = Path.Combine(Level0_RootDir, "Level0.sln") + " " + buildType +  " /cfg=\"" + configuration + "|" + platform + "\" " + parameters,
                UseShellExecute = false,
                WorkingDirectory = Level0_RootDir
                */

                FileName = BuildSystem.MsBuildExePath,
                Arguments = Path.Combine(Level0_RootDir, "Level0.sln") + " /property:Configuration=\"" + configuration + "\";Platform=\"" + platform + "\";GenerateFullPaths=true /consoleloggerparameters:Summary /verbosity:normal /maxcpucount /nodeReuse:False " + parameters,
                UseShellExecute = false,
                WorkingDirectory = Level0_RootDir
            };

            errorOut.WriteLine("Building with IncrediBuild: \"" + ibProcInfo.FileName + "\" " + ibProcInfo.Arguments);
            errorOut.WriteLine("Configuration: " + configuration + ", Platform: " + platform + ", Build type: " + buildType);

            // Start the build process.
            Process p = Process.Start(ibProcInfo);

            // Wait for the process to exit or time out.
            p.WaitForExit(300000);

            // Check to see if the process is still running.);
            if (p.HasExited == false)
            {
                // Build process is still running.
                p.Kill();

                // Creating return message.
                String errMsg = "ERROR: Level0 build process has not exited within the allowed wait interval (60 seconds)." + Environment.NewLine;
                errorOut.WriteLine(errMsg);

                throw new Exception("Level0 building process failed.");
            }

            // Checking process error code.
            if (p.ExitCode != 0)
            {
                // Creating return message.
                String errMsg = "ERROR: ";
                errMsg += "Level0 build process has finished with incorrect exit code." + Environment.NewLine;
                errorOut.WriteLine(errMsg);

                throw new Exception("Level0 building process failed.");
            }
        }

        static int Main(String[] args)
        {
            //System.Diagnostics.Debugger.Launch();

            // Catching all possible exceptions.
            try
            {
                // Printing tool welcome message.
                BuildSystem.PrintToolWelcome("Level0 Build");

                TextWriter errorOut = Console.Error;

                // Checking that we have correct environment.
                if (Environment.GetEnvironmentVariable(BuildSystem.CheckOutDirEnvVar) == null)
                {
                    throw new Exception(String.Format("Environment variable '{0}' does not exist.", BuildSystem.CheckOutDirEnvVar));
                }

                if (Environment.GetEnvironmentVariable(BuildSystem.BuildSystemDirEnvVar) == null)
                {
                    throw new Exception(String.Format("Environment variable '{0}' does not exist.", BuildSystem.BuildSystemDirEnvVar));
                }

                // Checking for build agent log directory existence.
                if (!Directory.Exists(BuildSystem.BuildAgentLogDir))
                    Directory.CreateDirectory(BuildSystem.BuildAgentLogDir);

                // Checking if its a release build.
                Boolean releaseConfBuild = (String.Compare(Environment.GetEnvironmentVariable("Configuration"), "Release", true) == 0);

                // Checking if any arguments are supplied.
                if (args.Length > 0)
                {
                    if ((args[0] == "JustCopyFiles"))
                    {
                        if (BuildSystem.IsPersonalBuild())
                        {
                            Console.WriteLine("Skipping uploading Level0 '{0}' build artifacts since its a Personal build...", level0_BuildBranch);
                            return 0;
                        }
                        else if (releaseConfBuild)
                        {
                            // Checking that directory exists.
                            if (!Directory.Exists(FtpLatestStableDir))
                                Directory.CreateDirectory(FtpLatestStableDir);

                            // Now we can copy all built files for all configurations.
                            Console.WriteLine("Uploading Level0 '{0}' build artifacts to mapped FTP directory: '{1}'", level0_BuildBranch, FtpLatestStableDir);

                            // Lock file used for files upload synchronization.
                            String lockFile = FtpLatestStableDir + "\\locked";

                            // Checking that there is no lock file.
                            while (File.Exists(lockFile))
                            {
                                errorOut.WriteLine("Waiting for lock file to be released...");
                                Thread.Sleep(5000);
                            }

                            try
                            {
                                // Creating lock file.
                                File.WriteAllText(lockFile, "locked!");

                                // Copying all built files.
                                BuildSystem.CopyDirToSharedFtp(Level0_RootDir + @"\x64\Release", FtpLatestStableDir + @"\x64\Release");
                                BuildSystem.CopyDirToSharedFtp(Level0_RootDir + @"\x64\Debug", FtpLatestStableDir + @"\x64\Debug");

                                // Copying headers directory as well.
                                BuildSystem.CopyDirToSharedFtp(Level0_RootDir + @"\include", FtpLatestStableDir + @"\include");

                                // BuildSystem.CopyDirToSharedFtp(Level0_RootDir + @"\Win32\Release", FtpCopyDir + @"\Win32\Release");
                                // BuildSystem.CopyDirToSharedFtp(Level0_RootDir + @"\Win32\Debug", FtpCopyDir + @"\Win32\Debug");
                            }
                            finally
                            {
                                // Deleting lock file.
                                if (File.Exists(lockFile))
                                    File.Delete(lockFile);
                            }

                            return 0;
                        }
                    }
                }

                Stopwatch timer = Stopwatch.StartNew();
                errorOut.WriteLine("Starting Level0 '{0}' Build Process...", level0_BuildBranch);

                // Checking if user runs special debug build.
                if (!releaseConfBuild)
                {
                    BuildWithIncrediBuild("Debug", "x64", "", errorOut);
                }
                else
                {
                    BuildWithIncrediBuild("Release", "x64", "", errorOut);

                    // Checking if we only run a personal build.
                    if (!BuildSystem.IsPersonalBuild())
                    {
                        BuildWithIncrediBuild("Debug", "x64", "", errorOut);

                        // BuildWithIncrediBuild("Release", "Win32", "", errorOut);
                        // BuildWithIncrediBuild("Debug", "Win32", "", errorOut);
                    }
                }

                // Calculating total time spent on the building process.
                timer.Stop();
                errorOut.WriteLine("Level0 '{0}' build process finished successfully.", level0_BuildBranch);

                errorOut.WriteLine("Total time spent on the build (ms): " + timer.ElapsedMilliseconds + ".");
                errorOut.WriteLine("---------------------------------------------------------------");
            }
            catch (Exception generalException)
            {
                return BuildSystem.LogException(generalException);
            }

            return 0;
        }
    }
}
