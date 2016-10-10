
using Starcounter.Bootstrap;
using Starcounter.Internal;
using Starcounter.Logging;
using StarcounterInternal.Bootstrap;
using System;

namespace scadminserver
{
    class scadminserver {
        static void Main(string[] args) {
            StarcounterEnvironment.SetInstallationDirectoryFromEntryAssembly();

            var log = new LogSource("Starcounter.AdminServer");
            Diagnostics.WriteTimeStamp(log.Source, "Started scadminserver Main()");

            var control = Control.CreateAndInitialize(log);

            control.RunUntilExit(() => {
                return new CommandLineConfiguration(args);
            });

            Environment.Exit(Environment.ExitCode);
        }
    }
}