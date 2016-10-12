
using Starcounter;
using Starcounter.Bootstrap;
using Starcounter.Bootstrap.RuntimeHosts;
using Starcounter.Internal;

namespace sccode
{
    class Program {
        static void Main(string[] args) {
            StarcounterEnvironment.SetInstallationDirectoryFromEntryAssembly();

            var log = LogSources.Hosting;
            Diagnostics.WriteTimeStamp(log.Source, "Started sccode Main()");

            // Coming up:
            // Auto-exec must support an assembly, not necceisarly a path to
            // a file, and also to have it hosted, but not run it's entrypoint.
            // Rationale: self-hosted apps have their entrypoint already cared
            // for.
            // TODO:
            
            var host = RuntimeHost.CreateAndAssignToProcess<AppSharedRuntimeHost>(log);

            host.Run(() => { return new CommandLineConfiguration(args); });
        }
    }
}
