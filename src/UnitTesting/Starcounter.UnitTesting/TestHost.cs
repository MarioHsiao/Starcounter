﻿
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

        public void Run(TestResult result, StreamWriter writer)
        {
            Run(testAssemblies.Values, result, writer);
        }

        public string Name { get; set; }

        protected abstract TestAssembly CreateTestAssembly(string assemblyPath);

        protected abstract void Run(IEnumerable<TestAssembly> assemblies, TestResult result, StreamWriter writer);

        void InternalIncludeAssemblyByPath(string assemblyPath)
        {
            if (!File.Exists(assemblyPath))
            {
                throw new FileNotFoundException(nameof(assemblyPath));
            }

            if (testAssemblies.ContainsKey(assemblyPath))
            {
                // Figure out how to treat multiple versions of the same assembly
                // to be given to a certain host.
                // TODO:

                return;
            }

            var testAssembly = CreateTestAssembly(assemblyPath);
            testAssembly.Host = this;

            testAssemblies.Add(assemblyPath, testAssembly);
        }
    }
}
