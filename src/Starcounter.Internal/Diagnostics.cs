
using System;
using System.Diagnostics;

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
    }
}
