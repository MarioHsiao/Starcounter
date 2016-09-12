
namespace Starcounter.UnitTesting.xUnit
{
    public sealed class xUnitTestHost : TestHost
    {
        protected override TestAssembly CreateTestAssembly(string assemblyPath)
        {
            return new xUnitTestAssembly(assemblyPath);
        }
    }
}