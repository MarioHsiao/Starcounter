
using Starcounter;
using Starcounter.Bootstrap;
using Starcounter.Internal;
using StarcounterInternal.Bootstrap;

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

            var control = Control.CreateAndInitialize(log);
            
            control.RunUntilExit(() => { return new CommandLineConfiguration(args); });
        }
    }
}
