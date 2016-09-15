
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
            foreach (var xUnitTestAssembly in assemblies.Cast<xUnitTestAssembly>())
            {
                var runner = new xUnitTestAssemblyRunner(xUnitTestAssembly, result, writer);
                runner.Run();
            }
        }
    }
}