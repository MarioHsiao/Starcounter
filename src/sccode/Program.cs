
using Starcounter;
using Starcounter.Bootstrap;
using Starcounter.Bootstrap.RuntimeHosts;
using Starcounter.Bootstrap.RuntimeHosts.Shared;
using Starcounter.Internal;

namespace sccode
{
    class Program {

        static void Main(string[] args) {
            StarcounterEnvironment.SetInstallationDirectoryFromEntryAssembly();

            var log = LogSources.Hosting;
            Diagnostics.WriteTimeStamp(log.Source, "Started sccode Main()");

            var host = RuntimeHost.CreateAndAssignToProcess<AppSharedRuntimeHost>(log);

            var config = new CommandLineConfiguration(args);
            host.Run(() => { return config; }, () => { return config.GetAutoExecStart(); });
        }
    }
}