
namespace Starcounter.UnitTesting
{
    public class TestHostFactory
    {
        public static TestHost CreateHost()
        {
            return new xUnitTestHost();
        }
    }
}
