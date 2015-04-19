
using Starcounter.Internal;
using Starcounter.Logging;
using StarcounterInternal.Bootstrap;
using System;

namespace scadminserver {
    class scadminserver {
        static void Main(string[] args) {
            var log = new LogSource("Starcounter.AdminServer");
            StarcounterEnvironment.SetInstallationDirectoryFromEntryAssembly();

            Diagnostics.WriteTimeStamp(log.Source, "Started scadminserver Main()");

            Control.ApplicationLogSource = log;
            Control.Main(args);

            Environment.Exit(Environment.ExitCode);
        }
    }
}