
using Starcounter.UnitTesting.xUnit;

namespace Starcounter.UnitTesting.Hosts
{
    internal class xUnitTestHost : TestHost
    {
        protected override void RunTestsInAssambly(string assemblyPath)
        {
            new HostedAssemblyTestRunner(assemblyPath).Run();
        }
    }
}