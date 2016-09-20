
using Xunit.Abstractions;

namespace Starcounter.UnitTesting.xUnit.ResultFormatters
{
    internal interface IResultFormatter
    {
        void Open(xUnitTestAssembly[] assemblies);

        void BeginAssembly(xUnitTestAssembly assembly);
        void EndAssembly(xUnitTestAssembly assembly);

        void TestPassed(xUnitTestAssembly assembly, ITestPassed passed);
        void TestFailed(xUnitTestAssembly assembly, ITestFailed failed);
        void TestSkipped(xUnitTestAssembly assembly, ITestSkipped skipped);

        void Close();
    }
}