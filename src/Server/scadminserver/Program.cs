
using Starcounter.Internal;
using StarcounterInternal.Bootstrap;
using System;

namespace scadminserver {
    class Program {
        static void Main(string[] args) {
            Diagnostics.WriteTimeStamp("SCADMINSERVER", "Started scadminserver Main()");
            Control.Main(args);
            Environment.Exit(Environment.ExitCode);
        }
    }
}