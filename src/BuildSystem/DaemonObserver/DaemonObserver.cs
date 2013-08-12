using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using BuildSystemHelper;
using System.Diagnostics;
using System.Reflection;
using System.Threading;

namespace DaemonObserver
{
    class DaemonObserver
    {
        static void StartDaemonsIfNeeded(String localBuildsDirectory)
        {
            // Getting list of all daemon paths in root builds directory.
            String[] daemonExePaths = Directory.GetFiles(localBuildsDirectory, BuildSystem.BuildDaemonName + ".exe", SearchOption.AllDirectories);
            if (daemonExePaths.Length <= 0) // No daemon executables found.
                return;

            // Getting list of all running daemons.
            Process[] daemonProcs = Process.GetProcessesByName(BuildSystem.BuildDaemonName);
            String[] daemonProcPaths = null;
            if (daemonProcs.Length > 0) // Some running daemons found.
            {
                daemonProcPaths = new String[daemonProcs.Length];
                for (Int32 i = 0; i < daemonProcs.Length; i++)
                    daemonProcPaths[i] = daemonProcs[i].MainModule.FileName;
            }

            // Going through all daemon paths on file system.
            foreach (String daemonExePath in daemonExePaths)
            {
                Boolean daemonIsRunning = false;

                // Checking if there are any daemon processes running.
                if (daemonProcs.Length > 0)
                {
                    // Testing every running process.
                    foreach (String daemonProcPath in daemonProcPaths)
                    {
                        // Checking if daemon process is running.
                        if (String.Compare(daemonExePath, daemonProcPath, StringComparison.InvariantCultureIgnoreCase) == 0)
                        {
                            daemonIsRunning = true;
                            break;
                        }
                    }
                }

                // Checking if daemon is not running, so we start it.
                if (!daemonIsRunning)
                {
                    // Checking that there is no blocking file to start daemon.
                    if (!File.Exists(Path.Combine(Path.GetDirectoryName(daemonExePath), BuildSystem.StopDaemonFileName)))
                    {
                        // Creating process information.
                        ProcessStartInfo procStartInfo = new ProcessStartInfo();
                        procStartInfo.FileName = "\"" + daemonExePath + "\"";

                        // Logging event...
                        Console.WriteLine("Starting new daemon process from: " + daemonExePath);

                        // Starting the daemon process.
                        Process daemonProc = Process.Start(procStartInfo);
                        daemonProc.Close();
                    }
                }
                else
                {
                    Console.WriteLine("Daemon '" + daemonExePath + "' is already running. Skipping..." + Environment.NewLine);
                }

                // Timeout.
                Thread.Sleep(3000);
            }
        }

        static Int32 Main(String[] args)
        {
            try
            {
                // Printing tool welcome message.
                BuildSystem.PrintToolWelcome("Build Daemons Observer");

                // Killing same processes.
                BuildSystem.KillSameProcesses();

                // Obtaining root builds directory.
                String localBuildsDirectory = BuildSystem.LocalBuildsFolder;
                if (args.Length > 0)
                    localBuildsDirectory = args[0];

                // Runs forever.
                while (true)
                {
                    // Checking if observer process should be stopped.
                    if (File.Exists(Path.Combine(BuildSystem.GetAssemblyDir(), BuildSystem.StopObserverFileName)))
                    {
                        Console.WriteLine("Observer stop file found. Quiting.");
                        return 0;
                    }

                    // Starting all needed daemons.
                    StartDaemonsIfNeeded(localBuildsDirectory);

                    // Timeout.
                    Thread.Sleep(5000);
                }
            }
            catch (Exception generalException)
            {
                return BuildSystem.LogException(generalException);
            }
        }
    }
}
