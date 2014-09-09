
using Starcounter;
using Starcounter.Internal;
using StarcounterInternal.Bootstrap;
using System;
using System.IO;

namespace sccode {
    class Program {
        static void Main(string[] args) {
            var log = LogSources.Hosting;

            StarcounterEnvironment.SetInstallationDirectoryFromEntryAssembly();

            Diagnostics.WriteTimeStamp(log.Source, "Started sccode Main()");

            //Trace.Listeners.Add(new ConsoleTraceListener());
            Control.ApplicationLogSource = log;
            Control.Main(args);
        }
    }
}
