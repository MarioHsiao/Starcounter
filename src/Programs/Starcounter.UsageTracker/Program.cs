using System;
using Starcounter;
using Starcounter.Advanced;
using System.Net.NetworkInformation;
using Starcounter.Internal;
using System.Net;
using System.IO;

namespace Starcounter.Applications.UsageTrackerApp {
    class Program {

        static void Main(string[] args) {

            Console.WriteLine("Starcounter Usage Tracking Software v0.3");

            // We accept to arguments, the first is the port number where the backend gui will answer to,
            // The second parameter is the resource folder (where html/javascript/images/etc... is located)
            if (args.Length == 2) {
                string port = args[0];
                string resourceFolder = args[1];

                ushort uPort;
                bool result = ushort.TryParse(port, out uPort);

                if (result == false || uPort > IPEndPoint.MaxPort || uPort < IPEndPoint.MinPort) {
                    throw new IndexOutOfRangeException("Invalid port number");
                }
                
                AppsBootstrapper.AddStaticFileDirectory(uPort, Path.GetFullPath(resourceFolder));

                Utils.AssureIndexes();

                // use TrackingEnvironment.StarcounterTrackerPort
                // Bootstrap Tracking Incoming message
                UsageTrackerAPI.Bootstrap(8585, 8282, 80, resourceFolder);
            }
        }
    }
}