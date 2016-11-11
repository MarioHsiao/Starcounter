
using Starcounter.Hosting;
using Starcounter.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Starcounter.Bootstrap.RuntimeHosts.SelfHosted
{
    internal class AssemblyResolver : IAssemblyResolver
    {
        readonly string installationDirectory;
        ApplicationDirectory appDirectory;

        public AssemblyResolver()
        {
            installationDirectory = StarcounterEnvironment.InstallationDirectory;
            appDirectory = null;
        }
        
        ApplicationDirectory IAssemblyResolver.RegisterApplication(string executablePath)
        {
            Trace.Assert(appDirectory == null);

            var exe = new FileInfo(executablePath);
            appDirectory = new ApplicationDirectory(exe.Directory);

            // Materialize a directory specific for self-hosted apps, that can load
            // "special assemblies" from the cache, and schemas too.
            // TODO:

            return appDirectory;
        }

        Assembly IAssemblyResolver.ResolveApplication(string executablePath)
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

            // Either look in the root application directory, or in the cache
            // it reference. Do this via custom ApplicationDirectory and PrivateBinaries.
            // TODO:

            return null;
        }
    }
}
