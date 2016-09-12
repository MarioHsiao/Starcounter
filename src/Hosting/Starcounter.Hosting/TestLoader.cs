using Starcounter.Internal;
using Starcounter.UnitTesting;
using Starcounter.UnitTesting.xUnit;
using System;
using System.Collections.Generic;

namespace Starcounter.Hosting
{
    public static class TestLoader
    {
        internal class TestsNotSupported : ITestRoot
        {
            public IEnumerable<TestHost> Hosts {
                get {
                    return new TestHost[0];
                }
            }

            public TestHost IncludeNewHost(string application, ITestHostFactory factory = null)
            {
                throw new InvalidOperationException("The database is not configured to run tests.");
            }
        }

        static ITestRoot testRoot;

        internal static ITestRoot TestRoot {
            get {
                return testRoot;
            }
        }

        public static void Setup()
        {
            SetupTestRoot();
        }
        
        static void SetupTestRoot()
        {
            // In this spot, we can put constraints if testing are to be
            // allowed in the contextual database or not. If not, create a
            // TestRoot that don't support hosts to be specified.
            var db = StarcounterEnvironment.DatabaseNameLower;
            var testsAreSupported = true;

            if (testsAreSupported)
            {
                testRoot = new TestRoot(db, new xUnitTestHostFactory());
            }
            else
            {
                testRoot = new TestsNotSupported();
            }
        }
    }
}
