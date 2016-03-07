using Codeplex.Data;
using Starcounter.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

    [Database]
    public class ProcessHardwareStatsEntry { // Statistics entry for hardware.
        public String ServerName; // Name of this server.
        public String StatTime; // Datetime when statistics received.
        public String ProcessName; // Name of the process.
        public Int32 MemoryPrivateWorkingSetKiB; // Memory private working set in KiB.
        public Int32 CpuUsagePercent; // CPU usage in percents.
    }

    [Database]
    public class SystemHardwareStatsEntry { // Statistics entry for hardware.
        public String ServerName; // Name of this server.
        public String StatTime; // Datetime when statistics received.
        public Int32 NetworkUsageMbitSec; // Network usage in mbit/sec.
    }

    /// <summary>
    /// Class that supports test statistics.
    /// </summary>
    public class TestStatistics {

        static double GetNetworkUtilization(string networkCard) {

            const int numberOfIterations = 10;

            using (PerformanceCounter bandwidthCounter = new PerformanceCounter("Network Interface", "Current Bandwidth", networkCard)) {
                float bandwidth = bandwidthCounter.NextValue();

                using (PerformanceCounter dataSentCounter = new PerformanceCounter("Network Interface", "Bytes Sent/sec", networkCard)) {

                    using (PerformanceCounter dataReceivedCounter = new PerformanceCounter("Network Interface", "Bytes Received/sec", networkCard)) {

                        float sendSum = 0;
                        float receiveSum = 0;

                        for (int index = 0; index < numberOfIterations; index++) {
                            sendSum += dataSentCounter.NextValue();
                            receiveSum += dataReceivedCounter.NextValue();
                        }
                        float dataSent = sendSum;
                        float dataReceived = receiveSum;

                        double utilization = (8 * (dataSent + dataReceived)) / (bandwidth * numberOfIterations) * 100;
                        return utilization;
                    }
                }
            }
        }

        static Timer hardwareStatsTimer_;

        readonly static String[] ScProcessNames = {
            StarcounterConstants.ProgramNames.ScCode,
            StarcounterConstants.ProgramNames.ScData,
            StarcounterConstants.ProgramNames.ScNetworkGateway,
            StarcounterConstants.ProgramNames.ScDbLog
        };

        static void CollectHardwareStats(Object state) {

            lock (hardwareStatsTimer_) {

                Stopwatch sw = Stopwatch.StartNew();

                String curTime = DateTime.Now.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");

                // Iterating through each process we need to get hardware counters.
                foreach (String procName in ScProcessNames) {

                    using (PerformanceCounter procStats = new PerformanceCounter("Process", "% Processor Time", procName)) {

                        using (PerformanceCounter memStats = new PerformanceCounter("Process", "Working Set - Private", procName)) {

                            Scheduling.ScheduleTask(() => {
                                Db.Transact(() => {

                                    float mem = memStats.NextValue();
                                    float procTime = procStats.NextValue();

                                    ProcessHardwareStatsEntry e = new ProcessHardwareStatsEntry() {
                                        ServerName = Environment.MachineName,
                                        StatTime = curTime,
                                        ProcessName = procName,
                                        MemoryPrivateWorkingSetKiB = Convert.ToInt32(Convert.ToInt64(mem) / 1024),
                                        CpuUsagePercent = Convert.ToInt32(procTime)
                                    };

                                });
                            });
                        }
                    }
                }

                // Saving current network statistics.
                PerformanceCounterCategory category = new PerformanceCounterCategory("Network Interface");
                String[] networkInterfaceName = category.GetInstanceNames();
                double totalNetworkUsage = 0;
                foreach (String name in networkInterfaceName) {
                    totalNetworkUsage += GetNetworkUtilization(name);
                }

                Scheduling.ScheduleTask(() => {
                    Db.Transact(() => {
                        SystemHardwareStatsEntry e = new SystemHardwareStatsEntry() {
                            ServerName = Environment.MachineName,
                            StatTime = curTime,
                            NetworkUsageMbitSec = Convert.ToInt32(totalNetworkUsage * 8 / 1000000.0)
                        };
                    });

                });

                sw.Stop();
            }

        }

        /// <summary>
        /// Enables test statistics.
        /// </summary>
        public static void EnableTestStatistics() {

            Db.Transact(() => {
                Db.SlowSQL("DELETE FROM ClientStatsEntry");
                Db.SlowSQL("DELETE FROM ProcessHardwareStatsEntry");
                Db.SlowSQL("DELETE FROM SystemHardwareStatsEntry");

            });

            // Handler that adds statistics from client.
            if (!Handle.IsHandlerRegistered("GET /TestStats/addstats?TestName={?}&NumOk={?}&NumFailed={?}", null)) {

                Handle.GET("/TestStats/AddStats?TestName={?}&NumOk={?}&NumFailed={?}",
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
            }

            // Getting all clients statistics.
            Handle.GET("/TestStats", () => {

                var json = new ClientStatsJson();
                json.TestStats = Db.SQL("SELECT s FROM ClientStatsEntry s");

                return new Response() {
                    BodyBytes = json.ToJsonUtf8()
                };
            });

            // Getting all clients statistics.
            Handle.GET("/HardwareStats", () => {

                List<Json> allHardwareStats = new List<Json>();

                dynamic hardwareStats = new DynamicJson();

                foreach (SystemHardwareStatsEntry se in Db.SQL("SELECT s FROM SystemHardwareStatsEntry s")) {

                    hardwareStats.ServerName = se.ServerName;
                    hardwareStats.StatTime = se.StatTime;
                    hardwareStats.NetworkUsageMbitSec = se.NetworkUsageMbitSec;

                    List<Json> hhhh = new List<Json>();
                    foreach (ProcessHardwareStatsEntry e in Db.SQL("SELECT s FROM ProcessHardwareStatsEntry s WHERE s.StatTime = ?", se.StatTime)) {

                        dynamic procStat = new DynamicJson();
                        procStat.ProcessName = e.ProcessName;
                        procStat.MemoryPrivateWorkingSetKiB = e.MemoryPrivateWorkingSetKiB;
                        procStat.CpuUsagePercent = e.CpuUsagePercent;

                        hhhh.Add(procStat);
                    }

                    hardwareStats.StatsForScProcesses = hhhh;
                }

                dynamic all = new DynamicJson();
                all.AllList = allHardwareStats;

                return new Response() {
                    BodyBytes = all.ToJsonUtf8()
                };
            });

            hardwareStatsTimer_ = new Timer(CollectHardwareStats, null, 5000, 5000);
        }
    }
}
