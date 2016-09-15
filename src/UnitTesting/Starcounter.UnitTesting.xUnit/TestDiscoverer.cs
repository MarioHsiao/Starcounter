using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace Starcounter.UnitTesting.xUnit
{
    internal class TestDiscoverer : TestMessageVisitor<IDiscoveryCompleteMessage>
    {
        readonly List<ITestCase> tests;

        public bool ContainTests {
            get {
                return tests.Count > 0;
            }
        }

        public TestDiscoverer()
        {
            tests = new List<ITestCase>();
        }

        public IEnumerable<ITestCase> TestCases {
            get {
                return tests;
            }
        }

        public void Reset()
        {
            tests.Clear();
        }

        protected override bool Visit(ITestCaseDiscoveryMessage testCaseDiscovered)
        {
            tests.Add(testCaseDiscovered.TestCase);
            return true;
        }
    }
}
