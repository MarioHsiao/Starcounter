
using Starcounter.Server;
using System;
using System.Diagnostics;

namespace scservice {

    class Program {

        static void Main(string[] args) {
            var startedAsService = IsStartedAsService(args);
            string serverName = "Personal";
            bool logSteps = false;

            if (args.Length > 2) {
                if (!startedAsService) {
                    Usage();
                }
                return;
            }

            var ignoreCase = StringComparison.InvariantCultureIgnoreCase;
            for (int i = 0; i < args.Length; i++) {
                var arg = args[i];
                if (arg.Equals("--logsteps", ignoreCase)) {
                    logSteps = true;
                } else if (arg.Equals("?") || arg.Equals("-h") || arg.Equals("--help")) {
                    if (!startedAsService) {
                        Usage();
                    }
                    return;
                } else {
                    serverName = arg;
                }
            }

            var serviceProcess = new ServerServiceProcess(serverName) {
                LogSteps = logSteps
            };
            serviceProcess.Launch(startedAsService);
        }

        static bool IsStartedAsService(string[] args) {
            return Process.GetCurrentProcess().SessionId == 0;
        }

        static void Usage() {
            Console.WriteLine("scservice.exe [ServerName] [--logsteps]");
            Console.WriteLine("Example: scservice.exe personal");
            Console.WriteLine("When no ServerName argument is supplied 'personal' is used.");
            Console.WriteLine();
            Console.WriteLine("How it works:");
            Console.WriteLine("scservice will load XML-file called [ServerName].xml");
            Console.WriteLine("from the same directory as scservice.exe and");
            Console.WriteLine("will fetch corresponding server directory from it.");
            Console.WriteLine("From obtained directory it will load [ServerName].config.xml");
            Console.WriteLine("to read server-related settings.");
            Console.WriteLine("scservice will then start and monitor all required");
            Console.WriteLine("Starcounter components, like scnetworkgateway, scipcmonitor, etc.");
        }
    }
}