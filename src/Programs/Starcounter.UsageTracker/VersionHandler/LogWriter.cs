using StarcounterApplicationWebSocket.VersionHandler.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarcounterApplicationWebSocket.VersionHandler {
    internal class LogWriter {

        private static Object LOCK = new Object();

        private static string LogFile;

        /// <summary>
        /// Initilize the logwriter
        /// </summary>
        /// <param name="file">Filename</param>
        public static void Init(string file) {
            LogWriter.LogFile = file;
        }

        /// <summary>
        /// Write to logfile
        /// </summary>
        /// <param name="text"></param>
        public static void WriteLine(string text) {

            lock (LOCK) {

                if (string.IsNullOrEmpty(text)) return;

                // Write to console
                Console.WriteLine(text);

                if (string.IsNullOrEmpty(LogWriter.LogFile)) return;

                string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");


                try {
                    // This text is added only once to the file. 
                    if (!File.Exists(LogWriter.LogFile)) {
                        // Create a file to write to. 
                        using (StreamWriter sw = File.CreateText(LogWriter.LogFile)) {
                            sw.WriteLine(timestamp + " " + text);
                        }
                        return;
                    }

                    // This text is always added, making the file longer over time 
                    // if it is not deleted. 
                    using (StreamWriter sw = File.AppendText(LogWriter.LogFile)) {
                        sw.WriteLine(timestamp + " " + text);
                    }
                }
                catch (Exception e) {
                    Console.WriteLine("{0} ERROR: Failed to write to log {0}. {1}.", timestamp, LogWriter.LogFile, e.ToString());
                }

            }
        }

    }
}
