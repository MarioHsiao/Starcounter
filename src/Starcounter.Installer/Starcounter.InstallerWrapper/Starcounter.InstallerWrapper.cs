using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace Starcounter.InstallerWrapper
{
    class InstallerWrapper
    {
        /// <summary>
        /// Checks if the latest .NET 4.5 version is installed.
        /// </summary>
        /// <returns>True if yes.</returns>
        static Boolean IsNet45Installed()
        {
            try
            {
                using (RegistryKey ndpKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full"))
                {
                    Int32 netVersion = (Int32) ndpKey.GetValue("Release");

                    if (netVersion >= 378389)
                        return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        /// <summary>
        /// Copies one stream to another.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="output"></param>
        static void CopyStream(Stream input, Stream output)
        {
            Byte[] buffer = new Byte[8 * 1024];
            Int32 len;
            while ((len = input.Read(buffer, 0, buffer.Length)) > 0)
                output.Write(buffer, 0, len);
        }

        /// <summary>
        /// Runs specified program and waits for it.
        /// </summary>
        /// <param name="exeFilePath"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        static Int32 RunAndWaitForProgram(String exeFilePath, String args, Boolean elevate, Boolean wait)
        {
            Process exeProg = new Process();
            exeProg.StartInfo.FileName = exeFilePath;
            exeProg.StartInfo.Arguments = args;
            exeProg.StartInfo.UseShellExecute = true;

            // Checking if we need to start elevated instance.
            if (elevate)
                exeProg.StartInfo.Verb = "runas";

            // Exit code of the child instance.
            Int32 exitCode = 1;
            try
            {
                // Starting program.
                exeProg.Start();

                // Checking if we need to wait.
                if (!wait)
                    return 0;

                // Waiting until program is finished.
                exeProg.WaitForExit();

                // Getting exit code.
                exitCode = exeProg.ExitCode;
                exeProg.Close();
            }
            catch
            {
                return 10;
            }

            return exitCode;
        }

        /// <summary>
        /// Extracts certain resource file to disk.
        /// </summary>
        /// <param name="resourceFileName"></param>
        /// <param name="pathToDestFile"></param>
        static Boolean ExtractResourceToFile(String resourceFileName, String pathToDestFile)
        {
            try
            {
                using (Stream resStream = Assembly.GetEntryAssembly().GetManifestResourceStream("Starcounter.InstallerWrapper.resources." + resourceFileName))
                {
                    using (Stream file = File.OpenWrite(pathToDestFile))
                        CopyStream(resStream, file);
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Check if another instance of setup is running.
        /// </summary>
        static Boolean AnotherSetupRunning()
        {
            // Trying to find through all processes.
            Process[] processeslist = Process.GetProcesses();
            foreach (Process process in processeslist)
            {
                if (process.ProcessName.StartsWith("Starcounter", StringComparison.CurrentCultureIgnoreCase) &&
                    process.ProcessName.EndsWith("Setup", StringComparison.CurrentCultureIgnoreCase))
                {
                    // Checking process IDs.
                    if (Process.GetCurrentProcess().Id != process.Id)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        const String Net45ResName = "dotnetfx45_full_x86_x64.exe";
        const String ScSetupVersion = "2.0.0.0";
        const String ScSetupResName = "Starcounter-Setup.exe";
        const String ScSetupExtractName = ScSetupResName; //"Starcounter-" + ScSetupVersion + "-Setup.exe";
        const String ElevatedParam = "Elevated";

        static Int32 Main(String[] args)
        {
            Boolean isElevated = false;

            if ((args.Length > 0) && (args[0] == ElevatedParam))
                isElevated = true;

            // Extracting everything to temp directory.
            String userTempDir = Environment.GetEnvironmentVariable("TEMP");

            // Simply exiting if another setup is running.
            if (!isElevated)
            {
                if (AnotherSetupRunning())
                {
                    MessageBox.Show("Another Starcounter setup instance is already running.",
                        "Starcounter setup...", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

                    return 1;
                }

                // Running elevated wrapper.
                Int32 errCode = RunAndWaitForProgram(Process.GetCurrentProcess().MainModule.FileName, ElevatedParam, true, true);
                if (0 != errCode)
                    return errCode;

                // Extracting installer and starting it.
                String scSetupExePath = Path.Combine(userTempDir, ScSetupExtractName);
                if (ExtractResourceToFile(ScSetupResName, scSetupExePath))
                {
                    // Skipping waiting for installer, just quiting.
                    if (0 == RunAndWaitForProgram(scSetupExePath, "", false, false))
                        return 0;
                }
            }
            else
            {
                // Checking if .NET 4.5 is installed.
                if (!IsNet45Installed())
                {
                    MessageBox.Show("Microsoft .NET Framework 4.5 is not detected on your computer. It will be installed now.",
                        "Starcounter setup...", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    String net45ExePath = Path.Combine(userTempDir, Net45ResName);
                    if (ExtractResourceToFile(Net45ResName, net45ExePath))
                    {
                        if (0 == RunAndWaitForProgram(net45ExePath, "", true, true))
                        {
                            // Double checking that now .NET version is really installed.
                            if (IsNet45Installed())
                                return 0;
                        }
                    }
                }
                else
                {
                    return 0;
                }
            }

            MessageBox.Show("Starcounter was not successfully installed.",
                "Starcounter setup...", MessageBoxButtons.OK, MessageBoxIcon.Error);

            return 2;
        }
    }
}
