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
        /// Lock object when saving statistics to file.
        /// </summary>
        static String lockObject_ = "SomeString";

        /// <summary>
        /// Date time of last written statistics.
        /// </summary>
        static DateTime lastWrittenStatististicsTime_ = DateTime.Now;

        /// <summary>
        /// Minimum time between two file writes.
        /// </summary>
        const Int32 MinStatsWriteIntervalSeconds = 2;

        /// <summary>
        /// Saving current statistics to file.
        /// </summary>
        public static void SaveStatisticsToFile(String testName) {

            // Checking if we are running on the build server.
            if (!TestLogger.IsRunningOnBuildServer())
                return;

            String statFileName = TestLogger.GetBuildNumber() + ".txt";

            if (TestLogger.IsPersonalBuild())
                statFileName = "personal-" + statFileName;

            if (!TestLogger.IsReleaseBuild())
                statFileName = "debug-" + statFileName;

            statFileName = testName + "-" + statFileName;

            String dirToStatistics = TestLogger.MappedBuildServerFTPDrive + @"\SCDev\BuildSystem\BuildStatistics\TestStats";

            var json = new ClientStatsJson();
            json.TestStats = Db.SQL("SELECT s FROM " + typeof(ClientStatsEntry).Name + " s");

            String jsonStats = json.ToJson();
            String pathToStatsFile = Path.Combine(dirToStatistics, statFileName);

            lock (lockObject_) {

                // Checking if MinStatsFileWriteInterval seconds passed since last time we wrote statistics to file.
                if ((DateTime.Now - lastWrittenStatististicsTime_).TotalSeconds < MinStatsWriteIntervalSeconds) {
                    return;
                }

                if (!Directory.Exists(dirToStatistics)) {
                    Directory.CreateDirectory(dirToStatistics);
                }

                // We need to lock because two threads might do this at the same time.
                File.WriteAllText(pathToStatsFile, jsonStats);

                // Taking current time when statistics was written.
                lastWrittenStatististicsTime_ = DateTime.Now;
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

                // Saving statistics to file.
                SaveStatisticsToFile(testName);

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
