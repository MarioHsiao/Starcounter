
using System.Collections.Generic;
using System.IO;

namespace Starcounter.UnitTesting.xUnit
{
    public sealed class xUnitTestHost : TestHost
    {
        protected override TestAssembly CreateTestAssembly(string assemblyPath)
        {
            return new xUnitTestAssembly(assemblyPath);
        }

        protected override void Run(IEnumerable<TestAssembly> assemblies, TestResult result, StreamWriter writer)
        {
            // Invoke runner on all assemblies and produce results.
            // TODO:

            result.TestsSucceeded = 1;
            result.TestsFailed = 1;
            result.TestsSkipped = 1;
        }
    }
}