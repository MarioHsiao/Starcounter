using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CycleTime {
    class CycleTime {

        static String StarcounterBin = Environment.GetEnvironmentVariable("StarcounterBin");

        static void Main(string[] args) {
            ProcessStartInfo processInfo = new ProcessStartInfo();
            processInfo.FileName = "star";
            processInfo.Arguments = @"s\NetworkIoTest\NetworkIoTest.exe";
            processInfo.WorkingDirectory = StarcounterBin;
            processInfo.UseShellExecute = false;
            processInfo.RedirectStandardOutput = true;
            processInfo.RedirectStandardError = true;
            processInfo.CreateNoWindow = true;

            // Starting the process and waiting for exit.
            Console.WriteLine("Starting the process and waiting for exit.");
            using (Process proc = new Process())
            {
                try
                {
                    proc.StartInfo = processInfo;
                    proc.Start();
                    proc.WaitForExit();

                    if (proc.ExitCode != 0)
                    {
                        Console.WriteLine(proc.StandardOutput.ReadToEnd());
                        Console.WriteLine(proc.StandardError.ReadToEnd());
                        Console.WriteLine("Starting star.exe failed with error code: " + proc.ExitCode);
                        Environment.Exit(1);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("CycleTime failed with Exception: " + e.Message);
                    Environment.Exit(1);
                }
            }

            Int32 numberOfStarts = 5;
            Stopwatch sw = new Stopwatch();

            // Running and determining the average.
            Console.WriteLine("Running and determining the average.");
            for (Int32 i = 0; i < numberOfStarts; i++) {
                Console.WriteLine(String.Format("Run No. {0}", i + 1));

                processInfo = new ProcessStartInfo();
                processInfo.FileName = "star";
                processInfo.Arguments = @"s\NetworkIoTest\NetworkIoTest.exe";
                processInfo.WorkingDirectory = StarcounterBin;
                processInfo.UseShellExecute = false;
                processInfo.RedirectStandardOutput = true;
                processInfo.RedirectStandardError = true;
                processInfo.CreateNoWindow = true;

                // Starting the process and waiting for exit.
                using (Process proc = new Process())
                {
                    try
                    {
                        proc.StartInfo = processInfo;

                        sw.Start();
                        proc.Start();
                        proc.WaitForExit();
                        sw.Stop();

                        if (proc.ExitCode != 0)
                        {
                            Console.WriteLine(proc.StandardOutput.ReadToEnd());
                            Console.WriteLine(proc.StandardError.ReadToEnd());
                            Console.WriteLine("Starting star.exe failed with error code: " + proc.ExitCode);
                            Environment.Exit(1);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("CycleTime failed with Exception: " + e.Message);
                        Environment.Exit(1);
                    }
                }
            }

            processInfo = new ProcessStartInfo();
            processInfo.FileName = "staradmin";
            processInfo.Arguments = "kill all";
            processInfo.UseShellExecute = false;
            processInfo.RedirectStandardOutput = true;
            processInfo.RedirectStandardError = true;
            processInfo.CreateNoWindow = true;

            using (Process proc = new Process())
            {
                try
                {
                    proc.StartInfo = processInfo;
                    proc.Start();
                    Console.WriteLine(proc.StandardOutput.ReadToEnd());
                    proc.WaitForExit();

                    if (proc.ExitCode != 0)
                    {
                        Console.WriteLine(proc.StandardError.ReadToEnd());
                        Console.WriteLine("Starting staradmin.exe failed with error code: " + proc.ExitCode);
                        Environment.Exit(1);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("CycleTime failed with Exception: " + e.Message);
                    Environment.Exit(1);
                }
            }

            Console.WriteLine("----------------------------------------------------");
            Console.WriteLine("Time taken for one application start(ms): " + (Double)sw.ElapsedMilliseconds / numberOfStarts);
            Console.WriteLine("----------------------------------------------------");
        }
    }
}
