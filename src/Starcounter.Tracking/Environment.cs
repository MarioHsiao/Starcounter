
using Starcounter.Internal;
using System;
using System.Management;
using System.Net.NetworkInformation;

namespace Starcounter.Tracking {
    /// <summary>
    /// Usage Tracking Environment
    /// TODO: This should maybe be moved to Starcounter Environment
    /// </summary>
    public class Environment {

        private static string cpuInfo = null;
        private static string osInfo = null;
        private static UInt64 mem = 0;

        /// <summary>
        /// The starcounter tracker server IP address
        /// </summary>
        //public const string StarcounterTrackerIp = "208.52.190.89"; // TODO: Atlanta, use DNS
        public const string StarcounterTrackerIp = "127.0.0.1"; // TODO: Atlanta, use DNS

        /// <summary>
        /// The starcounter tracker server port
        /// </summary>
        public const ushort StarcounterTrackerPort = 8585;

        /// <summary>
        /// Get OS information
        /// </summary>
        /// <returns>String with the OS information</returns>
        public static string GetOSFriendlyName() {

            if (Environment.osInfo != null) return Environment.osInfo;

            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem");
            foreach (ManagementObject os in searcher.Get()) {
                Environment.osInfo = os["Caption"].ToString();
                return Environment.osInfo;
            }
            Environment.osInfo = string.Empty;
            return Environment.osInfo;
        }

        /// <summary>
        /// Get Installed RAM in MBytes
        /// </summary>
        /// <returns></returns>
        public static UInt64 GetInstalledRAM() {

            if (Environment.mem > 0) return Environment.mem;

            UInt64 mem = 0;
            using (ManagementObjectSearcher win32Proc = new ManagementObjectSearcher("SELECT Capacity FROM Win32_PhysicalMemory")) {
                foreach (ManagementObject obj in win32Proc.Get()) {
                    mem += (UInt64)obj["Capacity"];
                }
            }
            Environment.mem = mem / 1024 / 1024; // In MBytes
            return Environment.mem;

        }

        /// <summary>
        /// Get CPU information
        /// </summary>
        /// <returns></returns>
        public static string GetCPUFriendlyName() {

            if (Environment.cpuInfo != null) return Environment.cpuInfo;


            using (ManagementObjectSearcher win32Proc = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor")) {
                foreach (ManagementObject obj in win32Proc.Get()) {
                    Environment.cpuInfo = obj["Name"].ToString();
                    return Environment.cpuInfo;
                }
            }

            Environment.cpuInfo = string.Empty;
            return Environment.cpuInfo;
        }

        /// <summary>
        /// Get Truncated MacAddress
        /// </summary>
        /// <returns></returns>
        public static string GetTruncatedMacAddress() {

            PhysicalAddress mac = Environment.GetMachineMacAddress();

            string macStr = mac.ToString(); //  08606E75DEFA

            int index = macStr.Length - 6;  // Last 6 chars   
            if (index < 0) {
                index = 0;
            }
            return macStr.Substring(index);
        }

        /// <summary>
        /// Get this Machine first available MacAddress
        /// </summary>
        /// <returns></returns>
        public static PhysicalAddress GetMachineMacAddress() {
            if (!NetworkInterface.GetIsNetworkAvailable()) {
                return PhysicalAddress.None;
            }

            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface networkInterface in networkInterfaces) {

                if (networkInterface.OperationalStatus == OperationalStatus.Up) {
                    return networkInterface.GetPhysicalAddress();
                }
            }

            return PhysicalAddress.None;
        }


        /// <summary>
        /// Save installation sequence number to starcounter application
        /// </summary>
        /// <param name="no">Installation Sequence number</param>
        public static void SaveInstallationNo(int no) {

            Properties.Settings.Default.installationNo = no;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// Get the starcounter application installation sequence number
        /// </summary>
        public static int GetInstallationNo() {
            return Properties.Settings.Default.installationNo;
        }

    }
}
