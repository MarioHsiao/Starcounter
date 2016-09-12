
namespace Starcounter.UnitTesting.xUnit
{
    public class xUnitTestHostFactory : ITestHostFactory
    {
        public TestHost CreateHost()
        {
            return new xUnitTestHost();
        }
    }
}