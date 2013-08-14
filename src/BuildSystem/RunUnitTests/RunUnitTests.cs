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
        /// Excluded NUnit test assemblies.
        /// </summary>
        static readonly String[] SkippedTestAssemblies =
        {
            "Skip.Tests.dll"
        };

        static Int32 Main(string[] args)
        {
            String outputFolder = null;

            if (args.Length > 0)
                outputFolder = args[0];

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
                    nunitParameters = "/include:LongRunning";

                // Getting the path to current build consolidated folder.
                if (null == outputFolder)
                    outputFolder = Path.Combine(sourcesDir, "Level1\\Bin\\" + configuration);

                // Setting StarcounterBin variable.
                Environment.SetEnvironmentVariable(BuildSystem.StarcounterBinVar, Path.Combine(sourcesDir, outputFolder));

                // Obtaining all probable unit tests DLLs.
                String[] testsDlls = Directory.GetFiles(outputFolder, "*.tests.dll");
                String[] testsExes = Directory.GetFiles(outputFolder, "*.tests.exe");

                // Combining tests together.
                String[] allTestAssemblies = testsDlls.Union(testsExes).ToArray();

                foreach (String testAssemblyPath in allTestAssemblies)
                {
                    Boolean skipped = false;
                    foreach (String s in SkippedTestAssemblies)
                    {
                        if (0 == String.Compare(Path.GetFileName(testAssemblyPath), s, true))
                        {
                            skipped = true;
                            Console.WriteLine("Skipping unit tests in: " + testAssemblyPath);
                            break;
                        }
                    }

                    if (skipped)
                        continue;

                    Console.WriteLine("--- Running unit tests in: " + testAssemblyPath);

                    ProcessStartInfo nunitProcessInfo = new ProcessStartInfo();
                    nunitProcessInfo.FileName = NUnitExePath;
                    nunitProcessInfo.Arguments = testAssemblyPath + " " + nunitParameters;
                    nunitProcessInfo.UseShellExecute = false;

                    // Starting the NUnit process and waiting for exit.
                    Process nunitProcess = Process.Start(nunitProcessInfo);
                    nunitProcess.WaitForExit();

                    if (nunitProcess.ExitCode != 0)
                    {
                        throw new Exception("Unit tests failed for: " + testAssemblyPath);
                    }

                    nunitProcess.Close();
                }

                String[] nativeTestsExes = Directory.GetFiles(outputFolder, "*_unitests.exe");

                foreach (String testAssemblyPath in nativeTestsExes)
                {
                    Boolean skipped = false;
                    foreach (String s in SkippedTestAssemblies)
                    {
                        if (0 == String.Compare(Path.GetFileName(testAssemblyPath), s, true))
                        {
                            skipped = true;
                            Console.WriteLine("Skipping unit tests in: " + testAssemblyPath);
                            break;
                        }
                    }

                    if (skipped)
                        continue;

                    Console.WriteLine("--- Running unit tests in: " + testAssemblyPath);

                    ProcessStartInfo testProcessInfo = new ProcessStartInfo();
                    testProcessInfo.FileName = testAssemblyPath;
                    testProcessInfo.UseShellExecute = false;

                    // Starting the test process and waiting for exit.
                    Process testProcess = Process.Start(testProcessInfo);

                    if ((!testProcess.WaitForExit(5000)) || (testProcess.ExitCode != 0))
                    {
                        throw new Exception("Unit tests failed for: " + testAssemblyPath);
                    }

                    testProcess.Close();
                }

                Console.WriteLine("--- All unit tests succeeded!");

                return 0;
            }
            catch (Exception generalException)
            {
                return BuildSystem.LogException(generalException);
            }
        }
    }
}
