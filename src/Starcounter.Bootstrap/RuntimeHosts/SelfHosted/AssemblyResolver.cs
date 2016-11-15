
using Sc.Server.Weaver.Schema;
using Starcounter.Hosting;
using Starcounter.Internal;
using System;
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
        Assembly appAssembly;

        public AssemblyResolver()
        {
            installationDirectory = StarcounterEnvironment.InstallationDirectory;
            appDirectory = null;
            appAssembly = null;
        }
        
        ApplicationDirectory IAssemblyResolver.RegisterApplication(string executablePath, out DatabaseSchema schema)
        {
            Trace.Assert(appDirectory == null);
            Trace.Assert(appAssembly == null);

            var exe = new FileInfo(executablePath);
            appDirectory = new ApplicationDirectory(exe.Directory);

            appAssembly = Assembly.LoadFile(executablePath);

            var stream = appAssembly.GetManifestResourceStream(DatabaseSchema.EmbeddedResourceName);
            stream.Seek(0, SeekOrigin.Begin);

            schema = DatabaseSchema.DeserializeFrom(stream);
            
            return appDirectory;
        }

        Assembly IAssemblyResolver.ResolveApplication(string executablePath)
        {
            Trace.Assert(appAssembly != null);
            return appAssembly;
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
