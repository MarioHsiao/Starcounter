using Sc.Server.Weaver.Schema;
using Starcounter.Weaver.Diagnostics;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Starcounter.Weaver
{
    class SchemaToTextWriter
    {
        readonly string directory;
        readonly string exeFile;
        readonly WeaverHost host;
        readonly IndentedTextWriter writer;

        public SchemaToTextWriter(string appDirectory, string appExeFile, WeaverHost weaverHost, IndentedTextWriter textWriter)
        {
            directory = appDirectory;
            exeFile = appExeFile;
            host = weaverHost;
            writer = textWriter;
        }
        
        public void Write()
        {
            Assembly appAssembly = null;

            if (host.OutputVerbosity >= Verbosity.Verbose)
            {
                appAssembly = WriteBinaryFileSection();
            }

            if (appAssembly == null)
            {
                var exe = Path.Combine(directory, exeFile);
                appAssembly = Assembly.LoadFrom(exe);
            }

            WriteSchema(appAssembly);
        }

        Assembly WriteBinaryFileSection()
        {
            Assembly appAssembly = null;
            
            var nativeBinaries = new List<string>();

            var w = new AssemblyWalker(directory, (file) => {
                try
                {
                    var a = Assembly.LoadFrom(file);
                    if (Path.GetFileName(file) == exeFile)
                    {
                        appAssembly = a;
                    }
                    return a;
                }
                catch (BadImageFormatException)
                {
                    nativeBinaries.Add(file);
                    return null;
                }
            });

            var weavedAssemblies = new List<Assembly>();
            var otherAssemblies = new List<Assembly>();

            foreach (var assembly in w.Walk())
            {
                if (assembly.IsWeaved())
                {
                    weavedAssemblies.Add(assembly);
                }
                else
                {
                    otherAssemblies.Add(assembly);
                }
            }

            writer.WriteLine("Weaved assemblies:");
            writer.Indent++;
            foreach (var weaved in weavedAssemblies)
            {
                writer.WriteLine(Path.GetFileName(weaved.Location));
            }
            writer.Indent--;
            writer.WriteLine();

            if (otherAssemblies.Count > 0)
            {
                writer.WriteLine("Other assemblies:");
                writer.Indent++;
                foreach (var other in otherAssemblies)
                {
                    writer.WriteLine(Path.GetFileName(other.Location));
                }
                writer.Indent--;
                writer.WriteLine();
            }

            if (nativeBinaries.Count > 0)
            {
                writer.WriteLine("Native binaries:");
                writer.Indent++;
                foreach (var native in nativeBinaries)
                {
                    writer.WriteLine(Path.GetFileName(native));
                }
                writer.Indent--;
                writer.WriteLine();
            }

            return appAssembly;
        }

        void WriteSchema(Assembly appAssembly)
        {
            var stream = appAssembly.GetManifestResourceStream(DatabaseSchema.EmbeddedResourceName);
            if (stream == null)
            {
                host.WriteError(
                    Error.SCERRCODENOTENHANCED,
                    "Assembly {0} contain no schema information. Is it really weaved? (no embedded resource \"{1}\").",
                    exeFile, DatabaseSchema.EmbeddedResourceName
                    );
                return;
            }

            stream.Seek(0, SeekOrigin.Begin);
            var schema = DatabaseSchema.DeserializeFrom(stream);
            schema.DebugOutput(writer);
        }
    }
}
