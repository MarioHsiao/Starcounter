
using System;

namespace Starcounter.Hosting
{
    
    public static class Diagnostics
    {

        public static void OutputTrace(string tag, long elapsedTicks, string message)
        {
            string elapsedTime = string.Concat(elapsedTicks / 10000, ".", elapsedTicks % 10000);
            string output = string.Concat(elapsedTime, " ", tag, ":", message);
            Console.WriteLine(output);
        }
    }
}
