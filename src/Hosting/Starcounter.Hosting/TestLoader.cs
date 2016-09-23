
using Starcounter.UnitTesting;
using Starcounter.UnitTesting.Runtime;
using Starcounter.UnitTesting.xUnit;
using System;
using System.Collections.Generic;
using System.Linq;

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
        
        public static void Setup(ushort port, string databaseName)
        {
            SetupTestRoot(databaseName);
            
            // Should it be possible to test all tests previously loaded?
            // TODO:
            
            Handle.POST(port, $"/sc/test/{databaseName}", (Request req) => {
                var root = testRoot;
                var testRequest = TestRequest.FromBytes(req.BodyBytes);

                var name = testRequest.Application + "-tests";
                var host = root.Hosts.FirstOrDefault((candidate) =>
                {
                    return name.Equals(candidate.Name, StringComparison.InvariantCultureIgnoreCase);
                });

                if (host == null)
                {
                    return 404;
                }

                var result = TestRunner.Run(databaseName, host);
                 
                return new Response() { StatusCode = 200, BodyBytes = result.ToBytes() };
            });

            Handle.GET(port, $"/sc/test/{databaseName}/tests", () => {
                // Return a list of all tests loaded.
                // TODO:
                return 404;
            });
        }
        
        static void SetupTestRoot(string db)
        {
            // In this spot, we can put constraints if testing are to be
            // allowed in the contextual database or not. If not, create a
            // TestRoot that don't support hosts to be specified.
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