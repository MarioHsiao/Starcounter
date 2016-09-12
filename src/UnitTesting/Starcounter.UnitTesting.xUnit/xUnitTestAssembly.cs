
using System;
using System.Collections.Generic;

namespace Starcounter.UnitTesting.xUnit
{
    internal class xUnitTestAssembly : TestAssembly
    {
        public xUnitTestAssembly(string assemblyPath) : base(assemblyPath)
        {
            // DiscoverAllTests()
            //
            // TODO:
        }

        public override IEnumerable<IHostedTest> Tests {
            get {
                // Return all tests discovered.
                // TODO:
                throw new NotImplementedException();
            }
        }
    }
}