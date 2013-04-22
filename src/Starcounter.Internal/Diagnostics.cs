
using System;
using System.Diagnostics;
using System.IO;

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
        public static void WriteTrace(string source, long elapsedTicks, string message)
        {
            string elapsedTime = string.Concat(elapsedTicks / 10000, ".", elapsedTicks % 10000);
            string output = string.Concat(elapsedTime, " ", source, ":", message);
            Trace.WriteLine(output);
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
