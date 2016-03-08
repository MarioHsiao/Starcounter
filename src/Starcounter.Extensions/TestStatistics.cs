using Codeplex.Data;
using Starcounter.Internal;
using Starcounter.TestFramework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Starcounter.Extensions {

    [Database]
    public class ClientStatsEntry { // Statistics entry from the client.
        public String ServerName; // Name of this server.
        public String TestName; // Name of the test.
        public String ReceivedTime; // Datetime when statistics received.
        public String ClientIp; // Client IP address.
        public Int32 NumFail; // Number of failed operations since last report.
        public Int32 NumOk; // Number of successful operations since last report.
    }

    /// <summary>
    /// Class that supports test statistics.
    /// </summary>
    public class TestStatistics {

        static double GetNetworkUtilizationMBitSec(string networkCard) {

            const Int32 NumberOfIterations = 10;

            using (PerformanceCounter dataSentCounter = new PerformanceCounter("Network Interface", "Bytes Sent/sec", networkCard)) {

                using (PerformanceCounter dataReceivedCounter = new PerformanceCounter("Network Interface", "Bytes Received/sec", networkCard)) {

                    float sendSum = 0;
                    float receiveSum = 0;

                    for (Int32 i = 0; i < NumberOfIterations; i++) {
                        sendSum += dataSentCounter.NextValue();
                        receiveSum += dataReceivedCounter.NextValue();
                    }

                    // Megabits per second.
                    return ((8 * (sendSum + receiveSum)) / NumberOfIterations) / 1000000.0;
                }
            }
        }

        /// <summary>
        /// Enables test statistics.
        /// </summary>
        public static void EnableTestStatistics() {

            // Checking if handlers are already registered.
            if (Handle.IsHandlerRegistered("GET /TestStats", null)) {
                return;
            }

            Db.Transact(() => {
                Db.SlowSQL("DELETE FROM " + typeof(ClientStatsEntry).Name);
            });

            // Handler that adds statistics from client.
            Handle.GET(StatisticsConstants.StatsUriWithParams.Replace(@"{0}", "{?}").Replace(@"{1}", "{?}").Replace(@"{2}", "{?}"),
            (Request req, String testName, String numOk, String numFail) => {

                Db.Transact(() => {

                    ClientStatsEntry cs = new ClientStatsEntry() {
                        ServerName = Environment.MachineName,
                        TestName = testName,
                        ReceivedTime = DateTime.Now.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'"),
                        ClientIp = req.ClientIpAddress.ToString(),
                        NumFail = Convert.ToInt32(numFail),
                        NumOk = Convert.ToInt32(numOk)
                    };

                });

                return 204;
            });

            // Getting all clients statistics.
            Handle.GET("/TestStats", () => {

                var json = new ClientStatsJson();
                json.TestStats = Db.SQL("SELECT s FROM " + typeof(ClientStatsEntry).Name + " s");

                return new Response() {
                    BodyBytes = json.ToJsonUtf8()
                };
            });
        }
    }
}
