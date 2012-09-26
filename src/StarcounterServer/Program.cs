
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.ServiceProcess;

namespace StarcounterServer {

    class Server : ServiceBase {
        static Server server;
        
        static void Main(string[] args) {
            server = new Server();

            if (Environment.UserInteractive) {
                // The server is executed either as a console program, or a
                // Windows application, depending on how it was built.
                Console.CancelKeyPress += Console_CancelKeyPress;
                server.OnStart(args);
                Console.WriteLine("Press CTRL+C to exit...");
                Thread.Sleep(Timeout.Infinite);

            } else {
                // The server is ran as a service.
                ServiceBase.Run(server);
            }
        }

        protected override void OnStart(string[] args) {
            // The server is starting.
        }

        protected override void OnStop() {
            // The server is stopping.
        }

        static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e) {
            server.OnStop();
            Environment.Exit(0);
        }
    }
}