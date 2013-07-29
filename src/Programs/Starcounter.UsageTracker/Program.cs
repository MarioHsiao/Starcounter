using System;
using Starcounter;
using Starcounter.Advanced;
using System.Net.NetworkInformation;
using Starcounter.Internal;
using System.Net;
using Starcounter.Applications.UsageTrackerApp.API.Backend;

namespace Starcounter.Applications.UsageTrackerApp {
    class Program {

        static void Main(string[] args) {

            Console.WriteLine("Starcounter Usage Tracking Software v0.1");

            // We accept to arguments, the first is the port number where the backend gui will answer to,
            // The second parameter is the resource folder (where html/javascript/images/etc... is located)
            if (args.Length == 2) {
                string port = args[0];
                string resourceFolder = args[1];

                ushort uPort;
                bool result = ushort.TryParse(port, out uPort);

                if (result == false || uPort > IPEndPoint.MaxPort || uPort < IPEndPoint.MinPort) {
                    Console.WriteLine("ERROR: Invalid port number");
                    return;
                }

                // Putting port and full path to resources directory.
                String body = port + StarcounterConstants.NetworkConstants.CRLF + resourceFolder;

                // Sending REST POST request to Administrator to register static resources directory.
                Node.LocalhostSystemPortNode.POST("/addstaticcontentdir", body, null, null, null, (Response resp, Object userObject) => {
                    String respString = resp.GetBodyStringUtf8_Slow();
                    if ("Success!" != respString) {
                        throw new Exception("Could not register static resources directory with administrator!");
                    }
                    return "Success!";
                });

                // Bootstrap Backend GUI (system port)
                Administrator.Bootstrap(uPort);

            }

            // Bootstrap Tracking Incoming message
            UsageTrackerAPI.Bootstrap(8585);


        }
    }


}