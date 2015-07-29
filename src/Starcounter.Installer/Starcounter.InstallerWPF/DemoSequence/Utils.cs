using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace Starcounter.InstallerWPF.DemoSequence
{
    public class Utils
    {
        public static string GetVisualStudioProjectAssemblyName(string file)
        {
            string result = string.Empty;

            if (File.Exists(file))
            {
                // <AssemblyName>Benchmark-Tester</AssemblyName>
                using (TextReader textReader = new StreamReader(file))
                {

                    string content = textReader.ReadToEnd();
                    textReader.Close();

                    string searchPattern = @"<AssemblyName>(?<name>\S+)</AssemblyName>";

                    Regex re = new Regex(searchPattern);
                    Match match = re.Match(content);
                    if (match.Success)
                    {
                        result = match.Groups["name"].Value;
                    }
                }
            }
            return result;
        }

        /*

        .NET app compiled for "x86":
            Always 32-bit
            On 32-bit platforms, accesses 32-bit registry
            On 64-bit platforms, accesses 32-bit registry (inside Wow6432Node)
            64-bit registry inaccessible (without doing something weird)

         .NET app compiled for "x64":
            Always 64 bit
            On 32-bit platforms, won't run
            On 64-bit platforms, accesses 64-bit registry (not inside Wow6432Node)
            If you want to get to the 32-bit registry, you need to do something weird

         .NET app compiled for "AnyCpu"
            Either 32 or 64 bit depending on platform
            On 32-bit platforms, accesses 32-bit registry
            On 64-bit platforms, accesses 64-bit registry (not inside Wow6432Node)
                If you want to get to the 32-bit registry, you need to do something weird

        */
        public static string GetVisualStudioExePath(VisualStudioVersion version)
        {

            try
            {

                // Visual Studio is a 32bit program
                // Use the 32-bit (WOW) registry 
                RegistryKey registryBase32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);

                switch (version)
                {
                    case VisualStudioVersion.VS2008:
                    {
                        using (RegistryKey regKey = registryBase32.OpenSubKey(@"SOFTWARE\Microsoft\VisualStudio\9.0\Setup\VS"))
                        {
                            if (regKey != null)
                            {
                                object result = regKey.GetValue("VS7EnvironmentLocation");
                                regKey.Close();

                                if (result != null && !string.IsNullOrEmpty(result.ToString()))
                                {
                                    return result.ToString();
                                }
                            }

                        }
                        break;
                    }

                    case VisualStudioVersion.VS2010:
                    {
                        using (RegistryKey regKey = registryBase32.OpenSubKey(@"SOFTWARE\Microsoft\VisualStudio\10.0\Setup\VS"))
                        {
                            if (regKey != null)
                            {
                                object result = regKey.GetValue("VS7EnvironmentLocation");
                                regKey.Close();

                                if (result != null && !string.IsNullOrEmpty(result.ToString()))
                                {
                                    return result.ToString();
                                }
                            }

                        }
                        break;
                    }

                    case VisualStudioVersion.VS2012:
                    {
                        using (RegistryKey regKey = registryBase32.OpenSubKey(@"SOFTWARE\Microsoft\VisualStudio\11.0\Setup\VS"))
                        {
                            if (regKey != null)
                            {
                                object result = regKey.GetValue("VS7EnvironmentLocation");
                                regKey.Close();

                                if (result != null && !string.IsNullOrEmpty(result.ToString()))
                                {
                                    return result.ToString();
                                }
                            }
                        }
                        break;
                    }
                }
            }
            catch (Exception)
            {
                // Ignore exceptions.
            }

            return string.Empty;

            // For VS2008 you can use VS90COMNTOOLS

        }

    }

    public enum VisualStudioVersion
    {
        VS2008,
        VS2010,
        VS2012,
        VS2013,
        VS2015
    }

    public enum PostDemoTypeEnum
    {
        PREBUILT,
        VS2008,
        VS2010,
        VS2012,
        VS2013,
        VS2015
    }
}
