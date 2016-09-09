
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Starcounter.UnitTesting
{
    /// <summary>
    /// Host knowing about a set of assemblies that contain tests and
    /// how to run them.
    /// </summary>
    public abstract class TestHost {
        readonly List<string> testAssemblyPaths = new List<string>();
        
        public void IncludeAssembly(Assembly assembly)
        {
            InternalIncludeAssemblyByPath(assembly.Location);
        }
        
        public void IncludeAssembly(string assemblyPath)
        {
            InternalIncludeAssemblyByPath(assemblyPath);
        }

        public void Run()
        {
            testAssemblyPaths.ForEach((path) =>
            {
                RunTestsInAssambly(path);
            });
        }

        protected abstract void RunTestsInAssambly(string assemblyPath);

        void InternalIncludeAssemblyByPath(string assemblyPath)
        {
            if (!File.Exists(assemblyPath))
            {
                throw new FileNotFoundException(nameof(assemblyPath));
            }

            testAssemblyPaths.Add(assemblyPath);
        }
    }
}
