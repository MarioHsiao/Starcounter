using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using System.Configuration.Install;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Threading;
using Starcounter.InstallerEngine.VsSetup;
using Starcounter;

namespace Starcounter.InstallerEngine
{
    public class DependenciesCheck
    {
        /// <summary>
        /// Gets an list of installed programs (that have installation paths)
        /// from a certain registry key.
        /// </summary>
        /// <returns>List of installed programs.</returns>
        static List<String> GetInstalledProgramsUsingKey(RegistryKey regKey)
        {
            List<String> installedPrograms = new List<String>();
            // Iterating through all "uninstall" entries.
            foreach (String skName in regKey.GetSubKeyNames())
            {
                using (RegistryKey regSubKey = regKey.OpenSubKey(skName))
                {
                    // Checking if installation location is known.
                    if (regSubKey.GetValue("InstallLocation") != null)
                    {
                        // Each properly installed program must have a "DisplayName" value.
                        String programName = (String) regSubKey.GetValue("DisplayName");
                        if (programName != null)
                        {
                            // Appending installation path as well.
                            installedPrograms.Add(programName + "@" + regSubKey.GetValue("InstallLocation"));
                        }
                    }
                }
            }
            return installedPrograms;
        }

        static List<String> GetInstalledProgramsFromCurrentUser32()
        {
            using (RegistryKey rk = Registry.CurrentUser.OpenSubKey(ConstantsBank.Registry32BitUninstallPath))
            {
                if (rk == null) return new List<String>();
                return GetInstalledProgramsUsingKey(rk);
            }
        }

        static List<String> GetInstalledProgramsFromCurrentUser64()
        {
            using (RegistryKey rk = Registry.CurrentUser.OpenSubKey(ConstantsBank.Registry64BitUninstallPath))
            {
                if (rk == null) return new List<String>();
                return GetInstalledProgramsUsingKey(rk);
            }
        }
        
        static List<String> GetInstalledProgramsFromLocalMachine32()
        {
            using (RegistryKey rk = Registry.LocalMachine.OpenSubKey(ConstantsBank.Registry32BitUninstallPath))
            {
                if (rk == null) return new List<String>();
                return GetInstalledProgramsUsingKey(rk);
            }
        }
        
        static List<String> GetInstalledProgramsFromLocalMachine64()
        {
            using (RegistryKey rk = Registry.LocalMachine.OpenSubKey(ConstantsBank.Registry64BitUninstallPath))
            {
                if (rk == null) return new List<String>();
                return GetInstalledProgramsUsingKey(rk);
            }
        }

        static List<String> GetInstalledPrograms()
        {
            List<String> installedPrograms = GetInstalledProgramsFromCurrentUser32();
            installedPrograms.AddRange(GetInstalledProgramsFromCurrentUser64());
            installedPrograms.AddRange(GetInstalledProgramsFromLocalMachine32());
            installedPrograms.AddRange(GetInstalledProgramsFromLocalMachine64());
            return installedPrograms;
        }

        /// <summary>
        /// Checks if proper version of DirectX is installed.
        /// </summary>
        static void CheckDirectXAndVideoCardCompatibility()
        {
            // Launching 'dxdiag' synchronously to get info about DirectX.
            String dxDiagOutputFile = Path.Combine(Environment.CurrentDirectory, "DxDiagOutput.tmp");
            Process dxDiag = new Process();
            try
            {
                dxDiag.StartInfo.FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "dxdiag.exe");
                dxDiag.StartInfo.Arguments = "/t \"" + dxDiagOutputFile + "\"";
                dxDiag.Start();
                dxDiag.WaitForExit(100 * 1000); // Waiting for the 'dxdiag' process to finish.

                if (!dxDiag.HasExited)
                {
                    throw ErrorCode.ToException(Error.SCERRINSTALLERPROCESSTIMEOUT,
                        "Process info: " + dxDiag.StartInfo.FileName);
                }

                if (dxDiag.ExitCode != 0)
                {
                    throw ErrorCode.ToException(Error.SCERRINSTALLERDXDIAGPROBLEM,
                        "DxDiag error code value: " + dxDiag.ExitCode);
                }
            }
            finally
            {
                dxDiag.Close();
            }
            String dxDiagOutput = null;

            // Waiting for DirectX Diagnostic file creation.
            while (dxDiagOutput == null)
            {
                try
                {
                   dxDiagOutput = File.ReadAllText(dxDiagOutputFile);
                }
                catch { }
            }
            File.Delete(dxDiagOutputFile);

            // Trying to determine DirectX version and features support.
            Match matchDXVersion = Regex.Match(dxDiagOutput, @"\bDirectX Version\: DirectX (\d{2})\b"),
                  matchDXD3D9Overlay = Regex.Match(dxDiagOutput, @"\bD3D9 Overlay\: (\w{9})\b");
            Boolean dxVersionOK = false, dxOverlaySupportOK = false;
            
            // Checking DirectX version.
            if (matchDXVersion.Success)
            {
                // The first group is always a full matched string so skip it.
                CaptureCollection captures = matchDXVersion.Groups[1].Captures;
                int i;
                for (i = 0; i < captures.Count; i++)
                {
                    Double dxVersion = Double.Parse(captures[i].Value);
                    if (dxVersion < 10.1) break; // Unsupported version of DirectX is installed.
                }
                if (i == captures.Count) dxVersionOK = true;
            }
            
            // Checking DirectX 'D3D9 Overlay' feature support.
            if (matchDXD3D9Overlay.Success)
            {
                // The first group is always a full matched string so skip it.
                CaptureCollection captures = matchDXD3D9Overlay.Groups[1].Captures;
                int i;
                for (i = 0; i < captures.Count; i++)
                {
                    String overlaySupport = captures[i].Value;
                    // Testing if 'D3D9 Overlay' is not supported by some of the cards.
                    if (!overlaySupport.Equals("Supported")) break;
                }
                if (i == captures.Count) dxOverlaySupportOK = true;
            }

            // Finally checking if everything is OK.
            if (dxVersionOK && dxOverlaySupportOK) return;

            throw ErrorCode.ToException(Error.SCERRINSTALLERDXORVIDEOCARD,
                "Either version of DirectX is lower than 10.1 or 'D3D9 Overlay' functionality is not supported.");
        }

        /// <summary>
        /// Simply checks if Visual Studio 2012 is installed.
        /// </summary>
        /// <returns></returns>
        public static Boolean VStudio2012Installed()
        {
            foreach (var installedEditon in VSIntegration.GetInstalledVSEditionsSupported())
            {
                if (installedEditon.Version.BuildNumber.Equals(VisualStudioVersion.VS2012.BuildNumber))
                    return true;
            }

            return false;
        }
     }
}
