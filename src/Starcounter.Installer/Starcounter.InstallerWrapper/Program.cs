using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
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
        static Int32 RunAndWaitForProgram(String exeFilePath, String args, Boolean wait = true)
        {
            Process exeProg = new Process();
            exeProg.StartInfo.FileName = exeFilePath;
            exeProg.StartInfo.Arguments = args;
            exeProg.StartInfo.UseShellExecute = true;
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
                // Returning error.
                return -1;
            }

            // Checking for the child error code explicitly.
            if (exitCode != 0)
                return exitCode;

            return 0;
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

        const String net45ResName = "dotnetfx45_full_x86_x64.exe";
        const String scSetupResName = "Starcounter-Setup.exe";

        static void Main(string[] args)
        {
            // Extracting everything to temp directory.
            String userTempDir = Environment.GetEnvironmentVariable("TEMP");

            // Checking if .NET 4.5 is installed.
            if (!IsNet45Installed())
            {
                String net45ExePath = Path.Combine(userTempDir, net45ResName);
                if (ExtractResourceToFile(net45ResName, net45ExePath))
                {
                    if (0 == RunAndWaitForProgram(net45ExePath, ""))
                    {
                        // Double checking that now .NET version is really installed.
                        if (IsNet45Installed())
                            goto RUN_SC_INSTALLER;
                    }
                }

                goto ERROR_OCCURIED;
            }

RUN_SC_INSTALLER:

            String scSetupExePath = Path.Combine(userTempDir, scSetupResName);

            if (ExtractResourceToFile(scSetupResName, scSetupExePath))
            {
                if (0 == RunAndWaitForProgram(scSetupExePath, "", false))
                {
                    return;
                }
            }

ERROR_OCCURIED:

            MessageBox.Show("Sorry, Starcounter can't be installed, your environment is crap..");
        }
    }
}
