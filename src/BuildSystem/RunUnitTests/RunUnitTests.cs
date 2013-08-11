using BuildSystemHelper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RunUnitTests
{
    class RunUnitTests
    {
        /// <summary>
        /// Path to installed NUnit exe.
        /// </summary>
        const String NUnitExePath = @"c:\Program Files (x86)\NUnit 2.6.2\bin\nunit-console.exe";

        /// <summary>
        /// NUnit DLLs exceptions.
        /// </summary>
        static readonly String[] SkipUnitTestsDlls =
        {
            "Skip.Tests.dll"
        };

        static Int32 Main(string[] args)
        {
            try
            {
                // Checking if environment variable is set.
                if ("True" != Environment.GetEnvironmentVariable("SC_RUN_UNIT_TESTS"))
                    return 0;

                // Getting current build configuration.
                String configuration = Environment.GetEnvironmentVariable("Configuration");
                if (configuration == null)
                {
                    throw new Exception("Environment variable 'Configuration' does not exist.");
                }

                // Getting sources directory.
                String sourcesDir = Environment.GetEnvironmentVariable(BuildSystem.CheckOutDirEnvVar);
                if (sourcesDir == null)
                {
                    throw new Exception("Environment variable " + BuildSystem.CheckOutDirEnvVar + " does not exist.");
                }

                String nunitParameters = "";
                if (BuildSystem.IsNightlyBuild())
                    nunitParameters = "/include:performance";

                // Getting the path to current build consolidated folder.
                String outputFolder = Path.Combine(sourcesDir, "Level1\\Bin\\" + configuration);

                // Obtaining all probable unit tests DLLs.
                String[] testsDlls = Directory.GetFiles(outputFolder, "*.tests.dll");
                foreach (String testDll in testsDlls)
                {
                    Boolean skipped = false;
                    foreach (String s in SkipUnitTestsDlls)
                    {
                        if (0 == String.Compare(Path.GetFileName(testDll), s, true))
                        {
                            skipped = true;
                            Console.WriteLine("Skipping unit tests in: " + testDll);
                            break;
                        }
                    }

                    if (skipped)
                        continue;

                    Console.WriteLine("--- Running unit tests in: " + testDll);

                    ProcessStartInfo nunitProcessInfo = new ProcessStartInfo();
                    nunitProcessInfo.FileName = NUnitExePath;
                    nunitProcessInfo.Arguments = testDll + " " + nunitParameters;
                    nunitProcessInfo.UseShellExecute = false;

                    // Starting the NUnit process and waiting for exit.
                    Process msbuildProcess = Process.Start(nunitProcessInfo);
                    msbuildProcess.WaitForExit();

                    if (msbuildProcess.ExitCode != 0)
                    {
                        throw new Exception("Unit tests failed for: " + testDll);
                    }

                    msbuildProcess.Close();
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
