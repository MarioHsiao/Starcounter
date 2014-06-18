using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CycleTime {
    class Program {
        static void Main(string[] args) {
            ProcessStartInfo processInfo = new ProcessStartInfo();
            processInfo.FileName = "star";
            processInfo.Arguments = @"s\NetworkIoTest\NetworkIoTest.exe";
            processInfo.UseShellExecute = false;

            // Starting the process and waiting for exit.
            Process proc = Process.Start(processInfo);
            proc.WaitForExit();

            if (proc.ExitCode != 0) {
                throw new Exception("Starting star.exe failed!");
            }

            proc.Close();

            Int32 numberOfStarts = 5;
            Stopwatch sw = new Stopwatch();

            // Running and determining the average.
            for (Int32 i = 0; i < numberOfStarts; i++) {
                processInfo = new ProcessStartInfo();
                processInfo.FileName = "star";
                processInfo.Arguments = @"s\NetworkIoTest\NetworkIoTest.exe";
                processInfo.UseShellExecute = false;

                // Starting the process and waiting for exit.
                sw.Start();
                proc = Process.Start(processInfo);
                proc.WaitForExit();
                sw.Stop();

                if (proc.ExitCode != 0) {
                    throw new Exception("Starting star.exe failed!");
                }

                proc.Close();
            }

            processInfo = new ProcessStartInfo();
            processInfo.FileName = "staradmin";
            processInfo.Arguments = "kill all";
            processInfo.UseShellExecute = false;

            proc = Process.Start(processInfo);
            proc.WaitForExit();

            if (proc.ExitCode != 0) {
                throw new Exception("Starting staradmin.exe failed!");
            }

            proc.Close();

            Console.WriteLine("----------------------------------------------------");
            Console.WriteLine("Time taken for one application start(ms): " + (Double)sw.ElapsedMilliseconds / numberOfStarts);
            Console.WriteLine("----------------------------------------------------");
        }
    }
}
