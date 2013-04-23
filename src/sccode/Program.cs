
using Starcounter.Internal;
using System;
using System.IO;

namespace sccode
{
    class Program
    {
        static void Main(string[] args)
        {
            Diagnostics.WriteTimeStamp("SCCODE", "Started sccode Main()");

            //Trace.Listeners.Add(new ConsoleTraceListener());
            StarcounterInternal.Bootstrap.Control.Main(args);
        }
    }
}
