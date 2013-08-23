using BuildSystemHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PreBuildTasks
{
    class PreBuildTasks
    {
        static Int32 Main(string[] args)
        {
            try
            {
                if (File.Exists(BuildSystem.BuildStatisticsFilePath))
                {
                    Console.WriteLine("Deleting existing build statistics file...");
                    File.Delete(BuildSystem.BuildStatisticsFilePath);
                }

                // Getting current build version.
                String version = Environment.GetEnvironmentVariable(BuildSystem.BuildNumberEnvVar);
                if (version == null)
                {
                    throw new Exception("Environment variable 'BUILD_NUMBER' does not exist.");
                }

                // Getting sources directory.
                String checkoutDir = Environment.GetEnvironmentVariable(BuildSystem.CheckOutDirEnvVar);
                if (checkoutDir == null)
                {
                    throw new Exception("Environment variable " + BuildSystem.CheckOutDirEnvVar + " does not exist.");
                }

                // Looking for an Installer WPF resources folder.
                String installerWpfFolder = Path.Combine(checkoutDir, @"Level1\src\Starcounter.Installer\Starcounter.InstallerWPF");

                // Replacing version information.
                String currentVersionFilePath = Path.Combine(checkoutDir, @"Level1\src\Starcounter.Internal\Constants\CurrentVersion.cs");
                BuildSystem.ReplaceStringInFile(currentVersionFilePath, @"String Version = ""[0-9\.]+"";", "String Version = \"" + version + "\";");

                String installerWrapperDir = Path.Combine(checkoutDir, @"Level1\src\Starcounter.Installer\Starcounter.InstallerNativeWrapper");

                // Setting current installer version.
                BuildSystem.ReplaceStringInFile(Path.Combine(installerWrapperDir, "Starcounter.InstallerNativeWrapper.cpp"),
                    @"wchar_t\* ScVersion = L""[0-9\.]+"";", "wchar_t* ScVersion = L\"" + version + "\";");

                // Replacing unique installer version string.
                BuildSystem.ReplaceStringInFile(Path.Combine(installerWpfFolder, "App.xaml.cs"), @"String ScVersion = ""[0-9\.]+"";", "String ScVersion = \"" + version + "\";");

                return 0;
            }
            catch (Exception generalException)
            {
                return BuildSystem.LogException(generalException);
            }
        }
    }
}
