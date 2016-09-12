
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
        readonly Dictionary<string, TestAssembly> testAssemblies = new Dictionary<string, TestAssembly>();
        
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
            // When we run, we must allocate a new Result on disk, including
            // headers, config, etc. Emit that here, and then give the stream
            // further to implementations.
            // TODO:
            // No. This comes from the root. At this level, we are already in
            // a context.

            //testAssemblyPaths.ForEach((path) =>
            //{
            //    RunTestsInAssambly(path);
            //});
        }

        protected abstract TestAssembly CreateTestAssembly(string assemblyPath);

        void InternalIncludeAssemblyByPath(string assemblyPath)
        {
            if (!File.Exists(assemblyPath))
            {
                throw new FileNotFoundException(nameof(assemblyPath));
            }

            var testAssembly = CreateTestAssembly(assemblyPath);
            testAssembly.Host = this;

            testAssemblies.Add(assemblyPath, testAssembly);
        }
    }
}
