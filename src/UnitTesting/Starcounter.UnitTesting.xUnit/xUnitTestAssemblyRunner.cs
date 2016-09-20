using System.IO;
using Xunit;
using Xunit.Abstractions;
using Starcounter.UnitTesting.xUnit.ResultFormatters;

namespace Starcounter.UnitTesting.xUnit
{
    internal class xUnitTestAssemblyRunner : IMessageSink
    {
        readonly xUnitTestAssembly assembly;
        readonly TestResult result;
        readonly IResultFormatter resultFormatter;

        public xUnitTestAssemblyRunner(xUnitTestAssembly a, TestResult r, IResultFormatter formatter)
        {
            assembly = a;
            result = r;
            resultFormatter = formatter;
        }
        
        public void Run()
        {
            resultFormatter.BeginAssembly(assembly);

            if (assembly.ContainTests)
            {
                var front = assembly.Front;
                var tests = assembly.TestCases;

                front.RunTests(tests, this, GetExecutionOptions());
            }
            
            resultFormatter.EndAssembly(assembly);
        }

        ITestFrameworkExecutionOptions GetExecutionOptions()
        {
            var configuration = ConfigReader.Load(assembly.AssemblyPath);
            configuration.ShadowCopy = false;
            configuration.ParallelizeAssembly = false;
            configuration.MaxParallelThreads = 1;
            configuration.AppDomain = AppDomainSupport.Denied;
            configuration.ParallelizeTestCollections = false;
            configuration.PreEnumerateTheories = false;

            var options = TestFrameworkOptions.ForExecution(configuration);
            options.SetSynchronousMessageReporting(true);

            return options;
        }
        
        void React(ITestPassed test)
        {
            if (test != null)
            {
                result.TestsSucceeded++;
                resultFormatter.TestPassed(assembly, test);
            }
        }

        void React(ITestFailed test)
        {
            if (test != null)
            {
                result.TestsFailed++;
                resultFormatter.TestFailed(assembly, test);
            }
        }

        void React(ITestSkipped test)
        {
            if (test != null)
            {
                result.TestsSkipped++;
                resultFormatter.TestSkipped(assembly, test);
            }
        }

        bool IMessageSink.OnMessage(IMessageSinkMessage message)
        {
            React(message as ITestPassed);
            React(message as ITestFailed);
            React(message as ITestSkipped);
            
            // Continue running tests
            return true;
        }
    }
}