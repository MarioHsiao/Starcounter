
using Starcounter.Bootstrap;
using Starcounter.Bootstrap.RuntimeHosts;
using Starcounter.Internal;
using Starcounter.Logging;
using System;

namespace scadminserver
{
    class scadminserver {
        static void Main(string[] args) {
            StarcounterEnvironment.SetInstallationDirectoryFromEntryAssembly();

            var log = new LogSource("Starcounter.AdminServer");
            Diagnostics.WriteTimeStamp(log.Source, "Started scadminserver Main()");

            var host = RuntimeHost.CreateAndAssignToProcess<AdminServerRuntimeHost>(log);

            var config = new CommandLineConfiguration(args);
            host.Run(() => { return config; }, () => { return config.GetAutoExecStart(); });

            Environment.Exit(Environment.ExitCode);
        }
    }
}