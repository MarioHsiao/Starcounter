
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

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
            catch (InvalidOperationException e)
            {
                // Check if it does not find execution path. If so, it's
                // probably not meant to be tests (e.g. app.exe). Can we
                // distinguish this from a poor setup (i.e. xunit not copied
                // to exe-path, etc).
                // TODO: 

                Console.WriteLine(e.ToString());
            }
        }
    }
}