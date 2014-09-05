
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Starcounter.Internal
{
    /// <summary>
    /// </summary>
    public static class Diagnostics
    {
        /// <summary>
        /// </summary>
        /// <param name="source"></param>
        /// <param name="elapsedTicks"></param>
        /// <param name="message"></param>
        [Conditional("TRACE")]
        public static void WriteTrace(string source, long elapsedTicks, string message) {
            // Note:
            // If changing this format, make sure also to adapt the
            // corresponding parsing method (TryParseTrace) below.
            string elapsedTime = string.Concat(elapsedTicks / 10000, ".", elapsedTicks % 10000);
            string output = string.Concat(elapsedTime, " ", source, ":", message);
            Trace.WriteLine(output);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static bool TryParseTrace(string message, out string ticks, out string source, out string content) {
            ticks = source = content = null;

            try {
                var delimiter = message.IndexOf(":");
                if (delimiter > 0) {
                    var headers = message.Substring(0, delimiter);
                    content = message.Substring(delimiter + 1);
                    var temp = headers.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    if (temp.Length == 2) {
                        if (temp[0].All((c) => { return char.IsDigit(c) || c == '.'; })) {
                            ticks = temp[0];
                            source = temp[1];
                            return true;
                        }
                    }
                }

            } catch {/*Nope, not ours appearantly.*/}

            return false;
        }

        /// <summary>
        /// Path to time stamp file.
        /// </summary>
        static String TimeStampFilePath_ = Environment.GetEnvironmentVariable("SC_TIMESTAMPS_FILE_PATH");

        /// <summary>
        /// Writes current time
        /// </summary>
        /// <param name="message"></param>
        public static void WriteTimeStamp(String prefix, String message)
        {
            if (null != TimeStampFilePath_)
            {
                lock (TimeStampFilePath_)
                {
                    File.AppendAllText(TimeStampFilePath_,
                        prefix + ": " + message + ": " + DateTime.Now.ToString("hh.mm.ss.fff") + Environment.NewLine);
                }
            }
        }
    }
}
