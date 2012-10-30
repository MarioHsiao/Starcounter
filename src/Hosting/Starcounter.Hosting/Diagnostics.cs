
using System;

namespace Starcounter.Hosting
{
    
    /// <summary>
    /// </summary>
    public static class Diagnostics
    {

        /// <summary>
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="elapsedTicks"></param>
        /// <param name="message"></param>
        public static void OutputTrace(string tag, long elapsedTicks, string message)
        {
            string elapsedTime = string.Concat(elapsedTicks / 10000, ".", elapsedTicks % 10000);
            string output = string.Concat(elapsedTime, " ", tag, ":", message);
            Console.WriteLine(output);
        }
    }
}
