using Starcounter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SiegeTest {

    class Settings {

        public UInt16 ServerPort;
        public String ServerIp;
        public Int32 NumberOfRequests;

        public void Init(string[] args) {

            foreach (String arg in args) {

                if (arg.StartsWith("-ServerIp=")) {
                    
                    ServerIp = arg.Substring("-ServerIp=".Length);

                } else if (arg.StartsWith("-ServerPort=")) {

                    ServerPort = UInt16.Parse(arg.Substring("-ServerPort=".Length));

                } else if (arg.StartsWith("-NumberOfRequests=")) {

                    NumberOfRequests = Int32.Parse(arg.Substring("-NumberOfRequests=".Length));
                }
            }
        }
    }

    class Program {
    
        static Int32 Main(string[] args) {
            
            try {

                Settings settings = new Settings() {
                    ServerPort = 8080,
                    ServerIp = "127.0.0.1",
                    NumberOfRequests = Environment.ProcessorCount - 1
                };

                settings.Init(args);

                // Waiting until host is available.
                Boolean hostIsReady = false;
                Console.Write("Waiting for the host to be ready");

                Response resp;

                for (Int32 i = 0; i < 10; i++) {

                    resp = X.POST("http://" + settings.ServerIp + ":" + settings.ServerPort + "/echotest", "Test!", null, 5000);

                    if ((200 == resp.StatusCode) && ("Test!" == resp.Body)) {

                        hostIsReady = true;
                        break;
                    }

                    Thread.Sleep(3000);
                    Console.Write(".");
                }

                Console.WriteLine();

                if (!hostIsReady)
                    throw new Exception("Host is not ready by some reason!");

                Console.Write("Starting Siege test...");

                ProcessStartInfo processInfo = new ProcessStartInfo();
                processInfo.FileName = "c:\\siege-windows\\siege.exe";
                processInfo.Arguments = "-q -r3000 -b -f /siege-windows/etc/urls.txt";
                processInfo.UseShellExecute = false;
                processInfo.RedirectStandardError = true;

                // Starting the process and waiting for exit.
                Process proc = Process.Start(processInfo);
                String output = proc.StandardError.ReadToEnd();

                proc.WaitForExit();

                if (proc.ExitCode != 0) {
                    throw new Exception("Executing siege.exe failed!");
                }

                proc.Close();

                Console.Write("Siege output: " + Environment.NewLine + output + Environment.NewLine);

                // Checking errors during execution.
                Match match = Regex.Match(output, @"Failed transactions\:\s+(\d+)", RegexOptions.IgnoreCase);

                // Trying to find this exact string in file.
                if ((!match.Success) || (match.Captures.Count != 1) || (match.Groups.Count != 2)) {
                    throw new Exception("Can't find matching string in output: " + output);
                }

                // Checking number of failed transactions.
                Int32 numFailedOperations = Int32.Parse(match.Groups[1].Value);

                if (numFailedOperations != 0) {
                    throw new Exception("Number of failed transactions is non-zero: " + output);
                }

                resp = X.POST("http://" + settings.ServerIp + ":" + settings.ServerPort + "/echotest", "Test!", null, 5000);

                if ((200 != resp.StatusCode) || ("Test!" != resp.Body)) {
                    throw new Exception("Error accessing code-host after Siege run!");
                }

                return 0;

            } catch (Exception exc) {

                Console.WriteLine(exc);

                return 1;
            }            
        }
    }
}
