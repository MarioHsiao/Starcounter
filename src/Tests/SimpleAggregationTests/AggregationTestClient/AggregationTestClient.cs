using Starcounter;
using Starcounter.Internal;
using System;
using System.Diagnostics;

namespace AggregationTestClient {

    class Program {

        /// <summary>
        /// Do aggregated GET calls.
        /// </summary>
        static void DoAggregatedGet(
            String testName,
            String hostName,
            UInt16 uriPort, 
            UInt16 aggrPort, 
            String uri, 
            Int32 numCalls) {

            Console.WriteLine("Starting test: " + testName);

            AggregationClient aggrClient = new AggregationClient(hostName, uriPort, aggrPort);

            Stopwatch sw = Stopwatch.StartNew();

            Int32 c = 0;
            Int32 successfullResps = 0;
            Int32 failedResps = 0;

            for (Int32 i = 0; i < numCalls; i++) {

                aggrClient.Send("GET", uri, null, null, (Response resp) => {

                    if (resp.IsSuccessStatusCode)
                        successfullResps++;
                    else
                        failedResps++;
                });

                c++;
                if (c == 100000) {
                    Console.WriteLine("Initiated: {0}, successfull: {1}, failed: {2}", i, successfullResps, failedResps);
                    aggrClient.SendStatistics(testName, successfullResps, failedResps);
                    c = 0;
                }
            }

            sw.Stop();

            Int32 rps = (Int32) (numCalls / (sw.ElapsedMilliseconds / 1000.0));

            aggrClient.Shutdown();

            Console.WriteLine("##teamcity[buildStatisticValue key='{0}' value='{1}']", testName, rps);
        }

        static Int32 Main(string[] args) {

            // Starting server side application.
            Int32 exitCode = -1;
            try {
                Console.WriteLine("Starting server side code...");
                exitCode = Diagnostics.StartProcessAndWaitForExit("star.exe", "..\\..\\..\\AggregationTestServer.cs", 30000);
            } catch (Exception exc) {
                Console.WriteLine(exc.ToString());
            }

            if (exitCode != 0) {
                Console.WriteLine("Can't properly start test server side code.");
                return 1;
            }

            DoAggregatedGet("AggregatedSimpleCodehostEchoTest", "localhost", 8080, 9191, "/test", 1000000);

            DoAggregatedGet("AggregatedSimpleGatewayEchoTest", "localhost", 8181, 9191, "/gw/test", 1000000);

            DoAggregatedGet("AggregatedSimpleCodehostEchoTest", "localhost", 8181, 9191, "/test", 1000000);

            return 0;
        }
    }
}