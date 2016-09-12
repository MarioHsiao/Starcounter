using System.Collections.Generic;

namespace Starcounter.UnitTesting
{
    /// <summary>
    /// Define semantics of a test assembly.
    /// </summary>
    public abstract class TestAssembly
    {
        /// <summary>
        /// The host the current assembly belong to.
        /// </summary>
        public TestHost Host { get; internal set; }

        /// <summary>
        /// Path to the assembly.
        /// </summary>
        public string AssemblyPath { get; private set; }

        public TestAssembly(string assemblyPath)
        {
            AssemblyPath = assemblyPath;
        }

        /// <summary>
        /// When implemented by a test framework, return the set of
        /// tests contained in the current assembly.
        /// </summary>
        public abstract IEnumerable<IHostedTest> Tests { get; }
    }
}
