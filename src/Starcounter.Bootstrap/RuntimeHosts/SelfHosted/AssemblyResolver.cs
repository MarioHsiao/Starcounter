
using Starcounter.Hosting;
using Starcounter.Internal;
using System;
using System.IO;
using System.Reflection;

namespace Starcounter.Bootstrap.RuntimeHosts.SelfHosted
{
    internal class AssemblyResolver : IAssemblyResolver
    {
        readonly string installationDirectory;

        public AssemblyResolver()
        {
            installationDirectory = StarcounterEnvironment.InstallationDirectory;
        }

        Assembly IAssemblyResolver.RegisterApplication(string executablePath, ApplicationDirectory appDirectory)
        {
            // Not really interesting for us. But we could assert the call
            // just reach us once.

            return Assembly.GetEntryAssembly();
        }

        Assembly IAssemblyResolver.ResolveApplicationReference(ResolveEventArgs args)
        {
            var name = new AssemblyName(args.Name);
            var assemblyPath = Path.Combine(installationDirectory, name + ".dll");

            if (File.Exists(assemblyPath))
            {
                return Assembly.LoadFile(assemblyPath);
            }

            return null;
        }
    }
}
