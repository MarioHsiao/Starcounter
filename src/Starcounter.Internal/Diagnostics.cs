
using System;
using System.Diagnostics;
using System.IO;
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

        /// <summary>
        /// Managed callback to handle errors.
        /// </summary>
        /// <param name="err_code"></param>
        /// <param name="err_string"></param>
        public unsafe delegate void ErrorHandlingCallback(
            UInt32 err_code,
            Char* err_string,
            Int32 err_string_len
            );

        public static unsafe ErrorHandlingCallback g_error_handling_callback = ErrorHandlingCallbackFunc;

        /// <summary>
        /// Managed callback to handle errors.
        /// </summary>
        /// <param name="err_code"></param>
        /// <param name="err_string"></param>
        public static unsafe void ErrorHandlingCallbackFunc(
            UInt32 err_code,
            Char* err_string,
            Int32 err_string_len
            )
        {
            String managed_err_string = new String(err_string, 0, err_string_len);
            throw ErrorCode.ToException(err_code, managed_err_string);
        }
    }
}
