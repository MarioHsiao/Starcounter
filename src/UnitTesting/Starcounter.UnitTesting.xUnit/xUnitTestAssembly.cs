
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Starcounter.UnitTesting.xUnit
{
    internal class xUnitTestAssembly : TestAssembly
    {
        TestDiscoverer discoverer;
        XunitFrontController front;
        
        public xUnitTestAssembly(string assemblyPath) : base(assemblyPath)
        {
            discoverer = new TestDiscoverer();
            front = new XunitFrontController(AppDomainSupport.Denied, AssemblyPath);

            RediscoverTests();
        }
        
        public override IEnumerable<IHostedTest> Tests {
            get {
                return discoverer.TestCases.Select((tc) => new xUnitTest(this, tc));
            }
        }

        internal bool ContainTests {
            get {
                return discoverer.ContainTests;
            }
        }

        internal IEnumerable<ITestCase> TestCases {
            get {
                return discoverer.TestCases;
            }
        }

        internal XunitFrontController Front {
            get {
                return front;
            }
        }

        void RediscoverTests()
        {
            discoverer.Reset();
            
            var configuration = ConfigReader.Load(AssemblyPath);
            configuration.ShadowCopy = false;
            configuration.ParallelizeAssembly = false;
            configuration.MaxParallelThreads = 1;
            configuration.AppDomain = AppDomainSupport.Denied;
            configuration.ParallelizeTestCollections = false;
            configuration.PreEnumerateTheories = false;

            var discoveryOptions = TestFrameworkOptions.ForDiscovery(configuration);
            discoveryOptions.SetSynchronousMessageReporting(true);

            try
            {
                front.Find(false, discoverer, discoveryOptions);
                discoverer.Finished.WaitOne();
            }
            catch (InvalidOperationException)
            {
                discoverer.Reset();

                // Check if it does not find execution path. If so, it's
                // probably not meant to be tests (e.g. app.exe). Can we
                // distinguish this from a poor setup (i.e. xunit not copied
                // to exe-path, etc).
                // TODO: 

                // Maybe we should log this when diagnostics are there?
                // Figure out how to make best experience for the user when
                // invoking tests with --test: what do he/she expect?
                // TODO?
                
                /*
                 * {"Unknown test framework: could not find xunit.dll (v1) or xunit.execution.*.dll (v2) in C:\\Users\\Per\\Git\\Starcounter\\PNext\\Level1\\bin\\Debug\\scadmin"}
                 */

                // Console.WriteLine(e.ToString());
            }
        }
    }
}