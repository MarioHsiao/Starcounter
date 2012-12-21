
using System.Diagnostics;

namespace sccode
{
    class Program
    {
        
        static void Main(string[] args)
        {
            //Trace.Listeners.Add(new ConsoleTraceListener());
            StarcounterInternal.Bootstrap.Control.Main(args);
        }
    }
}
