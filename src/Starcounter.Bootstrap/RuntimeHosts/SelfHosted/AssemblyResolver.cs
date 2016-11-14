
using Starcounter.Hosting;
using Starcounter.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
            var cachePath = Path.Combine(exe.Directory.FullName, ".starcounter", "cache");
            var weaverCache = new DirectoryInfo(cachePath);

            // This is not a long-term solution and one we must eventually address.
            // The only idea I have is to have MsBuild\Weave write some kind of reference
            // to where the real cache directory exist, and we must consume that.
            // TODO:
            Trace.Assert(weaverCache.Exists);

            appDirectory = new ApplicationDirectory(exe.Directory, new[] { weaverCache });

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
            
            var binary = appDirectory.Binaries.FirstOrDefault((candidate) =>
            {
                return candidate.IsAssembly && candidate.Name == name;
            });

            if (binary != null)
            {
                return Assembly.LoadFile(binary.FilePath);
            }
            
            return null;
        }
    }
}
