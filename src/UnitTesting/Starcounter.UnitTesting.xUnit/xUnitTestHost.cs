
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Starcounter.UnitTesting.xUnit.ResultFormatters;

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
            var xunitAssemblies = assemblies.Cast<xUnitTestAssembly>().ToArray();

            var formatter = CreateResultFormatter(writer);

            formatter.Open(xunitAssemblies);

            foreach (var xUnitTestAssembly in xunitAssemblies)
            {
                var runner = new xUnitTestAssemblyRunner(xUnitTestAssembly, result, formatter);
                runner.Run();
            }

            formatter.Close();
        }

        IResultFormatter CreateResultFormatter(StreamWriter writer)
        {
            return new HtmlResultFormatter(writer);
        }
    }
}