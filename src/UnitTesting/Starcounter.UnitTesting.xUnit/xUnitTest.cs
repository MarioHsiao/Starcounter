
using Xunit.Abstractions;

namespace Starcounter.UnitTesting.xUnit
{
    internal class xUnitTest : IHostedTest
    {
        readonly xUnitTestAssembly assembly;
        readonly ITestCase testCase;

        public xUnitTest(xUnitTestAssembly asm, ITestCase t)
        {
            assembly = asm;
            testCase = t;
        }

        public override string ToString()
        {
            return $"{testCase.DisplayName} ({testCase.UniqueID})";
        }

        public override int GetHashCode()
        {
            return testCase.UniqueID.GetHashCode();
        }
    }
}