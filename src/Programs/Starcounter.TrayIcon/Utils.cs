using Starcounter.Advanced;
using Starcounter.Internal;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Tools {

    /// <summary>
    /// 
    /// </summary>
    public class Utils {


        /// <summary>
        /// Get starcounter system port
        /// </summary>
        /// <param name="port"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public static bool GetPort(out ushort port, out string error) {

            port = 0;
            error = null;

            string file = Path.Combine(
                StarcounterEnvironment.Directories.InstallationConfiguration, 
                StarcounterEnvironment.FileNames.InstallationServerConfigReferenceFile
                );
            string serverDir;

            bool result = ReadConfiguration(file, out serverDir, out error);
            if (result == false) {
                return false;
            }

            string serverConfig = Path.Combine(serverDir, "Personal.server.config");

            result = ReadServerConfiguration(serverConfig, out port, out error);
            if (result == false) {
                return false;
            }

            return true;
        }


        /// <summary>
        /// Get Server folder
        /// </summary>
        /// <param name="file"></param>
        /// <param name="serverDir"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        private static bool ReadConfiguration(string file, out string serverDir, out string error) {

            string result;
            serverDir = null;
            error = null;

            bool success = ReadConfigFile(file, "server-dir", out result, out error);
            if (success) {

                serverDir = result;
                if (!Directory.Exists(serverDir)) {
                    error = string.Format("Invalid server folder {0} ", serverDir);
                    return false;
                }

                return true;
            }
            return false;
        }


        /// <summary>
        /// Get port number from server configuration file
        /// </summary>
        /// <param name="file"></param>
        /// <param name="port"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        private static bool ReadServerConfiguration(string file, out ushort port, out string error) {

            string result;
            port = 0;
            error = null;

            bool success = ReadConfigFile(file, "SystemHttpPort", out result, out error);
            if (success) {
                if (ushort.TryParse(result, out port)) {
                    if (port > IPEndPoint.MaxPort || port < IPEndPoint.MinPort) {
                        error = string.Format("Invalid port number {0}.", port);
                        return false;
                    }
                    return true;
                }
            }
            return false;
        }


        /// <summary>
        /// Get a tag value from a xml configuration file
        /// </summary>
        /// <param name="file"></param>
        /// <param name="tag"></param>
        /// <param name="result"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        private static bool ReadConfigFile(string file, string tag, out string result, out string error) {

            result = null;
            error = null;

            if (!File.Exists(file)) {
                error = string.Format("Missing {0} configuration file.", file);
                return false;
            }

            string[] lines = File.ReadAllLines(file);
            foreach (string line in lines) {

                if (line.StartsWith("//")) continue;



                int startIndex = line.IndexOf("<" + tag + ">", StringComparison.CurrentCultureIgnoreCase);
                if (startIndex != -1) {

                    int len = tag.Length + 2;
                    int endIndex = line.IndexOf("</" + tag + ">", startIndex + len, StringComparison.CurrentCultureIgnoreCase);

                    result = line.Substring(startIndex + len, endIndex - (startIndex + len));
                    return true;
                }
            }

            error = string.Format("Failed to find the <{0}> tag in {1}", tag, file);
            return false;
        }


    }
}
